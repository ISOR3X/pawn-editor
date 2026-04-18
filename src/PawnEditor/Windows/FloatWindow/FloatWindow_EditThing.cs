using HotSwap;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.Extensions;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_EditThing(Rect boundWidgetRect, Thing thing, Window? owner = null)
    : FloatWindow(boundWidgetRect, owner)
{
    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomCenter;
    protected override Vector2 InitialPositionShift => Vector2.zero;
    public override Vector2 InitialSize => new(500, 200);

    private static void GridButton(TaffyBuilder grid, string label, Texture2D? icon = null, Color? iconColor = null, Action<Rect>? onClick = null,
        StyleOverride? style = null)
    {
        style ??= new StyleOverride();
        style = style.Merge(new StyleOverride
        {
            width = Dimension.AUTO, justifySelf = AlignItems.Stretch
        });
        grid.Button(label, icon, iconColor, style: style, onClick: onClick);
    }

    public override void DoWindowContents(Rect inRect)
    {
        // MeasuredGrid runs with unconstrained height, so Taffy computes the exact content height,
        // which we use to auto-resize the window below.

        // TODO: Convert to clean taffy components/ layout.
        var contentHeight = Void.Taffy.MeasuredGrid(inRect,
            columns: [Void.Taffy.Fr(), Void.Taffy.Fr(2), Void.Taffy.Fr(), Void.Taffy.Fr(2)],
            gapX: GenUI.GapLabel, gapY: GenUI.GapTiny, autoRowHeight: UIUtility.ButtonHeight,
            build: grid =>
            {
                if (thing.def.MadeFromStuff)
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Stuff"));
                    GridButton(grid, thing.Stuff.LabelCap,
                        Verse.Widgets.GetIconFor(thing.Stuff), thing.Stuff.stuffProps.color, onClick: _ =>
                        {
                            {
                                Find.WindowStack.Add(new FloatMenu(GenStuff.AllowedStuffsFor(thing.def)
                                    .Select(stuff => new FloatMenuOption(stuff.LabelCap, () =>
                                        {
                                            thing.SetStuffDirect(stuff);
                                            thing.SetColor(stuff.stuffProps.color);
                                            thing.Notify_ColorChanged();
                                        },
                                        Verse.Widgets.GetIconFor(stuff),
                                        stuff.stuffProps.color))
                                    .ToList()));
                            }
                        });
                }

                if (thing.HasComp<CompQuality>())
                {
                    var compQuality = thing.TryGetComp<CompQuality>();
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Quality"));
                    GridButton(grid,compQuality.Quality.GetLabel().CapitalizeFirst(), onClick: _ =>
                    {
                        Find.WindowStack.Add(new FloatMenu(QualityUtility.AllQualityCategories
                            .Select(quality => new FloatMenuOption(quality.GetLabel().CapitalizeFirst(),
                                () => compQuality.SetQuality(quality, ArtGenerationContext.Outsider)))
                            .ToList()));
                    });
                }

                if (thing is Apparel && thing.HasComp<CompColorable>())
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Color"));
                    grid.GridItem(draw: r =>
                    {
                        var apparel = (Apparel)thing;
                        var colorRect = r.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize);
                        var curColor = apparel.DrawColor;

                        Verse.Widgets.DrawLightHighlight(colorRect);
                        colorRect = colorRect.ContractedBy(2f);
                        Verse.Widgets.DrawRectFast(colorRect, curColor);
                        r.xMax -= 2f;
                        if (Verse.Widgets.ButtonText(r, "Pick color"))
                        {
                            Find.WindowStack.Add(new Dialog_ColorPicker(color => apparel.SetColor(color), curColor,
                                DefDatabase<ColorDef>.AllDefs.Select(cd => cd.color).ToList()));
                        }
                    });
                }

                if (ThingUtility.ThingStyles.Select(ts => ts.thingDef).Contains(thing.def))
                {
                    var styleOptions = ThingUtility.ThingStyles.FirstOrDefault(ts => ts.thingDef == thing.def)
                        .styleDefs;
                    var currentStyle = styleOptions.FirstOrDefault(so => so.Key == thing.GetStyleDef());
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Style"));
                    GridButton(grid,currentStyle.Value?.LabelCap ?? "None",
                        currentStyle.Value?.Icon ?? Verse.Widgets.PlaceholderIconTex, onClick: _ =>
                        {
                            Find.WindowStack.Add(new FloatMenu(styleOptions
                                .Select(style => new FloatMenuOption(style.Value.LabelCap, () =>
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
                        });
                }

                grid.GridItem(draw: r => Verse.Widgets.Label(r, "Hit points"));
                grid.GridItem(draw: r =>
                {
                    float hitPoints = thing.HitPoints;
                    float maxHitPoints = thing.MaxHitPoints;
                    thing.HitPoints = Mathf.CeilToInt(Verse.Widgets.HorizontalSlider(r,
                        thing.HitPoints, 1, thing.MaxHitPoints, true,
                        (hitPoints / maxHitPoints).ToStringPercent()));
                });

                if (thing is Apparel)
                {
                    grid.GridItem(colSpan: 2, draw: r =>
                    {
                        var apparel = (Apparel)thing;
                        var isTainted = apparel.WornByCorpse;
                        Verse.Widgets.CheckboxLabeled(r, "Tainted", ref isTainted);
                        if (isTainted != apparel.WornByCorpse) apparel.WornByCorpse = isTainted;
                    });
                }

                if (thing.def.stackLimit > 1)
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Count"));
                    grid.InputNumber(ref thing.stackCount, 1, thing.def.stackLimit);
                }

                if (thing.HasComp<CompGeneratedNames>())
                {
                    var name = thing.TryGetComp<CompGeneratedNames>();
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Name"));
                    grid.GridItem(draw: r =>
                    {
                        if (Verse.Widgets.ButtonImage(r.TakeRightPart(30f).ContractedBy(4f), TexPawnEditor.Reroll))
                            name.Initialize(name.Props);
                        name.name = Verse.Widgets.TextField(r, name.name);
                    });
                }

                if (thing.HasComp<CompBladelinkWeapon>())
                {
                    var bladelink = thing.TryGetComp<CompBladelinkWeapon>();
                    WeaponTraitDef? toRemove = null;

                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Persona traits"));
                    grid.GridItem(colSpan: 3, draw: r =>
                    {
                        var traitOptions = DefDatabase<WeaponTraitDef>.AllDefs
                            .OrderBy(t => !bladelink.CanAddTrait(t))
                            .Select(weaponTraitDef =>
                            {
                                var canAdd = bladelink.CanAddTrait(weaponTraitDef);
                                return new FloatMenuOption(
                                    weaponTraitDef.LabelCap.Colorize(canAdd
                                        ? Color.white
                                        : ColoredText.SubtleGrayColor),
                                    () =>
                                    {
                                        if (canAdd) bladelink.traits.Add(weaponTraitDef);
                                        else
                                            Messages.Message(
                                                "TraitDisallowedByKind".Translate(weaponTraitDef.label, thing.Label),
                                                MessageTypeDefOf.RejectInput);
                                    });
                            }).ToList();

                        if (Verse.Widgets.ButtonImage(
                                r.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize),
                                TexButton.Add))
                            Find.WindowStack.Add(new FloatMenu(traitOptions));

                        const float elementHeight = 22f;
                        const float margin = (UIUtility.ButtonHeight - elementHeight) / 2;
                        var widgetRect = r.ContractedBy(margin);
                        GenUI.DrawElementStack(widgetRect, elementHeight, bladelink.traits,
                            delegate(Rect er, WeaponTraitDef weaponTraitDef)
                            {
                                GUI.color = CharacterCardUtility.StackElementBackground;
                                GUI.DrawTexture(er, BaseContent.WhiteTex);
                                GUI.color = Color.white;
                                if (Mouse.IsOver(er)) Verse.Widgets.DrawHighlight(er);

                                Verse.Widgets.Label(new Rect(er.x + 5f, er.y, er.width - 10f, er.height),
                                    weaponTraitDef.LabelCap);
                                if (Mouse.IsOver(er))
                                {
                                    TooltipHandler.TipRegion(er, weaponTraitDef.description);
                                    if (Verse.Widgets.ButtonImage(er.RightPartPixels(er.height).ContractedBy(4),
                                            TexButton.Delete))
                                        toRemove = weaponTraitDef;
                                }
                            }, weaponTraitDef => Text.CalcSize(weaponTraitDef.LabelCap).x + 10f);

                        if (toRemove != null) bladelink.traits.Remove(toRemove);
                    });
                }
            });

        if (!Mathf.Approximately(windowRect.height, contentHeight + Margin * 2))
            windowRect.height = contentHeight + Margin * 2;
    }
}