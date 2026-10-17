using HarmonyLib;
using UnityEngine;
using Verse;

namespace PerformancePlus;

public sealed class PerformancePlusMod : Mod
{
    private readonly PerformancePlusSettings settings;
    public override string SettingsCategory() => "Axen's Performance Enhancements";
    public override void DoSettingsWindowContents(Rect inRect) => settings.Draw(inRect);
    public const string PackageId = "axeno7.performanceplus";
    public static readonly List<string> AppliedFeatures = [];
    public static bool InitializationComplete;
    public static object AlgorithmParameters() => new { pathInitialNodes = Pathfinding.PathOutputMemory.InitialNodes };

    public PerformancePlusMod(ModContentPack content) : base(content)
    {
        settings = GetSettings<PerformancePlusSettings>();
        Apply("path-memory", Pathfinding.PathOutputMemory.Apply);
        Apply("sparse-gas", Simulation.SparseGas.Apply);
        Apply("world-dispatch", World.WorldTickDispatch.Apply);
        Apply("reservation-index", AI.ReservationQueries.Apply);
        Apply("melee-weights", Combat.MeleeWeights.Apply);
        Apply("entropy-local", Psychic.EntropyReuse.Apply);
        Apply("colony-cadence", Experimental.ColonyCadence.Apply);
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
            AppliedFeatures.Add(name);
        }
    }
}
