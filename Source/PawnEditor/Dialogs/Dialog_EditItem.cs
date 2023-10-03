using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class Dialog_EditItem : Window
{
    public override Vector2 InitialSize => new(790f, 160f);

    private readonly Vector2 _position;
    public static Thing SelectedThing;
    private readonly Pawn _pawn;
    private string _buffer = "";

    public Dialog_EditItem(Vector2 absPosition, Pawn pawn, Thing item)
    {
        onlyOneOfTypeAllowed = true;

        _position = absPosition;
        _pawn = pawn;
        SelectedThing = item;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        windowRect.x = _position.x - 4f;
        windowRect.y = _position.y - InitialSize.y;
        _buffer = SelectedThing.stackCount.ToString();
    }
    
    public override void PostClose()
    {
        base.PostClose();
        _buffer = "";
    }

    public override void DoWindowContents(Rect inRect)
    {
        const float labelWidthPct = 0.3f;
        if (SelectedThing == null) return;

        var cellCount = 0;
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, null))
        {
            // Stuff
            if (SelectedThing.def.stuffCategories is { Count: > 1 })
            {
                Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "StatsReport_Material".Translate());
                var options = new List<FloatMenuOption>();
                foreach (var stuff in GenStuff.AllowedStuffsFor(SelectedThing.def))
                    options.Add(new(stuff.LabelCap, () =>
                    {
                        SelectedThing.SetStuffDirect(stuff);
                        SelectedThing.Notify_ColorChanged();
                    }, Widgets.GetIconFor(stuff), stuff.uiIconColor));

                if (UIUtility.ButtonTextImage(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), SelectedThing.Stuff))
                    Find.WindowStack.Add(new FloatMenu(options));
                cellCount++;
            }


            // Quality
            if (SelectedThing.TryGetComp<CompQuality>() != null)
            {
                Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "Quality".Translate());
                var compQuality = SelectedThing.TryGetComp<CompQuality>();
                var buttonLabel = compQuality.Quality.GetLabel().CapitalizeFirst();

                var options3 = new List<FloatMenuOption>();
                foreach (var quality in QualityUtility.AllQualityCategories)
                    options3.Add(new(quality.GetLabel().CapitalizeFirst(), () =>
                    {
                        buttonLabel = quality.GetLabel().CapitalizeFirst();
                        compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
                    }));

                if (Widgets.ButtonText(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), buttonLabel))
                    Find.WindowStack.Add(new FloatMenu(options3));
                cellCount++;
            }


            // Color
            if (SelectedThing is Apparel apparel2 && SelectedThing.TryGetComp<CompColorable>() != null)
            {
                Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "Color".Translate());
                var widgetRect = UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct);
                var colorRect = widgetRect.TakeRightPart(24f);
                widgetRect.xMax -= 4f;
                colorRect.height = 24f;
                colorRect.y += 3f;
                var curColor = apparel2.GetComp<CompColorable>().color;
                curColor = curColor == Color.white ? apparel2.Stuff.stuffProps.color : curColor;

                if (Widgets.ButtonText(widgetRect, "PawnEditor.PickColor".Translate()))
                    Find.WindowStack.Add(new Dialog_ColorPicker(color => apparel2.SetColor(color),
                        DefDatabase<ColorDef>.AllDefs.Select(cd => cd.color).ToList(), curColor, apparel2.Stuff.stuffProps.color,
                        _pawn.story.favoriteColor));

                Widgets.DrawRectFast(colorRect, curColor);
                cellCount++;
            }


            // Style
            if (Dialog_SelectItem.thingStyles.Select(ts => ts.ThingDef).Contains(SelectedThing.def))
            {
                Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "Stat_Thing_StyleLabel".Translate());
                List<FloatMenuOption> options2 = new();
                var styleOptions = Dialog_SelectItem.thingStyles.FirstOrDefault(ts => ts.ThingDef == SelectedThing.def).StyleDefs;
                foreach (var style in styleOptions)
                    options2.Add(new(style.Value.LabelCap, () =>
                    {
                        SelectedThing.SetStyleDef(style.Key);
                        SelectedThing.Notify_ColorChanged();
                    }, style.Value.Icon, Color.white));

                options2.Add(new("None", () =>
                {
                    SelectedThing.SetStyleDef(null);
                    SelectedThing.Notify_ColorChanged();
                }));

                if (UIUtility.ButtonTextImage(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct),
                        styleOptions.FirstOrDefault(so => so.Key == SelectedThing.GetStyleDef()).Value)) Find.WindowStack.Add(new FloatMenu(options2));
                cellCount++;
            }


            // Hit Points
            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "HitPointsBasic".Translate().CapitalizeFirst());
            float hitPoints = SelectedThing.HitPoints;
            var widgetRect2 = UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct);
            Widgets.HorizontalSlider(widgetRect2, ref hitPoints, new(0, SelectedThing.MaxHitPoints), hitPoints.ToString());
            SelectedThing.HitPoints = (int)hitPoints;
            cellCount++;

            // Tainted
            if (SelectedThing is Apparel apparel)
            {
                Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "PawnEditor.Tainted".Translate());
                var widgetRect = UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct);
                var isTainted = apparel.WornByCorpse;
                Widgets.Checkbox(new(widgetRect.x + (widgetRect.width - Widgets.CheckboxSize) / 2, widgetRect.y + 3f), ref isTainted);
                apparel.wornByCorpseInt = isTainted;
                cellCount++;
            }

            // Count
            if (SelectedThing.def.stackLimit > 1)
            {
                Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "PenFoodTab_Count".Translate());
                var widgetRect = UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct);
                UIUtility.IntField(widgetRect, ref SelectedThing.stackCount, 1, SelectedThing.def.stackLimit, ref _buffer, true);
                cellCount++;
            }
        }

        float newHeight = (float)Math.Ceiling(cellCount / 2f) * 38f + 24f;
        windowRect.y += windowRect.height - newHeight;
        windowRect.height = newHeight;
    }
}