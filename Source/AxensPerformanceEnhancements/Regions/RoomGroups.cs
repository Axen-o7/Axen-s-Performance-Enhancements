// room grouping.
using System.Buffers;
using HarmonyLib;
using Verse;
namespace AxensPerformanceEnhancements.Regions;

internal static class RoomGroups
{
    internal static void Apply(Harmony h)
    {
        var districts = AccessTools.Method(typeof(RegionAndRoomUpdater), "CreateOrAttachToExistingDistricts");
        var rooms = AccessTools.Method(typeof(RegionAndRoomUpdater), "CreateOrAttachToExistingRooms");
        h.Patch(districts, prefix: new HarmonyMethod(typeof(RoomGroups), nameof(CreateOrAttachToExistingDistricts)));
        h.Patch(rooms, prefix: new HarmonyMethod(typeof(RoomGroups), nameof(CreateOrAttachToExistingRooms)));
        StartupChecks.GuardReplacement(h, "room-groups", districts, rooms);
    }
    // Orders items by group, keeping source order within each group; group g spans starts[g]..starts[g + 1].
    private static T[] GroupInOrder<T>(IEnumerable<T> items, int count, int groupCount, Func<T, int> groupOf, out int[] starts)
    {
        starts = ArrayPool<int>.Shared.Rent(groupCount + 1);
        Array.Clear(starts, 0, groupCount + 1);
        foreach (var item in items) starts[groupOf(item) + 1]++;
        for (int group = 0; group < groupCount; group++) starts[group + 1] += starts[group];
        var ordered = ArrayPool<T>.Shared.Rent(count);
        var next = ArrayPool<int>.Shared.Rent(groupCount);
        Array.Copy(starts, next, groupCount);
        foreach (var item in items) ordered[next[groupOf(item)]++] = item;
        ArrayPool<int>.Shared.Return(next);
        return ordered;
    }

    private static void Release<T>(T[] ordered, int[] starts)
    {
        ArrayPool<T>.Shared.Return(ordered, clearArray: true);
        ArrayPool<int>.Shared.Return(starts);
    }

    private static bool CreateOrAttachToExistingDistricts(RegionAndRoomUpdater __instance, int numRegionGroups)
    {
        var ordered = GroupInOrder(__instance.newRegions, __instance.newRegions.Count, numRegionGroups,
            static region => region.newRegionGroupIndex, out var starts);
        try { AttachDistricts(__instance, numRegionGroups, ordered, starts); }
        finally { Release(ordered, starts); }
        return false;
    }

    private static void AttachDistricts(RegionAndRoomUpdater __instance, int numRegionGroups, Region[] ordered, int[] starts)
    {
        for (int i = 0; i < numRegionGroups; i++)
        {
            __instance.currentRegionGroup.Clear();
            for (int k = starts[i]; k < starts[i + 1]; k++) __instance.currentRegionGroup.Add(ordered[k]);

            if (!__instance.currentRegionGroup[0].type.AllowsMultipleRegionsPerDistrict())
            {
                if (__instance.currentRegionGroup.Count != 1)
                {
                    Log.Error("Region type doesn't allow multiple regions per room but there are >1 regions in this group.");
                }
                District district = District.MakeNew(__instance.map);
                __instance.currentRegionGroup[0].District = district;
                __instance.newDistricts.Add(district);
                continue;
            }
            District district2 = __instance.FindCurrentRegionGroupNeighborWithMostRegions(out var multipleOldNeighborDistricts);
            if (district2 == null)
            {
                District item = RegionTraverser.FloodAndSetDistricts(__instance.currentRegionGroup[0], __instance.map, null);
                __instance.newDistricts.Add(item);
            }
            else if (!multipleOldNeighborDistricts)
            {
                for (int k = 0; k < __instance.currentRegionGroup.Count; k++)
                {
                    __instance.currentRegionGroup[k].District = district2;
                }
                __instance.reusedOldDistricts.Add(district2);
            }
            else
            {
                RegionTraverser.FloodAndSetDistricts(__instance.currentRegionGroup[0], __instance.map, district2);
                __instance.reusedOldDistricts.Add(district2);
            }
        }
    }

    private static bool CreateOrAttachToExistingRooms(RegionAndRoomUpdater __instance, int numRooms)
    {
        // Reused districts precede new ones, matching the original group order.
        var ordered = GroupInOrder(__instance.reusedOldDistricts.Concat(__instance.newDistricts),
            __instance.reusedOldDistricts.Count + __instance.newDistricts.Count, numRooms,
            static district => district.newOrReusedRoomIndex, out var starts);
        try { AttachRooms(__instance, numRooms, ordered, starts); }
        finally { Release(ordered, starts); }
        return false;
    }

    private static void AttachRooms(RegionAndRoomUpdater __instance, int numRooms, District[] ordered, int[] starts)
    {
        for (int i = 0; i < numRooms; i++)
        {
            __instance.currentDistrictGroup.Clear();
            for (int k = starts[i]; k < starts[i + 1]; k++) __instance.currentDistrictGroup.Add(ordered[k]);

            Room room = __instance.FindCurrentRoomNeighborWithMostRegions(out var multipleOldNeighborRooms);
            if (room == null)
            {
                Room room2 = Room.MakeNew(__instance.map);
                __instance.roomLookup[room2.ID] = room2;
                __instance.FloodAndSetRooms(__instance.currentDistrictGroup[0], room2);
                __instance.newRooms.Add(room2);
            }
            else if (!multipleOldNeighborRooms)
            {
                for (int k = 0; k < __instance.currentDistrictGroup.Count; k++)
                {
                    __instance.currentDistrictGroup[k].Room = room;
                }
                __instance.reusedOldRooms.Add(room);
            }
            else
            {
                __instance.FloodAndSetRooms(__instance.currentDistrictGroup[0], room);
                __instance.reusedOldRooms.Add(room);
            }
        }
    }
}
