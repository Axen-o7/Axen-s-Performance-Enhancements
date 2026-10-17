using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;

namespace PerformancePlus.Psychic;

internal static class EntropyReuse
{
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(Pawn_PsychicEntropyTracker), "EntropyToRelativeValue"),
            transpiler: new HarmonyMethod(typeof(EntropyReuse), nameof(Conversion)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(Pawn_PsychicEntropyTracker), "Severity"),
            transpiler: new HarmonyMethod(typeof(EntropyReuse), nameof(Severity)));
    }
    private static IEnumerable<CodeInstruction> Conversion(IEnumerable<CodeInstruction> input, ILGenerator generator)
    {
        var cache = generator.DeclareLocal(typeof(float)); var hits = 0;
        var getter = AccessTools.PropertyGetter(typeof(Pawn_PsychicEntropyTracker), "MaxEntropy");
        foreach (var instruction in input)
        {
            if (!instruction.Calls(getter)) { yield return instruction; continue; }
            if (hits++ == 0)
            { yield return instruction; yield return new(OpCodes.Dup); yield return new(OpCodes.Stloc, cache); }
            else
            {
                yield return new CodeInstruction(OpCodes.Pop).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                yield return new(OpCodes.Ldloc, cache);
            }
        }
        if (hits != 4) throw new InvalidOperationException("Expected four maximum reads in entropy conversion.");
    }
    private static float RelativeOnce(Pawn_PsychicEntropyTracker tracker, ref bool initialized, ref float value)
    {
        if (!initialized) { value = tracker.EntropyRelativeValue; initialized = true; }
        return value;
    }
    private static IEnumerable<CodeInstruction> Severity(IEnumerable<CodeInstruction> input, ILGenerator generator)
    {
        var initialized = generator.DeclareLocal(typeof(bool)); var value = generator.DeclareLocal(typeof(float));
        yield return new(OpCodes.Ldc_I4_0); yield return new(OpCodes.Stloc, initialized);
        var hits = 0;
        foreach (var instruction in input)
        {
            if (instruction.Calls(AccessTools.PropertyGetter(typeof(Pawn_PsychicEntropyTracker), "EntropyRelativeValue")))
            {
                hits++;
                yield return new CodeInstruction(OpCodes.Ldloca, initialized).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                yield return new(OpCodes.Ldloca, value);
                yield return new(OpCodes.Call, AccessTools.Method(typeof(EntropyReuse), nameof(RelativeOnce)));
            }
            else yield return instruction;
        }
        if (hits != 1) throw new InvalidOperationException("Entropy severity loop pattern changed.");
    }
}
