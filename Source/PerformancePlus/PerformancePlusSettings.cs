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
        Header("Standard", "Original simulation behavior • On by default");
        Row("Path output memory", ref PathMemory, "Reduces reserved path memory while allowing longer paths to grow normally.");
        Row("Sparse gas processing", ref SparseGas, "Reduces processing when gas occupies only a small part of a map.");
        Row("World dispatch", ref WorldDispatch, "Reduces world-object dispatch work while preserving scheduled callbacks.");
        Row("Reservation indexing", ref ReservationIndex, "Speeds up searches for existing reservations.");
        Row("Melee weight reuse", ref MeleeWeights, "Reuses calculations within one melee selection refresh.");
        Row("Psychic entropy reuse", ref EntropyLocal, "Reuses calculations within one psychic heat evaluation.");
        listing.Gap(24f);
        Header("Experimental", "May affect outcomes • Off by default");
        Row("Colonist update delay", ref ColonyCadence,
            "Sets a 60-tick minimum interval for spawned, undrafted player colonists outside mental states. Uses the game's elapsed-tick handling, but can delay health, AI and work decisions by up to one game second. Drafted, animal and non-player pawns retain native intervals.");
        Text.Font = GameFont.Tiny;
        listing.Label("Small delay between updates (60 ticks)");
        listing.Gap(24f);
        var originalColor = GUI.color;
        GUI.color = Color.gray;
        listing.Label("NOTICE: This mod is a WIP made for the constantly updating 1.7, so any one of these optimizations could become obsolete at any point.");
        GUI.color = originalColor;
        Text.Font = GameFont.Small;
        if (RestartRequired)
        {
            listing.Gap();
            var color = GUI.color;
            GUI.color = Color.red;
            listing.Label("game must restart for changes to take effect");
            GUI.color = color;
        }
        listing.End();

        void Header(string title, string description)
        {
            Text.Font = GameFont.Medium;
            listing.Label(title);
            Text.Font = GameFont.Tiny;
            var color = GUI.color;
            GUI.color = Color.gray;
            listing.Label(description);
            GUI.color = color;
            Text.Font = GameFont.Small;
            listing.GapLine(8f);
        }

        void Row(string label, ref bool enabled, string tooltip)
        {
            var rect = listing.GetRect(36f);
            if (label is "Path output memory" or "World dispatch" or "Melee weight reuse" or "Colonist update delay")
                Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.025f));
            rect.xMin += 8f;
            rect.xMax -= 8f;
            Widgets.CheckboxLabeled(rect, label, ref enabled);
            TooltipHandler.TipRegion(rect, tooltip);
        }
    }
}
