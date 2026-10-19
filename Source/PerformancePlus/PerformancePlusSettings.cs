// shared optimization settings and interface.
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
    public bool CombatLine;
    public bool GlowBuffers;
    public bool CloudZero;
    public bool CameraRegistry;
    public bool KeywordKey;
    public bool CoverPass;
    public bool PathDag;
    public bool VacuumBfs;
    public bool RoomGroups;
    public bool SlaughterCursors;
    public bool LazyTooltip;

    private Vector2 scrollPosition;
    private float contentHeight;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref PathMemory, "pathMemory", true);
        Scribe_Values.Look(ref SparseGas, "sparseGas", true);
        Scribe_Values.Look(ref WorldDispatch, "worldDispatch", true);
        Scribe_Values.Look(ref ReservationIndex, "reservationIndex", true);
        Scribe_Values.Look(ref MeleeWeights, "meleeWeights", true);
        Scribe_Values.Look(ref EntropyLocal, "entropyLocal", true);
        Scribe_Values.Look(ref CombatLine, "combatLine", false);
        Scribe_Values.Look(ref GlowBuffers, "glowBuffers", false);
        Scribe_Values.Look(ref CloudZero, "cloudZero", false);
        Scribe_Values.Look(ref CameraRegistry, "cameraRegistry", false);
        Scribe_Values.Look(ref KeywordKey, "keywordKey", false);
        Scribe_Values.Look(ref CoverPass, "coverPass", false);
        Scribe_Values.Look(ref PathDag, "pathDag", false);
        Scribe_Values.Look(ref VacuumBfs, "vacuumBfs", false);
        Scribe_Values.Look(ref RoomGroups, "roomGroups", false);
        Scribe_Values.Look(ref SlaughterCursors, "slaughterCursors", false);
        Scribe_Values.Look(ref LazyTooltip, "lazyTooltip", false);
    }

    internal bool Enabled(string feature) => feature switch {
        "path-memory" => PathMemory, "sparse-gas" => SparseGas, "world-dispatch" => WorldDispatch,
        "reservation-index" => ReservationIndex, "melee-weights" => MeleeWeights, "entropy-local" => EntropyLocal,
        "combat-line" => CombatLine,
        "glow-buffers" => GlowBuffers,
        "cloud-zero" => CloudZero,
        "camera-registry" => CameraRegistry,
        "keyword-key" => KeywordKey,
        "cover-pass" => CoverPass,
        "path-dag" => PathDag,
        "vacuum-bfs" => VacuumBfs,
        "room-groups" => RoomGroups,
        "slaughter-cursors" => SlaughterCursors,
        "lazy-tooltip" => LazyTooltip,
        _ => throw new ArgumentException("Unknown optimization: " + feature)
    };

    internal bool RestartRequired =>
        PathMemory != PerformancePlusMod.AppliedFeatures.Contains("path-memory") ||
        SparseGas != PerformancePlusMod.AppliedFeatures.Contains("sparse-gas") ||
        WorldDispatch != PerformancePlusMod.AppliedFeatures.Contains("world-dispatch") ||
        ReservationIndex != PerformancePlusMod.AppliedFeatures.Contains("reservation-index") ||
        MeleeWeights != PerformancePlusMod.AppliedFeatures.Contains("melee-weights") ||
        EntropyLocal != PerformancePlusMod.AppliedFeatures.Contains("entropy-local") ||
        CombatLine != PerformancePlusMod.AppliedFeatures.Contains("combat-line") ||
        GlowBuffers != PerformancePlusMod.AppliedFeatures.Contains("glow-buffers") ||
        CloudZero != PerformancePlusMod.AppliedFeatures.Contains("cloud-zero") ||
        CameraRegistry != PerformancePlusMod.AppliedFeatures.Contains("camera-registry") ||
        KeywordKey != PerformancePlusMod.AppliedFeatures.Contains("keyword-key") ||
        CoverPass != PerformancePlusMod.AppliedFeatures.Contains("cover-pass") ||
        PathDag != PerformancePlusMod.AppliedFeatures.Contains("path-dag") ||
        VacuumBfs != PerformancePlusMod.AppliedFeatures.Contains("vacuum-bfs") ||
        RoomGroups != PerformancePlusMod.AppliedFeatures.Contains("room-groups") ||
        SlaughterCursors != PerformancePlusMod.AppliedFeatures.Contains("slaughter-cursors") ||
        LazyTooltip != PerformancePlusMod.AppliedFeatures.Contains("lazy-tooltip");

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
        Row("Melee weight reuse", ref MeleeWeights, "Reuses calculations within one melee selection refresh.");
        Row("Psychic entropy reuse", ref EntropyLocal, "[Royalty] Reuses calculations within one psychic heat evaluation.");
        listing.Gap(24f);
        Header("Experimental", "Off by default");
        Row("Combat line calculation", ref CombatLine,
            "Uses only the shooting line during friendly-fire scoring, avoiding a full shot report and its stat-cache refreshes.");
        Row("Lighting buffer reuse", ref GlowBuffers,
            "Sizes native lighting buffers to each light and reuses available pool slots.");
        Row("Invisible cloud rendering", ref CloudZero,
            "Skips cloud rendering only at exactly zero current and target opacity; keeps cloud fades.");
        Row("Camera lookup indexing", ref CameraRegistry,
            "Keeps a per-world camera index, invalidated when cameras are registered or destroyed. Other object searches use the original engine.");
        Row("Shader keyword reuse", ref KeywordKey,
            "Reuses combined material and global shader keyword keys until either changes.");
        Row("Cover calculation reuse", ref CoverPass,
            "Collects cover and calculates its blocking chance in one pass when creating a shot report.");
        Row("Parallel path grids", ref PathDag,
            "Allows independent path-grid jobs to overlap. Requests with customizers or lord grids use the original scheduling.");
        Row("Vacuum room traversal", ref VacuumBfs,
            "[Odyssey] Avoids repeated frontier searches while finding rooms exposed to vacuum.");
        Row("Room grouping", ref RoomGroups,
            "Groups new regions and districts once during room rebuilding instead of repeatedly scanning them.");
        Row("Slaughter selection cursors", ref SlaughterCursors,
            "Selects animals without repeatedly removing entries from the front of temporary lists. Keeps existing limits and selection order.");
        Row("Lazy mechanitor tooltip", ref LazyTooltip,
            "[Biotech] Builds the mechanitor bandwidth tooltip when it is requested instead of every draw.");
        listing.Gap(24f);
        Text.Font = GameFont.Tiny;
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
