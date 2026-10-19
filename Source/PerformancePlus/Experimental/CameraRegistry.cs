// camera lookup indexing.
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Waylith.UnityCompat;
namespace PerformancePlus.Experimental;

internal static class CameraRegistry
{
    private sealed class State { internal UnityWorld.UnityComponentState[]? Cameras; }
    private static readonly ConditionalWeakTable<UnityWorld, State> Worlds = new();
    internal static void Apply(Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(UnityWorld), "RegisterComponent"), postfix: new HarmonyMethod(typeof(CameraRegistry), nameof(Registered)));
        h.Patch(AccessTools.Method(typeof(UnityWorld), "DestroyNow", [typeof(UnityWorld.UnityComponentState)]), postfix: new HarmonyMethod(typeof(CameraRegistry), nameof(Destroyed)));
        h.Patch(AccessTools.Method(typeof(UnityWorld), nameof(UnityWorld.FindObjectsByType)), prefix: new HarmonyMethod(typeof(CameraRegistry), nameof(Find)));
    }
    private static void Registered(UnityWorld __instance, UnityWorld.UnityComponentState componentState)
    { if (componentState.Instance is Camera) Worlds.GetValue(__instance, static _ => new State()).Cameras = null; }
    private static void Destroyed(UnityWorld __instance, UnityWorld.UnityComponentState component)
    { if (component.Instance is Camera) Worlds.GetValue(__instance, static _ => new State()).Cameras = null; }
    private static bool Find(UnityWorld __instance, Type requestedType, bool includeInactive, ref Array __result)
    {
        if (requestedType != typeof(Camera)) return true;
        using (__instance._gate.EnterScope())
        {
            var state = Worlds.GetValue(__instance, static _ => new State());
            var cameras = state.Cameras ??= __instance._objects.Values.OfType<UnityWorld.UnityComponentState>().Where(c => c.Instance is Camera && !c.GameObject.IsAssetToken).ToArray();
            int count = 0;
            foreach (var camera in cameras) if (!camera.Destroyed && (includeInactive || __instance.IsComponentVisibleForFind(camera))) count++;
            var result = new Camera[count];
            int i = 0;
            foreach (var camera in cameras) if (!camera.Destroyed && (includeInactive || __instance.IsComponentVisibleForFind(camera))) result[i++] = (Camera)camera.Instance;
            __result = result;
        }
        return false;
    }
}
