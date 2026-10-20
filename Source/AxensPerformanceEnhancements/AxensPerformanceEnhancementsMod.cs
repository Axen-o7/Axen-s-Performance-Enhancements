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
        Apply("melee-weights", Combat.MeleeWeights.Apply);
        Apply("entropy-local", Psychic.EntropyReuse.Apply);
        Apply("combat-line", Experimental.CombatLine.Apply);
        Apply("glow-buffers", Experimental.GlowBuffers.Apply);
        Apply("cloud-zero", Experimental.CloudZero.Apply);
        Apply("camera-registry", Experimental.CameraRegistry.Apply);
        Apply("keyword-key", Experimental.KeywordKey.Apply);
        Apply("cover-pass", Experimental.CoverPass.Apply);
        Apply("path-dag", Experimental.PathDag.Apply);
        Apply("vacuum-bfs", Experimental.VacuumBfs.Apply);
        Apply("room-groups", Experimental.RoomGroups.Apply);
        Apply("slaughter-cursors", Experimental.SlaughterCursors.Apply);
        Apply("lazy-tooltip", Experimental.LazyTooltip.Apply);
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
