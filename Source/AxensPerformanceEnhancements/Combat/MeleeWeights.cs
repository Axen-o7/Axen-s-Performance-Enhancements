// invocation-local melee weight reuse.
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AxensPerformanceEnhancements.Combat;

internal static class MeleeWeights
{
    private sealed class Frame
    {
        internal Pawn Pawn = null!;
        internal Frame? Previous;
        internal readonly Dictionary<Verb, float> Weights = new(8);
    }
    [ThreadStatic] private static Frame? current;
    [ThreadStatic] private static Frame? spare;

    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(Pawn_MeleeVerbs), "GetUpdatedAvailableVerbsList"),
            prefix: new HarmonyMethod(typeof(MeleeWeights), nameof(Begin)),
            transpiler: new HarmonyMethod(typeof(MeleeWeights), nameof(ReuseWeight)),
            finalizer: new HarmonyMethod(typeof(MeleeWeights), nameof(End)));
        harmony.Patch(AccessTools.Method(typeof(VerbUtility), "GetSelectionCategory"),
            transpiler: new HarmonyMethod(typeof(MeleeWeights), nameof(ReuseWeight)));
    }
    private static void Begin(Pawn_MeleeVerbs __instance, out Frame __state)
    {
        __state = spare ?? new Frame(); spare = null;
        __state.Previous = current; __state.Pawn = __instance.Pawn; current = __state;
    }
    private static Exception? End(Frame? __state, Exception? __exception)
    {
        if (__state == null) return __exception;
        current = __state.Previous;
        __state.Weights.Clear(); __state.Pawn = null!; __state.Previous = null; spare = __state;
        return __exception;
    }
    private static float CachedWeight(Verb verb, Pawn pawn)
    {
        if (current is not { } frame || frame.Pawn != pawn) return VerbUtility.InitialVerbWeight(verb, pawn);
        if (!frame.Weights.TryGetValue(verb, out var weight))
            frame.Weights.Add(verb, weight = VerbUtility.InitialVerbWeight(verb, pawn));
        return weight;
    }
    private static IEnumerable<CodeInstruction> ReuseWeight(IEnumerable<CodeInstruction> input)
    {
        var hits = 0;
        foreach (var instruction in input)
        {
            if (instruction.Calls(AccessTools.Method(typeof(VerbUtility), "InitialVerbWeight")))
            { hits++; instruction.operand = AccessTools.Method(typeof(MeleeWeights), nameof(CachedWeight)); }
            yield return instruction;
        }
        if (hits != 1) throw new InvalidOperationException("Melee initial-weight call pattern changed.");
    }
}
