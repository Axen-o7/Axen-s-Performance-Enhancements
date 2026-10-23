// shared optimization settings and interface.
using UnityEngine;
using Verse;

namespace AxensPerformanceEnhancements;

public sealed class AxensPerformanceEnhancementsSettings : ModSettings
{
    public bool PathMemory = true;
    public bool SparseGas = true;
    public bool WorldDispatch = true;
    public bool ReservationIndex = true;
    public bool EntropyLocal = true;
    public bool CombatLine = true;
    public bool CameraRegistry = true;
    public bool PathDag = true;
    public bool RoomGroups = true;

    private bool promotedDefaultsApplied = true;
    private Vector2 scrollPosition;
    private float contentHeight;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref PathMemory, "pathMemory", true);
        Scribe_Values.Look(ref SparseGas, "sparseGas", true);
        Scribe_Values.Look(ref WorldDispatch, "worldDispatch", true);
        Scribe_Values.Look(ref ReservationIndex, "reservationIndex", true);
        Scribe_Values.Look(ref EntropyLocal, "entropyLocal", true);
        Scribe_Values.Look(ref CombatLine, "combatLine", true);
        Scribe_Values.Look(ref CameraRegistry, "cameraRegistry", true);
        Scribe_Values.Look(ref PathDag, "pathDag", true);
        Scribe_Values.Look(ref RoomGroups, "roomGroups", true);
        Scribe_Values.Look(ref promotedDefaultsApplied, "promotedDefaultsApplied", false);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && !promotedDefaultsApplied)
        {
            CombatLine = CameraRegistry = PathDag = RoomGroups = true;
            promotedDefaultsApplied = true;
        }
    }

    internal bool Enabled(string feature) => feature switch {
        "path-memory" => PathMemory, "sparse-gas" => SparseGas, "world-dispatch" => WorldDispatch,
        "reservation-index" => ReservationIndex, "entropy-local" => EntropyLocal,
        "combat-line" => CombatLine,
        "camera-registry" => CameraRegistry,
        "path-dag" => PathDag,
        "room-groups" => RoomGroups,
        _ => throw new ArgumentException("Unknown optimization: " + feature)
    };

    internal bool RestartRequired =>
        PathMemory != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("path-memory") ||
        SparseGas != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("sparse-gas") ||
        WorldDispatch != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("world-dispatch") ||
        ReservationIndex != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("reservation-index") ||
        EntropyLocal != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("entropy-local") ||
        CombatLine != Combat.CombatLine.Requested ||
        CameraRegistry != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("camera-registry") ||
        PathDag != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("path-dag") ||
        RoomGroups != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("room-groups");

    internal void Draw(Rect area)
    {
        var view = new Rect(0f, 0f, area.width - 20f, Mathf.Max(contentHeight, area.height));
        Widgets.BeginScrollView(area, ref scrollPosition, view);
        var listing = new Listing_Standard(); listing.Begin(new Rect(0f, 0f, view.width, 10000f));
        var rowIndex = 0;
        Header("Standard", "On by default");
        Row("Path output memory", ref PathMemory, "Reduces reserved path memory while allowing longer paths to grow normally.");
        Row("Sparse gas processing", ref SparseGas, "Reduces processing when gas occupies only a small part of a map.");
        Row("World dispatch", ref WorldDispatch, "Reduces world-object dispatch work while preserving scheduled callbacks.");
        Row("Reservation indexing", ref ReservationIndex, "Speeds up searches for existing reservations.");
        Row("Psychic entropy reuse", ref EntropyLocal, "[Royalty] Reuses calculations within one psychic heat evaluation.");
        Row("Combat line calculation", ref CombatLine,
            "Uses only the shooting line during friendly-fire scoring, avoiding a full shot report and its stat-cache refreshes.");
        Row("Camera lookup indexing", ref CameraRegistry,
            "Keeps a per-world camera index, invalidated when cameras are registered or destroyed. Other object searches use the original engine.");
        Row("Parallel path grids", ref PathDag,
            "Schedules independent path grids in parallel. Requests with custom grids or lord-specific behavior use the original scheduling.");
        Row("Room grouping", ref RoomGroups,
            "Groups regions and districts in one pass when rebuilding rooms.");
        listing.Gap(24f);
        Header("Experimental", "Off by default");
        listing.Label("Nothing to see here, everything’s stable.");
        listing.Gap(24f);
        Text.Font = GameFont.Tiny;
        var originalColor = GUI.color;
        GUI.color = Color.gray;
        listing.Label("NOTICE: RimWorld 1.7 changes frequently, so game updates may make individual optimizations obsolete.");
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
        contentHeight = listing.CurHeight + 8f;
        listing.End();
        Widgets.EndScrollView();

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
            if ((rowIndex++ & 1) == 0)
                Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.025f));
            rect.xMin += 8f;
            rect.xMax -= 8f;
            Widgets.CheckboxLabeled(rect, label, ref enabled);
            TooltipHandler.TipRegion(rect, tooltip);
        }
    }
}
