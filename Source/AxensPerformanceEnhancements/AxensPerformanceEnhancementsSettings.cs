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
    public bool MeleeWeights = true;
    public bool EntropyLocal = true;
    public bool CombatLine;
    public bool CameraRegistry;
    public bool KeywordKey;
    public bool SlaughterCursors;

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
        Scribe_Values.Look(ref CameraRegistry, "cameraRegistry", false);
        Scribe_Values.Look(ref KeywordKey, "keywordKey", false);
        Scribe_Values.Look(ref SlaughterCursors, "slaughterCursors", false);
    }

    internal bool Enabled(string feature) => feature switch {
        "path-memory" => PathMemory, "sparse-gas" => SparseGas, "world-dispatch" => WorldDispatch,
        "reservation-index" => ReservationIndex, "melee-weights" => MeleeWeights, "entropy-local" => EntropyLocal,
        "combat-line" => CombatLine,
        "camera-registry" => CameraRegistry,
        "keyword-key" => KeywordKey,
        "slaughter-cursors" => SlaughterCursors,
        _ => throw new ArgumentException("Unknown optimization: " + feature)
    };

    internal bool RestartRequired =>
        PathMemory != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("path-memory") ||
        SparseGas != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("sparse-gas") ||
        WorldDispatch != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("world-dispatch") ||
        ReservationIndex != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("reservation-index") ||
        MeleeWeights != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("melee-weights") ||
        EntropyLocal != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("entropy-local") ||
        CombatLine != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("combat-line") ||
        CameraRegistry != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("camera-registry") ||
        KeywordKey != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("keyword-key") ||
        SlaughterCursors != AxensPerformanceEnhancementsMod.AppliedFeatures.Contains("slaughter-cursors");

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
        Row("Camera lookup indexing", ref CameraRegistry,
            "Keeps a per-world camera index, invalidated when cameras are registered or destroyed. Other object searches use the original engine.");
        Row("Shader keyword reuse", ref KeywordKey,
            "Reuses combined material and global shader keyword keys until either changes.");
        Row("Slaughter selection cursors", ref SlaughterCursors,
            "Selects animals without repeatedly removing entries from the front of temporary lists. Keeps existing limits and selection order.");
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
