// reservation indexing.
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace AxensPerformanceEnhancements.AI;

internal static class ReservationQueries
{
    private sealed class TargetIndex
    {
        internal List<ReservationManager.Reservation>? Source;
        internal int Version = -1;
        internal readonly Dictionary<LocalTargetInfo, List<ReservationManager.Reservation>> Targets = new();
        internal readonly Stack<List<ReservationManager.Reservation>> Spare = new();
    }
    private static readonly ConditionalWeakTable<ReservationManager, TargetIndex> Indices = new();
    private static readonly AccessTools.FieldRef<List<ReservationManager.Reservation>, int> ListVersion = AccessTools.FieldRefAccess<List<ReservationManager.Reservation>, int>("_version");
    private static readonly FieldInfo Reservations = AccessTools.Field(typeof(ReservationManager), "reservations");
    private static readonly MethodInfo Already = AccessTools.Method(typeof(ReservationManager), "IsAlreadyReserved");
    private static readonly List<ReservationManager.Reservation> Empty = [];

    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(ReservationManager), "CanReserve"), transpiler: new HarmonyMethod(typeof(ReservationQueries), nameof(Rewrite)));
        harmony.Patch(Already, transpiler: new HarmonyMethod(typeof(ReservationQueries), nameof(IndexAlready)));
    }
    private static List<ReservationManager.Reservation> ForTarget(ReservationManager manager, LocalTargetInfo target)
    {
        var source = manager.ReservationsReadOnly;
        if (Scribe.mode != LoadSaveMode.Inactive) return source;
        var index = Indices.GetOrCreateValue(manager); var version = ListVersion(source);
        if (!ReferenceEquals(source, index.Source) || version != index.Version)
        {
            foreach (var list in index.Targets.Values) { list.Clear(); index.Spare.Push(list); }
            index.Targets.Clear();
            foreach (var reservation in source)
            {
                if (!index.Targets.TryGetValue(reservation.Target, out var list))
                    index.Targets.Add(reservation.Target, list = index.Spare.TryPop(out var spare) ? spare : []);
                list.Add(reservation);
            }
            index.Source = source; index.Version = version;
        }
        return index.Targets.GetValueOrDefault(target, Empty);
    }
    // Loop conditions and indexers reload the list, so resolve it once per invocation.
    private static List<ReservationManager.Reservation> ForTargetOnce(ReservationManager manager, LocalTargetInfo target,
        ref List<ReservationManager.Reservation>? cache) => cache ??= ForTarget(manager, target);
    private static IEnumerable<CodeInstruction> IndexAlready(IEnumerable<CodeInstruction> input, ILGenerator generator) => RewriteLists(input, generator);
    private static IEnumerable<CodeInstruction> RewriteLists(IEnumerable<CodeInstruction> input, ILGenerator generator)
    {
        var cache = generator.DeclareLocal(typeof(List<ReservationManager.Reservation>));
        yield return new CodeInstruction(OpCodes.Ldnull);
        yield return new CodeInstruction(OpCodes.Stloc, cache);
        var hits = 0;
        foreach (var instruction in input)
        {
            if (instruction.LoadsField(Reservations))
            {
                hits++;
                yield return new CodeInstruction(OpCodes.Ldarg_2).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                yield return new CodeInstruction(OpCodes.Ldloca, cache);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ReservationQueries), nameof(ForTargetOnce)));
            }
            else yield return instruction;
        }
        if (hits != 2) throw new InvalidOperationException("Reservation list target pattern changed.");
    }
    private static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> input, ILGenerator generator)
    {
        var code = input.Select(i => new CodeInstruction(i)).ToList();
        var hits = 0;
        for (var i = 0; i < code.Count; i++)
            if (code[i].Calls(Already) && ++hits == 2)
            {
                var first = new CodeInstruction(OpCodes.Pop).MoveLabelsFrom(code[i]).MoveBlocksFrom(code[i]);
                code.RemoveAt(i);
                code.InsertRange(i, [first, new(OpCodes.Pop), new(OpCodes.Pop), new(OpCodes.Pop), new(OpCodes.Pop), new(OpCodes.Ldc_I4_0)]);
                break;
            }
        if (hits != 2) throw new InvalidOperationException("Expected two already-reserved calls.");
        return RewriteLists(code, generator);
    }
}
