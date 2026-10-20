// invisible cloud rendering.
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;
namespace AxensPerformanceEnhancements.Experimental;

internal static class CloudZero
{
    internal static void Apply(Harmony h) => h.Patch(AccessTools.Method(typeof(WorldDrawLayer_Clouds), nameof(WorldDrawLayer_Clouds.Render)), prefix: new HarmonyMethod(typeof(CloudZero), nameof(Before)));
    private static bool Before(WorldDrawLayer_Clouds __instance)
    {
        if (__instance.GetTargetOpacity() != 0f || (__instance.set && (__instance.opacity != 0f || __instance.opacityVel != 0f))) return true;
        __instance.opacity = 0f;
        __instance.set = true;
        __instance.material.SetFloat(ShaderPropertyIDs.CloudShaderOpacity, 0f);
        return false;
    }
}
