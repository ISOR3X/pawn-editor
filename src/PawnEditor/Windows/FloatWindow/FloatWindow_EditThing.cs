using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_EditThing(Rect boundWidgetRect, Thing thing) : FloatWindow(boundWidgetRect)
{
    protected override Window? Owner => Find.WindowStack.WindowOfType<Window_Editor>();
    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomRight;
    protected override bool UseWidgetWidth => true;

    private static readonly string?[] TextfieldBuffers = new string[2];

    public override void DoWindowContents(Rect inRect)
    {
        var layout = LayoutHelper.Row([
            // Stuff
            LayoutHelper.Cell(rect =>
            {
                if (UIUtility.ButtonTextLabeled_WithIcon(rect, "StatsReport_Material".Translate(),
                        thing.Stuff.LabelCap, Verse.Widgets.GetIconFor(thing.Stuff),
                        thing.Stuff.stuffProps.color))
                {
                    Find.WindowStack.Add(new FloatMenu(GenStuff.AllowedStuffsFor(thing.def)
                        .Select(stuff => new FloatMenuOption(stuff.LabelCap, () =>
                            {
                                thing.SetStuffDirect(stuff);
                                thing.SetColor(stuff.stuffProps.color);
                                thing.Notify_ColorChanged();
                            },
                            Verse.Widgets.GetIconFor(stuff),
                            stuff.stuffProps.color
                        ))
                        .ToList()));
                }
            }).When(() => thing.def.MadeFromStuff),
            // Quality
            LayoutHelper.Cell(rect =>
            {
                var compQuality = thing.TryGetComp<CompQuality>();
                if (UIUtility.ButtonTextLabeled(rect, "Quality", compQuality.Quality.GetLabel().CapitalizeFirst()))
                {
                    Find.WindowStack.Add(new FloatMenu(QualityUtility.AllQualityCategories.Select(quality =>
                            new FloatMenuOption(quality.GetLabel().CapitalizeFirst(),
                                () => { compQuality.SetQuality(quality, ArtGenerationContext.Outsider); }))
                        .ToList()));
                }
            }).When(thing.HasComp<CompQuality>),
            // Color
            LayoutHelper.Cell(rect =>
            {
                var apparel = thing as Apparel;
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
            }).When(() => thing is Apparel && thing.HasComp<CompColorable>()),
            // Style
            LayoutHelper.Cell(rect =>
            {
                var styleOptions = ThingUtility.ThingStyles.FirstOrDefault(ts => ts.thingDef == thing.def).styleDefs;
                var currentStyle = styleOptions.FirstOrDefault(so => so.Key == thing.GetStyleDef());
                if (UIUtility.ButtonTextLabeled_WithIcon(rect, "Stat_Thing_StyleLabel".Translate(),
                        currentStyle.Value?.LabelCap ?? "None",
                        currentStyle.Value?.Icon ?? Verse.Widgets.PlaceholderIconTex))
                {
                    Find.WindowStack.Add(new FloatMenu(styleOptions.Select(style =>
                            new FloatMenuOption(style.Value.LabelCap, () =>
                            {
                                thing.SetStyleDef(style.Key);
                                thing.Notify_ColorChanged();
                            }, style.Value.Icon, Color.white))
                        .Append(new FloatMenuOption("None", () =>
                        {
                            thing.SetStyleDef(null);
                            thing.Notify_ColorChanged();
                        }))
                        .ToList()));
                }
            }).When(() => ThingUtility.ThingStyles.Select(ts => ts.thingDef).Contains(thing.def)),
            // Hit points
            LayoutHelper.Cell(rect =>
            {
                var widgetRect = UIUtility.RectLabeled(rect, "Hitpoints");

                float hitPoints = thing.HitPoints;
                float maxHitPoints = thing.MaxHitPoints;

                thing.HitPoints = Mathf.CeilToInt(Verse.Widgets.HorizontalSlider(widgetRect, thing.HitPoints, 1,
                    thing.MaxHitPoints, true,
                    (hitPoints / maxHitPoints).ToStringPercent()));
            }),
            // Tainted
            LayoutHelper.Cell(rect =>
            {
                var widgetRect = UIUtility.RectLabeled(rect, "Tainted")
                    .CenteredHorizontally(Verse.Widgets.CheckboxSize);

                var apparel = thing as Apparel;
                var isTainted = apparel!.WornByCorpse;

                Verse.Widgets.Checkbox(widgetRect.position, ref isTainted);
                if (isTainted != apparel.WornByCorpse) apparel.WornByCorpse = isTainted;
            }).When(() => thing is Apparel),
            // Count
            LayoutHelper.Cell(rect =>
            {
                var widgetRect = UIUtility.RectLabeled(rect, "Count");

                Widgets.DelayedTextFieldNumeric(widgetRect, thing.stackCount, ref TextfieldBuffers[0], 1,
                    thing.def.stackLimit, null,
                    true);
            }).When(() => thing.def.stackLimit > 1),
            // Persona traits
            LayoutHelper.Cell(rect =>
            {
                var widgetRect = UIUtility.RectLabeled(rect, "Traits");

                WeaponTraitDef? toRemove = null;
                var bladelink = thing.TryGetComp<CompBladelinkWeapon>();
                var traitOptions = DefDatabase<WeaponTraitDef>.AllDefs.OrderBy(t => !bladelink.CanAddTrait(t))
                    .Select(weaponTraitDef =>
                    {
                        var canAdd = bladelink.CanAddTrait(weaponTraitDef);
                        return new FloatMenuOption(
                            weaponTraitDef.LabelCap.Colorize(canAdd ? Color.white : ColoredText.SubtleGrayColor), () =>
                            {
                                if (canAdd) bladelink.traits.Add(weaponTraitDef);
                                else
                                    Messages.Message(
                                        "TraitDisallowedByKind".Translate(weaponTraitDef.label, thing.Label),
                                        MessageTypeDefOf.RejectInput);
                            });
                    }).ToList();

                if (Verse.Widgets.ButtonImage(widgetRect.TakeRightPart(WidgetRow.IconSize), TexButton.Add))
                    Find.WindowStack.Add(new FloatMenu(traitOptions));

                GenUI.DrawElementStack(widgetRect, 22f, bladelink.traits,
                    delegate(Rect r, WeaponTraitDef weaponTraitDef)
                    {
                        GUI.color = CharacterCardUtility.StackElementBackground;
                        GUI.DrawTexture(r, BaseContent.WhiteTex);
                        GUI.color = Color.white;
                        if (Mouse.IsOver(r)) Verse.Widgets.DrawHighlight(r);

                        Verse.Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height), weaponTraitDef.LabelCap);
                        if (Mouse.IsOver(r))
                        {
                            TooltipHandler.TipRegion(r, weaponTraitDef.description);
                            if (Verse.Widgets.ButtonImage(r.RightPartPixels(r.height).ContractedBy(4),
                                    TexButton.Delete))
                            {
                                toRemove = weaponTraitDef;
                            }
                        }
                    }, weaponTraitDef => Text.CalcSize(weaponTraitDef.LabelCap).x + 10f, 5f);

                if (toRemove != null) bladelink.traits.Remove(toRemove);
            }, flexBasis: 1f).When(thing.HasComp<CompBladelinkWeapon>),
            // Name
            LayoutHelper.Cell(rect =>
            {
                var widgetRect = UIUtility.RectLabeled(rect, "Name");

                var name = thing.TryGetComp<CompGeneratedNames>();

                if (Verse.Widgets.ButtonImage(widgetRect.TakeRightPart(30f).ContractedBy(4f), TexPawnEditor.Reroll))
                    name.Initialize(name.Props);
                name.name = Verse.Widgets.TextField(widgetRect, name.name);
            }).When(thing.HasComp<CompGeneratedNames>),
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