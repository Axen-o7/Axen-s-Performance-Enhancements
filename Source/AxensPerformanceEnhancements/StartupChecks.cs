// shared compatibility checks after startup patching.
using Verse;

namespace AxensPerformanceEnhancements;

internal static class StartupChecks
{
    // Many mods patch from [StaticConstructorOnStartup], which runs after mod construction.
    // Kept outside the attributed runner so registering does not trigger its static constructor early.
    internal static readonly List<Action> AfterAllPatches = [];
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
