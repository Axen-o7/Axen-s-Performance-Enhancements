// shared compatibility checks after startup patching.
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AxensPerformanceEnhancements;

internal static class StartupChecks
{
    // Many mods patch from [StaticConstructorOnStartup], which runs after mod construction.
    // Kept outside the attributed runner so registering does not trigger its static constructor early.
    internal static readonly List<Action> AfterAllPatches = [];

    // A skipping prefix would silently bypass other mods' transpilers on the replaced methods.
    internal static void GuardReplacement(Harmony harmony, string feature, params MethodBase[] replaced) => AfterAllPatches.Add(() =>
    {
        var owners = replaced.SelectMany(method => Harmony.GetPatchInfo(method)?.Transpilers ?? [])
            .Select(patch => patch.owner).Where(owner => owner != harmony.Id).Distinct().ToArray();
        if (owners.Length == 0) return;
        harmony.UnpatchAll(harmony.Id);
        AxensPerformanceEnhancementsMod.AppliedFeatures.Remove(feature);
        Log.Warning($"[Axen's Performance Enhancements] {feature} disabled: conflicting transpilers from {string.Join(", ", owners)}.");
    });
}

[StaticConstructorOnStartup]
internal static class StartupChecksRunner
{
    // Actions queued here run after every static constructor has finished.
    static StartupChecksRunner()
    {
        foreach (var check in StartupChecks.AfterAllPatches) LongEventHandler.ExecuteWhenFinished(check);
        StartupChecks.AfterAllPatches.Clear();
    }
}
