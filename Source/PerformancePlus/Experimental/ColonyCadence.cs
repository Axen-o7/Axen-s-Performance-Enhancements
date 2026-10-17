using HarmonyLib;
using Verse;

namespace PerformancePlus.Experimental;

internal static class ColonyCadence
{
    internal const int Interval = 60;
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.UpdateRateTicks)),
            postfix: new HarmonyMethod(typeof(ColonyCadence), nameof(PawnRate)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(Thing), "MaxTickIntervalRate"),
            postfix: new HarmonyMethod(typeof(ColonyCadence), nameof(Maximum)));
    }

    private static bool Eligible(Pawn pawn) => pawn.Spawned && pawn.IsColonistPlayerControlled && !pawn.Drafted && !pawn.InMentalState;
    private static void PawnRate(Pawn __instance, ref int __result)
    { if (Eligible(__instance)) __result = Math.Max(__result, Interval); }
    private static void Maximum(Thing __instance, ref int __result)
    { if (__instance is Pawn pawn && Eligible(pawn)) __result = Math.Max(__result, Interval); }
}
