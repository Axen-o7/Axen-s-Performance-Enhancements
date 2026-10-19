// combat-line shooting calculation.
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;
namespace PerformancePlus.Experimental;

internal static class CombatLine
{
    internal static void Apply(Harmony h) => h.Patch(AccessTools.Method(typeof(AttackTargetFinder), "FriendlyFireConeTargetScoreOffset"), transpiler: new HarmonyMethod(typeof(CombatLine), nameof(Transpile)));
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (var c in instructions)
        {
            if (c.Calls(AccessTools.Method(typeof(ShotReport), nameof(ShotReport.HitReportFor))))
            { replaced++; yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CombatLine), nameof(LineOnly))).WithLabels(c.labels); }
            else yield return c;
        }
        if (replaced != 1) throw new InvalidOperationException("Expected exactly one hit-report construction.");
    }
    private static ShotReport LineOnly(Thing caster, Verb verb, LocalTargetInfo target)
    {
        var report = default(ShotReport);
        if (!verb.TryFindShootLineFromTo(verb.caster.Position, target, out report.shootLine))
            report.shootLine = new ShootLine(IntVec3.Invalid, IntVec3.Invalid);
        return report;
    }
}
