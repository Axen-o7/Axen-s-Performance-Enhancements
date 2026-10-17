using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Unity.Collections;
using Verse;

namespace PerformancePlus.Pathfinding;

internal static class PathOutputMemory
{
    internal static readonly int InitialNodes = 128;

    internal static void Apply(Harmony harmony)
    {
        var output = AccessTools.Inner(typeof(PathFinder), "PathJobOutput");
        harmony.Patch(AccessTools.Method(output, "Initialize"),
            transpiler: new HarmonyMethod(typeof(PathOutputMemory), nameof(Initialize)));
    }

    private static IEnumerable<CodeInstruction> Initialize(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var matches = 0;
        for (var i = 1; i < code.Count; i++)
        {
            if (code[i].opcode != OpCodes.Newobj || code[i].operand is not ConstructorInfo ctor
                || ctor.DeclaringType != typeof(NativeList<IntVec3>)) continue;
            var capacity = i - 3;
            if (capacity < 0 || !code[capacity].IsLdarg(1))
                throw new InvalidOperationException("Unexpected PathJobOutput capacity IL; refusing an unsafe patch.");
            code[capacity].opcode = OpCodes.Ldc_I4;
            code[capacity].operand = InitialNodes;
            matches++;
        }
        if (matches != 1) throw new InvalidOperationException($"Expected one path allocation, found {matches}.");
        return code;
    }
}
