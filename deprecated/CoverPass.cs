// single-pass cover calculation.
// Deprecated: individual benchmarks did not establish a consistent performance benefit.
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
namespace AxensPerformanceEnhancements.Experimental;

internal static class CoverPass
{
    internal static void Apply(Harmony h) => h.Patch(AccessTools.Method(typeof(ShotReport), nameof(ShotReport.HitReportFor)), transpiler: new HarmonyMethod(typeof(CoverPass), nameof(Transpile)));
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var chance = generator.DeclareLocal(typeof(float)); int lists = 0, sums = 0;
        foreach (var c in instructions)
        {
            if (c.Calls(AccessTools.Method(typeof(CoverUtility), nameof(CoverUtility.CalculateCoverGiverSet))))
            {
                lists++; yield return new CodeInstruction(OpCodes.Ldloca, chance).WithLabels(c.labels);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CoverPass), nameof(Both)));
            }
            else if (c.Calls(AccessTools.Method(typeof(CoverUtility), nameof(CoverUtility.CalculateOverallBlockChance))))
            {
                sums++; yield return new CodeInstruction(OpCodes.Pop).WithLabels(c.labels);
                yield return new CodeInstruction(OpCodes.Pop); yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Ldloc, chance);
            }
            else yield return c;
        }
        if (lists != 1 || sums != 1) throw new InvalidOperationException("Cover call layout changed.");
    }
    private static List<CoverInfo> Both(LocalTargetInfo target, IntVec3 shooterLoc, Map map, out float overall)
    {
        var covers = new List<CoverInfo>(); overall = 0f;
        if (target.HasThing && !target.Thing.def.CanBenefitFromCover) return covers;
        var cell = target.Cell;
        for (int i = 0; i < 8; i++)
        {
            var adjacent = cell + GenAdj.AdjacentCells[i];
            if (!adjacent.InBounds(map) || !CoverUtility.TryFindAdjustedCoverInCell(shooterLoc, target, adjacent, map, out var result)) continue;
            if (result.BlockChance > 0f) covers.Add(result);
            if (shooterLoc != adjacent) overall += (1f - overall) * result.BlockChance;
        }
        return covers;
    }
}
