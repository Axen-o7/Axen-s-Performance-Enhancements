// room grouping.
using HarmonyLib;
using Verse;
namespace PerformancePlus.Experimental;

internal static class RoomGroups
{
    internal static void Apply(Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(RegionAndRoomUpdater), "CreateOrAttachToExistingDistricts"), prefix: new HarmonyMethod(typeof(RoomGroups), nameof(CreateOrAttachToExistingDistricts)));
        h.Patch(AccessTools.Method(typeof(RegionAndRoomUpdater), "CreateOrAttachToExistingRooms"), prefix: new HarmonyMethod(typeof(RoomGroups), nameof(CreateOrAttachToExistingRooms)));
    }
    private static bool CreateOrAttachToExistingDistricts(RegionAndRoomUpdater __instance, int numRegionGroups)
    {
        var groups = new List<Region>[numRegionGroups];
        for (int group = 0; group < groups.Length; group++) groups[group] = [];
        foreach (var item in __instance.newRegions) groups[item.newRegionGroupIndex].Add(item);

        for (int i = 0; i < numRegionGroups; i++)
        {
            __instance.currentRegionGroup.Clear();
            __instance.currentRegionGroup.AddRange(groups[i]);

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
        return false;
    }
    private static bool CreateOrAttachToExistingRooms(RegionAndRoomUpdater __instance, int numRooms)
    {
        var groups = new List<District>[numRooms];
        for (int group = 0; group < groups.Length; group++) groups[group] = [];
        foreach (var district in __instance.reusedOldDistricts) groups[district.newOrReusedRoomIndex].Add(district);
        foreach (var item in __instance.newDistricts) groups[item.newOrReusedRoomIndex].Add(item);

        for (int i = 0; i < numRooms; i++)
        {
            __instance.currentDistrictGroup.Clear();
            __instance.currentDistrictGroup.AddRange(groups[i]);

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
        return false;
    }
}
