using UnityEngine;
using Verse;

namespace PerformancePlus;

public sealed class PerformancePlusSettings : ModSettings
{
    public bool PathMemory = true;
    public bool SparseGas = true;
    public bool WorldDispatch = true;
    public bool ReservationIndex = true;
    public bool MeleeWeights = true;
    public bool EntropyLocal = true;
    public bool ColonyCadence;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref PathMemory, "pathMemory", true);
        Scribe_Values.Look(ref SparseGas, "sparseGas", true);
        Scribe_Values.Look(ref WorldDispatch, "worldDispatch", true);
        Scribe_Values.Look(ref ReservationIndex, "reservationIndex", true);
        Scribe_Values.Look(ref MeleeWeights, "meleeWeights", true);
        Scribe_Values.Look(ref EntropyLocal, "entropyLocal", true);
        Scribe_Values.Look(ref ColonyCadence, "colonyCadence", false);
    }

    internal bool Enabled(string feature) => feature switch {
        "path-memory" => PathMemory, "sparse-gas" => SparseGas, "world-dispatch" => WorldDispatch,
        "reservation-index" => ReservationIndex, "melee-weights" => MeleeWeights, "entropy-local" => EntropyLocal,
        "colony-cadence" => ColonyCadence,
        _ => throw new ArgumentException("Unknown optimization: " + feature)
    };

    internal bool RestartRequired =>
        PathMemory != PerformancePlusMod.AppliedFeatures.Contains("path-memory") ||
        SparseGas != PerformancePlusMod.AppliedFeatures.Contains("sparse-gas") ||
        WorldDispatch != PerformancePlusMod.AppliedFeatures.Contains("world-dispatch") ||
        ReservationIndex != PerformancePlusMod.AppliedFeatures.Contains("reservation-index") ||
        MeleeWeights != PerformancePlusMod.AppliedFeatures.Contains("melee-weights") ||
        EntropyLocal != PerformancePlusMod.AppliedFeatures.Contains("entropy-local") ||
        ColonyCadence != PerformancePlusMod.AppliedFeatures.Contains("colony-cadence");

    internal void Draw(Rect area)
    {
        var listing = new Listing_Standard(); listing.Begin(area);
        listing.Label("Preserves vanilla simulation"); listing.GapLine();
        listing.Label("Designed to preserve vanilla results and update timing. Enabled by default.");
        listing.CheckboxLabeled("Path output memory reduction", ref PathMemory, "Reduces reserved path memory while allowing longer paths to grow normally.");
        listing.CheckboxLabeled("Sparse gas processing", ref SparseGas, "Reduces processing when gas occupies only a small part of a map.");
        listing.CheckboxLabeled("World dispatch", ref WorldDispatch, "Reduces world-object dispatch work while preserving scheduled callbacks.");
        listing.CheckboxLabeled("Reservation indexing", ref ReservationIndex, "Speeds up searches for existing reservations.");
        listing.CheckboxLabeled("Melee weight reuse", ref MeleeWeights, "Reuses calculations within one melee selection refresh.");
        listing.CheckboxLabeled("Psychic entropy reuse", ref EntropyLocal, "Reuses calculations within one psychic heat evaluation.");
        listing.Gap(); listing.Label("Changes vanilla simulation"); listing.GapLine();
        listing.Label("These options change update timing and can affect outcomes. Off by default.");
        listing.CheckboxLabeled("Small delay between colonist updates (60 ticks)", ref ColonyCadence,
            "Sets a 60-tick minimum interval for spawned, undrafted player colonists outside mental states. Uses the game's elapsed-tick handling, but can delay health, AI and work decisions by up to one game second. Drafted, animal and non-player pawns retain native intervals.");
        listing.Gap();
        listing.Label("NOTICE: This mod is a WIP made for the constantly updating 1.7, so any one of these optimizations could become obsolete at any point.");
        if (RestartRequired)
        {
            listing.Gap();
            var color = GUI.color;
            GUI.color = Color.red;
            listing.Label("game must restart for changes to take effect");
            GUI.color = color;
        }
        listing.End();
    }
}
