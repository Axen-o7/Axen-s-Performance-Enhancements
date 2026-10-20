// vacuum room traversal.
using HarmonyLib;
using Verse;
using RimWorld;
using static Verse.VacuumComponent;
namespace AxensPerformanceEnhancements.Experimental;

internal static class VacuumBfs
{
    internal static void Apply(Harmony h) => h.Patch(AccessTools.Method(typeof(VacuumComponent), "HasDirectPathToVacuum"), prefix: new HarmonyMethod(typeof(VacuumBfs), nameof(Before)));
    private static bool Before(VacuumComponent __instance, Room room, out HashSet<IntVec3> warning, ref bool __result)
    {
        visited.Add(room);
        frontier.Enqueue(room);
        Room? result;
        while (frontier.TryDequeue(out result))
        {
            if (result.ExposedToSpace)
            {
                warning = exteriorCells[result];
                visited.Clear();
                exteriorCells.Clear();
                frontier.Clear();
                __result = true; return false;
            }
            if (!__instance.roomGroups.TryGetValue(result, out var value))
            {
                continue;
            }
            foreach (var (room3, hashSet2) in value.adjacent)
            {
                if (visited.Contains(room3))
                {
                    continue;
                }
                foreach (IntVec3 item in hashSet2)
                {
                    Building edifice = item.GetEdifice(__instance.map);
                    if (edifice == null || edifice is Building_Door { Open: not false })
                    {
                        exteriorCells[room3] = hashSet2;
                        visited.Add(room3);
                        frontier.Enqueue(room3);
                        break;
                    }
                }
            }
        }
        visited.Clear();
        exteriorCells.Clear();
        frontier.Clear();
        warning = null!;
        __result = false;
        return false;
    }
}
