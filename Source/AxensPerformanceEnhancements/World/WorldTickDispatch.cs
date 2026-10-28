// world dispatch.
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace AxensPerformanceEnhancements.World;

internal static class WorldTickDispatch
{
    private static readonly AccessTools.FieldRef<WorldObject, int> Delta = AccessTools.FieldRefAccess<WorldObject, int>("tickDelta");
    private static readonly AccessTools.FieldRef<List<WorldObjectComp>, int> Version = AccessTools.FieldRefAccess<List<WorldObjectComp>, int>("_version");
    private static readonly List<WorldObject> Snapshot =
        (List<WorldObject>)AccessTools.Field(typeof(WorldObjectsHolder), "tmpWorldObjects").GetValue(null)!;
    private static readonly Action<Settlement, int> Interval = AccessTools.MethodDelegate<Action<Settlement, int>>(
        AccessTools.Method(typeof(Settlement), "TickInterval"));
    private static readonly ConditionalWeakTable<Settlement, TickShape> Shapes = new();
    private static readonly ConditionalWeakTable<WorldObjectsHolder, Schedule> Schedules = new();
    private static readonly AccessTools.FieldRef<List<WorldObject>, int> ObjectVersion = AccessTools.FieldRefAccess<List<WorldObject>, int>("_version");
    private static bool compatible;
    private static bool scheduledCompatible;

    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.WorldObjectsHolderTick)),
            prefix: new HarmonyMethod(typeof(WorldTickDispatch), nameof(Tick)));
        harmony.Patch(AccessTools.Method(typeof(WorldObject), "InitializeComps"),
            postfix: new HarmonyMethod(typeof(WorldTickDispatch), nameof(ComponentsChanged)));
        harmony.Patch(AccessTools.Method(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.ExposeData)),
            prefix: new HarmonyMethod(typeof(WorldTickDispatch), nameof(BeforeExpose)));
        StartupChecks.AfterAllPatches.Add(() =>
        {
            compatible = new[] {
                AccessTools.Method(typeof(WorldObject), nameof(WorldObject.DoTick)),
                AccessTools.Method(typeof(WorldObject), "Tick"),
                AccessTools.Method(typeof(WorldObjectComp), nameof(WorldObjectComp.CompTick)),
                AccessTools.Method(typeof(Settlement), nameof(Settlement.GetChildHolders)),
                AccessTools.Method(typeof(MapParent), nameof(MapParent.GetDirectlyHeldThings))
            }.All(method => Harmony.GetPatchInfo(method) is not { Owners.Count: > 0 });
            scheduledCompatible = LoadedModManager.RunningModsListForReading.All(m => m.IsOfficialMod ||
                m.PackageId is "brrainz.harmony" or AxensPerformanceEnhancementsMod.PackageId);
        });
    }

    private static bool Tick(WorldObjectsHolder __instance)
    {
        var tick = GenTicks.TicksGame;
        var schedule = Schedules.GetOrCreateValue(__instance);
        if (!compatible || WorldRendererUtility.WorldSelected)
        {
            schedule.Flush(); schedule.Initialized = false; schedule.LastTick = tick;
            return true;
        }
        if (scheduledCompatible && (schedule.LastTick == int.MinValue || tick == schedule.LastTick + 1))
        {
            if (!schedule.Initialized || schedule.Version != ObjectVersion(__instance.AllWorldObjects) || schedule.NeedsRebuild)
            {
                schedule.Flush(); schedule.Build(__instance.AllWorldObjects, tick);
            }
            schedule.Run(tick);
            return false;
        }
        schedule.Flush(); schedule.Initialized = false; schedule.LastTick = tick;
        var snapshot = Snapshot;
        snapshot.Clear();
        snapshot.AddRange(__instance.AllWorldObjects);
        foreach (var item in snapshot)
        {
            if (item.GetType() != typeof(Settlement)) { item.DoTick(); continue; }
            var settlement = (Settlement)item;
            if (settlement.trader != null && (settlement.trader.GetType() != typeof(Settlement_TraderTracker)
                || settlement.trader.GetDirectlyHeldThings() != null)) { item.DoTick(); continue; }
            var shape = Shapes.GetOrCreateValue(settlement);
            var comps = settlement.AllComps;
            if (shape.Components != comps || shape.Version != Version(comps)) shape.Refresh(comps);
            if (!shape.CanSkip) { item.DoTick(); continue; }
            ref var delta = ref Delta(item);
            delta++;
            if (delta > 15 || GenTicks.IsTickInterval(item.HashOffset(), 15))
            {
                Interval(settlement, delta);
                delta = 0;
            }
        }
        return false;
    }

    private static bool CanDefer(WorldObject item)
    {
        if (item.GetType() != typeof(Settlement)) return false;
        var settlement = (Settlement)item;
        if (settlement.trader != null && (settlement.trader.GetType() != typeof(Settlement_TraderTracker)
            || settlement.trader.GetDirectlyHeldThings() is { dontTickContents: false })) return false;
        var shape = Shapes.GetOrCreateValue(settlement);
        var comps = settlement.AllComps;
        if (shape.Components != comps || shape.Version != Version(comps)) shape.Refresh(comps);
        return shape.CanSkip;
    }

    private static void ComponentsChanged()
    {
        foreach (var pair in Schedules) pair.Value.NeedsRebuild = true;
    }

    private static void BeforeExpose(WorldObjectsHolder __instance)
    {
        if (Scribe.mode == LoadSaveMode.Saving) Synchronize(__instance);
    }

    private static void Synchronize(WorldObjectsHolder holder)
    {
        if (Schedules.TryGetValue(holder, out var schedule)) schedule.Flush();
    }

    private sealed class Schedule
    {
        public Schedule() { }
        internal int LastTick = int.MinValue;
        internal int Version;
        internal bool Initialized;
        internal bool NeedsRebuild;
        private readonly List<Entry>[] buckets = Enumerable.Range(0, 15).Select(_ => new List<Entry>()).ToArray();
        private readonly List<Entry> deferred = [];

        internal void Build(List<WorldObject> objects, int tick)
        {
            foreach (var bucket in buckets) bucket.Clear();
            deferred.Clear();
            Version = ObjectVersion(objects);
            foreach (var item in objects)
            {
                var entry = new Entry(item, CanDefer(item), tick - 1);
                if (entry.Deferred)
                {
                    var untilHash = (15 - unchecked(tick + item.HashOffset()) % 15) % 15;
                    var untilOverflow = Math.Max(0, 15 - Delta(item));
                    var due = tick + Math.Min(untilHash, untilOverflow);
                    buckets[(due % 15 + 15) % 15].Add(entry);
                    deferred.Add(entry);
                }
                else
                    foreach (var bucket in buckets) bucket.Add(entry);
            }
            LastTick = tick - 1;
            Initialized = true; NeedsRebuild = false;
        }

        internal void Run(int tick)
        {
            foreach (var entry in buckets[(tick % 15 + 15) % 15])
            {
                if (!entry.Deferred) { entry.Item.DoTick(); continue; }
                ref var delta = ref Delta(entry.Item);
                delta += tick - entry.AccountedTick;
                entry.AccountedTick = tick;
                if (delta <= 15 && !GenTicks.IsTickInterval(entry.Item.HashOffset(), 15))
                {
                    NeedsRebuild = true;
                    continue;
                }
                Interval((Settlement)entry.Item, delta);
                delta = 0;
                if (!GenTicks.IsTickInterval(entry.Item.HashOffset(), 15)) NeedsRebuild = true;
            }
            LastTick = tick;
        }

        internal void Flush()
        {
            if (!Initialized) return;
            foreach (var entry in deferred)
            {
                Delta(entry.Item) += LastTick - entry.AccountedTick;
                entry.AccountedTick = LastTick;
            }
        }
    }

    private sealed class Entry(WorldObject item, bool deferred, int accountedTick)
    {
        internal readonly WorldObject Item = item;
        internal readonly bool Deferred = deferred;
        internal int AccountedTick = accountedTick;
    }

    private sealed class TickShape
    {
        internal int Version = -1;
        internal bool CanSkip;
        internal List<WorldObjectComp>? Components;

        internal void Refresh(List<WorldObjectComp> comps)
        {
            Version = WorldTickDispatch.Version(comps);
            Components = comps;
            CanSkip = comps.All(comp => comp is not IThingHolder &&
                AccessTools.Method(comp.GetType(), nameof(WorldObjectComp.CompTick)).DeclaringType == typeof(WorldObjectComp));
        }
    }
}
