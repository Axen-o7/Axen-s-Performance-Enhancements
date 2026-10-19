// lazy mechanitor bandwidth tooltip.
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using static RimWorld.MechanitorBandwidthGizmo;
namespace PerformancePlus.Experimental;

internal static class LazyTooltip
{
    internal static void Apply(Harmony h) => h.Patch(AccessTools.Method(typeof(MechanitorBandwidthGizmo), nameof(MechanitorBandwidthGizmo.GizmoOnGUI)), prefix: new HarmonyMethod(typeof(LazyTooltip), nameof(Before)));
    private static bool Before(MechanitorBandwidthGizmo __instance, Vector2 topLeft, float maxWidth, GizmoRenderParms parms, ref GizmoResult __result)
    {
        if (!ModLister.CheckBiotech("Mechanitor bandwidth gizmo"))
        {
            __result = new GizmoResult(GizmoState.Clear); return false;
        }
        Rect rect = new Rect(topLeft.x, topLeft.y, __instance.GetWidth(maxWidth), 75f);
        Rect rect2 = rect.ContractedBy(6f);
        Widgets.DrawWindowBackground(rect);
        int totalBandwidth = __instance.tracker.TotalBandwidth;
        int usedBandwidth = __instance.tracker.UsedBandwidth;
        string text = usedBandwidth.ToString("F0") + " / " + totalBandwidth.ToString("F0");
        TooltipHandler.TipRegion(rect, new TipSignal(() => BuildTip(__instance.tracker, text), __instance.tracker.Pawn.thingIDNumber ^ 0x42a91));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, 20f);
        Widgets.Label(rect3, "Bandwidth".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperRight;
        Widgets.Label(rect3, text);
        Text.Anchor = TextAnchor.UpperLeft;
        int num = Mathf.Max(usedBandwidth, totalBandwidth);
        Rect rect4 = new Rect(rect2.x, rect3.yMax + 6f, rect2.width, rect2.height - rect3.height - 6f);
        int num2 = 2;
        int num3 = Mathf.FloorToInt(rect4.height / (float)num2);
        int num4 = Mathf.FloorToInt(rect4.width / (float)num3);
        int num5 = 0;
        while (num2 * num4 < num)
        {
            num2++;
            num3 = Mathf.FloorToInt(rect4.height / (float)num2);
            num4 = Mathf.FloorToInt(rect4.width / (float)num3);
            num5++;
            if (num5 >= 1000)
            {
                Log.Error("Failed to fit bandwidth cells into gizmo rect.");
                __result = new GizmoResult(GizmoState.Clear); return false;
            }
        }
        int num6 = Mathf.FloorToInt(rect4.width / (float)num3);
        int num7 = num2;
        float num8 = (rect4.width - (float)(num6 * num3)) / 2f;
        int num9 = 0;
        int usedBandwidthFromGestation2 = __instance.tracker.UsedBandwidthFromGestation;
        int num10 = ((num7 <= 2) ? 4 : 2);
        for (int num11 = 0; num11 < num7; num11++)
        {
            for (int num12 = 0; num12 < num6; num12++)
            {
                num9++;
                Rect rect5 = new Rect(rect4.x + (float)(num12 * num3) + num8, rect4.y + (float)(num11 * num3), num3, num3).ContractedBy(2f);
                if (num9 <= num)
                {
                    if (num9 <= usedBandwidthFromGestation2)
                    {
                        Widgets.DrawRectFast(rect5, EmptyBlockColor);
                        Widgets.DrawRectFast(rect5.ContractedBy(num10), FilledBlockColor);
                    }
                    else if (num9 <= usedBandwidth)
                    {
                        Widgets.DrawRectFast(rect5, (num9 <= totalBandwidth) ? FilledBlockColor : ExcessBlockColor);
                    }
                    else
                    {
                        Widgets.DrawRectFast(rect5, EmptyBlockColor);
                    }
                }
            }
        }
        __result = new GizmoResult(GizmoState.Clear); return false;
    }
    private static string BuildTip(Pawn_MechanitorTracker tracker, string text)
    {

        TaggedString taggedString = "Bandwidth".Translate().Colorize(ColoredText.TipSectionTitleColor) + ": " + text + "\n\n" + "BandwidthGizmoTip".Translate();
        int usedBandwidthFromSubjects = tracker.UsedBandwidthFromSubjects;
        if (usedBandwidthFromSubjects > 0)
        {
            taggedString += string.Concat("\n\n" + ("BandwidthUsage".Translate() + ": "), usedBandwidthFromSubjects.ToString());
            IEnumerable<string> entries = from p in tracker.OverseenPawns
                                          where !p.IsGestating()
                                          group p by p.kindDef into p
                                          select string.Concat(new string[5]
                                          {
                    p.Key.LabelCap + " x",
                    p.Count().ToString(),
                    " (+",
                    p.Sum((Pawn mech) => mech.GetStatValue(StatDefOf.BandwidthCost)).ToString(),
                    ")"
                                          });
            taggedString += "\n\n" + entries.ToLineList(" - ");
        }
        int usedBandwidthFromGestation = tracker.UsedBandwidthFromGestation;
        if (usedBandwidthFromGestation > 0)
        {
            taggedString += string.Concat("\n\n" + "MechGestationBandwidthUsed".Translate() + ": ", usedBandwidthFromGestation.ToString());
            IEnumerable<string> entries2 = from p in tracker.OverseenPawns
                                           where p.IsGestating()
                                           group p by p.kindDef into p
                                           select string.Concat(new string[5]
                                           {
                    p.Key.LabelCap + " x",
                    p.Count().ToString(),
                    " (+",
                    p.Sum((Pawn mech) => mech.GetStatValue(StatDefOf.BandwidthCost)).ToString(),
                    ")"
                                           });
            taggedString += "\n\n" + entries2.ToLineList(" - ");
        }
        return taggedString.Resolve();
    }
}
