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
            try { patch(new Harmony(PackageId + "." + name)); }
            catch
            {
                foreach (var feature in AppliedFeatures.Append(name))
                {
                    var id = PackageId + "." + feature;
                    new Harmony(id).UnpatchAll(id);
                }
                AppliedFeatures.Clear();
                throw;
            }
            if (name != "combat-line" || !Combat.CombatLine.CompatibilityBlocked)
                AppliedFeatures.Add(name);
        }
    }
}
