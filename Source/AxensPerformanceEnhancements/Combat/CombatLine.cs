// combat-line shooting calculation.
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;
namespace AxensPerformanceEnhancements.Combat;

internal static class CombatLine
{
    internal static bool CompatibilityBlocked { get; private set; }

    internal static void Apply(Harmony h)
    {
        var target = AccessTools.Method(typeof(AttackTargetFinder), "FriendlyFireConeTargetScoreOffset");
        if (CheckCompatibility(h, target))
            h.Patch(target, transpiler: new HarmonyMethod(typeof(CombatLine), nameof(Transpile)));
        // Catch mods loaded after us, before gameplay starts.
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (CompatibilityBlocked || !CheckCompatibility(h, target))
                AxensPerformanceEnhancementsMod.AppliedFeatures.Remove("combat-line");
        });
    }

    private static bool CheckCompatibility(Harmony h, System.Reflection.MethodBase target)
    {
        var owners = new[] { target, AccessTools.Method(typeof(ShotReport), nameof(ShotReport.HitReportFor)) }
            .SelectMany(method => Harmony.GetPatchInfo(method)?.Owners.Where(owner => owner != h.Id) ?? [])
            .Distinct().ToArray();
        if (owners.Length == 0) return true;
        h.UnpatchAll(h.Id);
        CompatibilityBlocked = true;
        Log.Warning($"[Axen's Performance Enhancements] Combat line disabled: conflicting patches from {string.Join(", ", owners)}.");
        return false;
    }

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
