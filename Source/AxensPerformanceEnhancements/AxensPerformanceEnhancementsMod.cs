// shared optimization startup and patch registration.
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AxensPerformanceEnhancements;

public sealed class AxensPerformanceEnhancementsMod : Mod
{
    private readonly AxensPerformanceEnhancementsSettings settings;
    public override string SettingsCategory() => "Axen's Performance Enhancements";
    public override void DoSettingsWindowContents(Rect inRect) => settings.Draw(inRect);
    public const string PackageId = "axeno7.performanceplus";
    public static readonly List<string> AppliedFeatures = [];
    // Features enabled at startup, whether or not they could be applied.
    internal static readonly List<string> RequestedFeatures = [];
    public static bool InitializationComplete;
    public static object AlgorithmParameters() => new { pathInitialNodes = Pathfinding.PathOutputMemory.InitialNodes };

    public AxensPerformanceEnhancementsMod(ModContentPack content) : base(content)
    {
        settings = GetSettings<AxensPerformanceEnhancementsSettings>();
        Apply("path-memory", Pathfinding.PathOutputMemory.Apply);
        Apply("sparse-gas", Simulation.SparseGas.Apply);
        Apply("world-dispatch", World.WorldTickDispatch.Apply);
        Apply("reservation-index", AI.ReservationQueries.Apply);
        Apply("entropy-local", Psychic.EntropyReuse.Apply);
        Apply("combat-line", Combat.CombatLine.Apply);
        Apply("camera-registry", Rendering.CameraRegistry.Apply);
        Apply("path-dag", Pathfinding.PathDag.Apply);
        Apply("room-groups", Regions.RoomGroups.Apply);
        InitializationComplete = true;
        Log.Message($"[Axen's Performance Enhancements] Enabled: {string.Join(", ", AppliedFeatures)}");

        void Apply(string name, Action<Harmony> patch)
        {
            if (!settings.Enabled(name)) return;
            RequestedFeatures.Add(name);
            var id = PackageId + "." + name;
            try { patch(new Harmony(id)); }
            catch (Exception ex)
            {
                // Isolate the failure so other features and the settings page stay available.
                new Harmony(id).UnpatchAll(id);
                Log.Error($"[Axen's Performance Enhancements] {name} disabled: failed to apply. {ex}");
                return;
            }
            if (name != "combat-line" || !Combat.CombatLine.CompatibilityBlocked)
                AppliedFeatures.Add(name);
        }
    }
}
