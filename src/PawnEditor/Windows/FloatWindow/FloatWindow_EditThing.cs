using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Layout;
using RimWorld;
using UnityEngine;
using Verse;
using L = PawnEditor.Layout.FlexLayoutHelper;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_EditThing(Rect boundWidgetRect, Thing thing) : FloatWindow(boundWidgetRect)
{
    protected override Window? Owner => Find.WindowStack.WindowOfType<Window_Editor>();
    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomRight;
    protected override bool UseWidgetWidth => true;
    private readonly Thing _thing = thing;

    public override void DoWindowContents(Rect inRect)
    {
        var layout = L.Row([
            // Stuff
            L.Cell(rect =>
            {
                if (UIUtility.ButtonTextLabeled_WithIcon(rect, "StatsReport_Material".Translate(),
                        _thing.Stuff.LabelCap, Verse.Widgets.GetIconFor(_thing.Stuff),
                        _thing.Stuff.stuffProps.color))
                {
                    Find.WindowStack.Add(new FloatMenu(GenStuff.AllowedStuffsFor(_thing.def)
                        .Select(stuff => new FloatMenuOption(stuff.LabelCap, () =>
                            {
                                _thing.SetStuffDirect(stuff);
                                _thing.SetColor(stuff.stuffProps.color);
                                _thing.Notify_ColorChanged();
                            },
                            Verse.Widgets.GetIconFor(stuff),
                            stuff.stuffProps.color
                        ))
                        .ToList()));
                }
            }).When(_thing.def.MadeFromStuff),
            // Quality
            L.Cell(rect =>
            {
                var compQuality = _thing.TryGetComp<CompQuality>();
                if (UIUtility.ButtonTextLabeled(rect, "Quality", compQuality.Quality.GetLabel().CapitalizeFirst()))
                {
                    Find.WindowStack.Add(new FloatMenu(QualityUtility.AllQualityCategories.Select(quality =>
                            new FloatMenuOption(quality.GetLabel().CapitalizeFirst(),
                                () => { compQuality.SetQuality(quality, ArtGenerationContext.Outsider); }))
                        .ToList()));
                }
            }).When(_thing.HasComp<CompQuality>()),
            // Color
            L.Cell(rect =>
            {
                var apparel = _thing as Apparel;
                var widgetRect = UIUtility.RectLabeled(rect, "Color");
                var colorRect = widgetRect.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize);
                var curColor = apparel?.DrawColor ?? Color.white;
                
                Verse.Widgets.DrawLightHighlight(colorRect);
                colorRect = colorRect.ContractedBy(2f);
                Verse.Widgets.DrawRectFast(colorRect, curColor);
                widgetRect.xMax -= 2f;
                if (Verse.Widgets.ButtonText(widgetRect, "Pick color"))
                {
                    Find.WindowStack.Add(new Dialog_ColorPicker(color => apparel.SetColor(color), curColor,
                        DefDatabase<ColorDef>.AllDefs.Select(cd => cd.color).ToList()));
                }
                
            }).When(_thing is Apparel && _thing.HasComp<CompColorable>()),
            // Style
            L.Cell(rect =>
            {
                var styleOptions = ThingUtility.ThingStyles.FirstOrDefault(ts => ts.thingDef == _thing.def).styleDefs;
                if (UIUtility.ButtonTextLabeled(rect, "Stat_Thing_StyleLabel".Translate(),
                        styleOptions.FirstOrDefault(so => so.Key == _thing.GetStyleDef()).Value.LabelCap))
                {
                    Find.WindowStack.Add(new FloatMenu(styleOptions.Select(style =>
                            new FloatMenuOption(style.Value.LabelCap, () =>
                            {
                                _thing.SetStyleDef(style.Key);
                                _thing.Notify_ColorChanged();
                            }, style.Value.Icon, Color.white))
                        .Append(new FloatMenuOption("None", () =>
                        {
                            _thing.SetStyleDef(null);
                            _thing.Notify_ColorChanged();
                        }))
                        .ToList()));
                }
            }).When(ThingUtility.ThingStyles.Select(ts => ts.thingDef).Contains(_thing.def)),
            // Hit points
            L.Cell(rect => { }),
            // Tainted
            L.Cell(rect => { }).When(_thing is Apparel),
            // Count
            L.Cell(rect => { }).When(_thing.def.stackLimit > 1),
            // Persona traits
            L.Cell(rect => { }).When(_thing.HasComp<CompBladelinkWeapon>()),
            // Name
            L.Cell(rect => { }).When(_thing.HasComp<CompGeneratedNames>()),
        ], gapX: 24f, wrap: true, childFlexBasis: 0.5f);

        using (new TextBlock(GameFont.Small))
        {
            var height = FlexLayoutEngine.Draw(layout, inRect, (action, rect) =>
            {
                action(rect);
                return UIUtility.ButtonHeight;
            });
            if (!Mathf.Approximately(windowRect.height, height))
                windowRect.height = height + Margin * 2;
        }
    }
}