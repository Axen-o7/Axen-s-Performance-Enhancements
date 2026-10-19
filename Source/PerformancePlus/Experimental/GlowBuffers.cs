// lighting buffer reuse.
using System.Runtime.CompilerServices;
using HarmonyLib;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Verse;
using Verse.Glow;
namespace PerformancePlus.Experimental;

internal static class GlowBuffers
{
    private static readonly ConditionalWeakTable<GlowGrid.GlowPool, Stack<int>> Free = new();
    internal static void Apply(Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(LocalGlowArea), nameof(LocalGlowArea.AllocateNew)), prefix: new HarmonyMethod(typeof(GlowBuffers), nameof(Allocate)));
        h.Patch(AccessTools.Method(typeof(ComputeGlowGridsJob), nameof(ComputeGlowGridsJob.Execute)), prefix: new HarmonyMethod(typeof(GlowBuffers), nameof(Size)));
        h.Patch(AccessTools.Method(typeof(GlowGrid.GlowPool), "Take"), prefix: new HarmonyMethod(typeof(GlowBuffers), nameof(Take)));
        h.Patch(AccessTools.Method(typeof(GlowGrid.GlowPool), "ReturnSet"), postfix: new HarmonyMethod(typeof(GlowBuffers), nameof(Returned)));
    }
    private static bool Allocate(ref LocalGlowArea __result)
    {
        __result = new LocalGlowArea { colors = new UnsafeList<Color32>(1, Allocator.Persistent) };
        __result.colors.Resize(1, NativeArrayOptions.ClearMemory);
        return false;
    }
    private static void Size(ref ComputeGlowGridsJob __instance, int index)
    {
        var light = __instance.lights[index];
        if (!light.dirty) return;
        var area = __instance.glowPool[light.localGlowPoolIndex];
        int length = checked(light.diameter * light.diameter);
        if (length <= 0 || length > GlowGrid.MaxLightCells) throw new InvalidOperationException("Unsupported light diameter.");
        int capacity = 4;
        while (capacity < length) capacity *= 2;
        if (area.colors.Capacity > capacity * 2)
        { area.colors.Dispose(); area.colors = new UnsafeList<Color32>(length, Allocator.Persistent); }
        area.colors.Resize(length, NativeArrayOptions.ClearMemory);
        __instance.glowPool[light.localGlowPoolIndex] = area;
    }
    private static Stack<int> Slots(GlowGrid.GlowPool pool) => Free.GetValue(pool, static p =>
    {
        var free = new Stack<int>();
        for (int i = p.pool.Length - 1; i >= 0; i--) if (!p.pool[i].inUse) free.Push(i);
        return free;
    });
    private static bool Take(GlowGrid.GlowPool __instance, ref int __result)
    {
        var free = Slots(__instance);
        if (free.TryPop(out var index))
        {
            var area = __instance.pool[index]; area.Take(); __instance.pool[index] = area; __result = index;
        }
        else
        {
            var area = LocalGlowArea.AllocateNew(); area.Take(); __instance.pool.Add(in area); __result = __instance.pool.Length - 1;
        }
        return false;
    }
    private static void Returned(GlowGrid.GlowPool __instance, int index) => Slots(__instance).Push(index);
}
