// sparse-gas processing.
using System.Numerics;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AxensPerformanceEnhancements.Simulation;

internal static class SparseGas
{
    private static readonly AccessTools.FieldRef<GasGrid, Map> MapOf = AccessTools.FieldRefAccess<GasGrid, Map>("map");
    private static readonly AccessTools.FieldRef<GasGrid, uint[]> Density = AccessTools.FieldRefAccess<GasGrid, uint[]>("gasDensity");
    private static readonly AccessTools.FieldRef<GasGrid, int> DissipationIndex = AccessTools.FieldRefAccess<GasGrid, int>("cycleIndexDissipation");
    private static readonly AccessTools.FieldRef<GasGrid, int> DiffusionIndex = AccessTools.FieldRefAccess<GasGrid, int>("cycleIndexDiffusion");
    private static readonly AccessTools.FieldRef<GasGrid, List<IntVec3>> RandomCells = AccessTools.FieldRefAccess<GasGrid, List<IntVec3>>("cellsInRandomOrder");
    private static readonly Action<GasGrid, int> Dissipate = AccessTools.MethodDelegate<Action<GasGrid, int>>(
        AccessTools.Method(typeof(GasGrid), "TryDissipateGases"));
    private static readonly Action<GasGrid, IntVec3> Diffuse = AccessTools.MethodDelegate<Action<GasGrid, IntVec3>>(
        AccessTools.Method(typeof(GasGrid), "TryDiffuseGases"));
    private static readonly ConditionalWeakTable<GasGrid, ActiveCells> States = new();
    private static GasGrid? denseTick;

    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(GasGrid), nameof(GasGrid.Tick)), prefix: new HarmonyMethod(typeof(SparseGas), nameof(Tick)),
            finalizer: new HarmonyMethod(typeof(SparseGas), nameof(TickFinished)));
        foreach (var method in AccessTools.GetDeclaredMethods(typeof(GasGrid)).Where(m => m.Name == "SetDirect" && m.GetParameters()[0].ParameterType == typeof(int)))
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(SparseGas), nameof(IndexChanged)));
        harmony.Patch(AccessTools.Method(typeof(GasGrid), nameof(GasGrid.ClearCellUnsafe)), postfix: new HarmonyMethod(typeof(SparseGas), nameof(CellCleared)));
        harmony.Patch(AccessTools.Method(typeof(GasGrid), nameof(GasGrid.Notify_ThingSpawned)), postfix: new HarmonyMethod(typeof(SparseGas), nameof(ThingSpawned)));
        harmony.Patch(AccessTools.Method(typeof(GasGrid), nameof(GasGrid.Debug_ClearAll)), postfix: new HarmonyMethod(typeof(SparseGas), nameof(Cleared)));
        harmony.Patch(AccessTools.Method(typeof(GasGrid), nameof(GasGrid.ExposeData)), postfix: new HarmonyMethod(typeof(SparseGas), nameof(Exposed)));
    }

    private static bool Tick(GasGrid __instance)
    {
        if (!__instance.CalculateGasEffects) return false;
        var state = States.GetValue(__instance, static grid => new ActiveCells(grid));
        var tick = Find.TickManager.TicksGame;
        if (state.Dense && unchecked((uint)(tick - state.LastDenseScan)) >= 256)
        {
            state.Dense = false;
            state.Rebuild();
        }
        if (state.Dense || state.Count > state.Cells.Count / 16)
        {
            if (!state.Dense) { state.Dense = true; state.LastDenseScan = tick; }
            denseTick = __instance;
            return true;
        }
        state.Process(ref DissipationIndex(__instance), Mathf.CeilToInt(state.Cells.Count * (1f / 64f)), false);
        state.Process(ref DiffusionIndex(__instance), Mathf.CeilToInt(state.Cells.Count * (1f / 32f)), true);
        if (state.Map.IsHashIntervalTick(600)) __instance.RecalculateEverHadGas();
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void IndexChanged(GasGrid __instance, int index)
    {
        if (denseTick == __instance) return;
        if (States.TryGetValue(__instance, out var state)) state.Update(index);
    }

    private static Exception? TickFinished(Exception? __exception)
    {
        denseTick = null;
        return __exception;
    }

    private static void CellCleared(GasGrid __instance, IntVec3 cell)
    {
        if (States.TryGetValue(__instance, out var state)) state.Update(state.Map.cellIndices.CellToIndex(cell));
    }

    private static void ThingSpawned(GasGrid __instance, Thing thing)
    {
        if (!thing.Spawned || thing.def.Fillage != FillCategory.Full || !States.TryGetValue(__instance, out var state)) return;
        foreach (var cell in thing.OccupiedRect()) state.Update(state.Map.cellIndices.CellToIndex(cell));
    }

    private static void Cleared(GasGrid __instance) => States.Remove(__instance);

    private static void Exposed(GasGrid __instance)
    {
        if (Scribe.mode == LoadSaveMode.LoadingVars) States.Remove(__instance);
    }

    private sealed class ActiveCells
    {
        internal readonly Map Map;
        internal readonly List<IntVec3> Cells;
        internal int Count;
        internal bool Dense;
        internal int LastDenseScan;
        private readonly GasGrid grid;
        private readonly uint[] density;
        private readonly int[] positionByCell;
        private readonly ulong[] active;

        internal ActiveCells(GasGrid grid)
        {
            this.grid = grid;
            Map = MapOf(grid);
            density = Density(grid);
            Cells = Map.cellsInRandomOrder.GetAll();
            RandomCells(grid) = Cells;
            positionByCell = new int[Cells.Count];
            active = new ulong[(Cells.Count + 63) / 64];
            for (var i = 0; i < Cells.Count; i++) positionByCell[Map.cellIndices.CellToIndex(Cells[i])] = i;
            Rebuild();
        }

        internal void Rebuild()
        {
            Count = 0;
            Array.Clear(active);
            for (var i = 0; i < density.Length; i++)
            {
                if (density[i] == 0) continue;
                var position = positionByCell[i];
                active[position >> 6] |= 1UL << (position & 63);
                Count++;
            }
        }

        internal void Update(int cell)
        {
            if (Dense) return;
            var position = positionByCell[cell];
            var mask = 1UL << (position & 63);
            ref var word = ref active[position >> 6];
            var wasActive = (word & mask) != 0;
            var isActive = density[cell] != 0;
            if (wasActive == isActive) return;
            if (isActive) { word |= mask; Count++; }
            else { word &= ~mask; Count--; }
        }

        internal void Process(ref int cycleIndex, int count, bool diffusion)
        {
            while (count > 0)
            {
                if (cycleIndex >= Cells.Count) cycleIndex = 0;
                var windowStart = cycleIndex;
                var end = Math.Min(Cells.Count, windowStart + count);
                var next = Next(cycleIndex, end);
                while (next < end)
                {
                    cycleIndex = next;
                    var cell = Cells[next];
                    if (diffusion) Diffuse(grid, cell);
                    else Dissipate(grid, Map.cellIndices.CellToIndex(cell));
                    next = Next(next + 1, end);
                }
                count -= end - windowStart;
                cycleIndex = end;
            }
        }

        private int Next(int start, int end)
        {
            while (start < end)
            {
                var word = active[start >> 6] & (ulong.MaxValue << (start & 63));
                if (word != 0) return Math.Min(end, (start & ~63) + BitOperations.TrailingZeroCount(word));
                start = (start & ~63) + 64;
            }
            return end;
        }
    }
}
