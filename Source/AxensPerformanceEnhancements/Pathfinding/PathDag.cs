// parallel path-grid scheduling.
using HarmonyLib;
using Unity.Jobs;
using Verse;
namespace AxensPerformanceEnhancements.Pathfinding;

internal static class PathDag
{
    internal static void Apply(Harmony h)
    {
        var target = AccessTools.Method(typeof(PathFinder), "ScheduleGridJobs");
        h.Patch(target, prefix: new HarmonyMethod(typeof(PathDag), nameof(Before)));
        StartupChecks.GuardReplacement(h, "path-dag", target);
    }
    private static bool Before(PathFinder __instance, ref JobHandle __result)
    {
        if (__instance.tmpCurrentWork.Any(static request => request.customizer != null || request.RequesterLord != null)) return true;
        JobHandle joined = default;
        foreach (var request in __instance.tmpCurrentWork)
        {
            var key = PathFinder.MapGridRequest.For(request);
            if (!__instance.gridJobLookup.ContainsKey(key))
            {
                var next = __instance.ScheduleGridJob(request, default).handle;
                joined = JobHandle.CombineDependencies(joined, next);
            }
        }
        __result = joined;
        return false;
    }
}
