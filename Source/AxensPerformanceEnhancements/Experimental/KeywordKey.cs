// shader keyword reuse.
using System.Runtime.CompilerServices;
using HarmonyLib;
using Waylith.Rendering;
namespace AxensPerformanceEnhancements.Experimental;

internal static class KeywordKey
{
    private sealed record Value(string Local, string Global, string Combined, long Id);
    private sealed class Cache { internal Value? Value; }
    private static readonly ConditionalWeakTable<Material, Cache> Caches = new();
    internal static void Apply(Harmony h) => h.Patch(AccessTools.Method(typeof(MaterialBindingResolver), "CreateShaderVariant"), prefix: new HarmonyMethod(typeof(KeywordKey), nameof(Before)));
    private static bool Before(Material material, string globalKeywordKey, ref ShaderVariantKey __result)
    {
        material.GetKeywordState(out var local, out var localId);
        var cache = Caches.GetValue(material, static _ => new Cache());
        var value = Volatile.Read(ref cache.Value);
        if (value == null || value.Local != local || value.Global != globalKeywordKey)
        {
            var combined = MaterialBindingResolver.CombineKeywordKeys(local, globalKeywordKey);
            value = new Value(local, globalKeywordKey, combined, combined == local ? localId : ShaderKeywordIdMaker.GetOrCreateId(combined));
            Volatile.Write(ref cache.Value, value);
        }
        __result = new ShaderVariantKey(material.Shader, value.Id, value.Combined, material.Shader.UseTexture);
        return false;
    }
}
