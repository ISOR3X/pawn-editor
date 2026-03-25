using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_EditThing(Rect boundWidgetRect, Thing thing) : FloatWindow(boundWidgetRect)
{
    protected override Window? Owner => Find.WindowStack.WindowOfType<Window_Editor>();
    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomRight;
    protected override Vector2 InitialPositionShift => Vector2.zero;
    protected override bool UseWidgetWidth => true;

    private static readonly string?[] TextfieldBuffers = new string[2];

    private const float Gap = 12f;

    public override void DoWindowContents(Rect inRect)
    {
        var hasMadeFromStuff = thing.def.MadeFromStuff;
        var hasQuality        = thing.HasComp<CompQuality>();
        var hasColor          = thing is Apparel && thing.HasComp<CompColorable>();
        var hasStyle          = ThingUtility.ThingStyles.Select(ts => ts.thingDef).Contains(thing.def);
        var isApparel         = thing is Apparel;
        var hasCount          = thing.def.stackLimit > 1;
        var hasPersonaTraits  = thing.HasComp<CompBladelinkWeapon>();
        var hasName           = thing.HasComp<CompGeneratedNames>();
        var hasRow4           = hasCount || hasName;

        var rows = 3;
        if (hasRow4)         rows++;
        if (hasPersonaTraits) rows++;

        Taffy.Grid(inRect,
            columns: [Taffy.Fr(), Taffy.Fr(2), Taffy.Fr(), Taffy.Fr(2)],
            gap: Gap, autoRowHeight: UIUtility.ButtonHeight,
            build: grid =>
            {
                // ── Row 1: Stuff + Quality ─────────────────────────────────────
                if (hasMadeFromStuff)
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Stuff"));
                    grid.GridItem(draw: r =>
                    {
                        if (UIUtility.ButtonText_WithIcon(r, thing.Stuff.LabelCap,
                                Verse.Widgets.GetIconFor(thing.Stuff), thing.Stuff.stuffProps.color))
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
                else
                    grid.GridItem(colSpan: 2);

                if (hasQuality)
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Quality"));
                    grid.GridItem(draw: r =>
                    {
                        var compQuality = thing.TryGetComp<CompQuality>();
                        if (Verse.Widgets.ButtonText(r, compQuality.Quality.GetLabel().CapitalizeFirst()))
                        {
                            Find.WindowStack.Add(new FloatMenu(QualityUtility.AllQualityCategories
                                .Select(quality => new FloatMenuOption(quality.GetLabel().CapitalizeFirst(),
                                    () => compQuality.SetQuality(quality, ArtGenerationContext.Outsider)))
                                .ToList()));
                        }
                    });
                }
                else
                    grid.GridItem(colSpan: 2);

                // ── Row 2: Color + Style ───────────────────────────────────────
                if (hasColor)
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Color"));
                    grid.GridItem(draw: r =>
                    {
                        var apparel   = (Apparel)thing;
                        var colorRect = r.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize);
                        var curColor  = apparel.DrawColor;

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
                else
                    grid.GridItem(colSpan: 2);

                if (hasStyle)
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Style"));
                    grid.GridItem(draw: r =>
                    {
                        var styleOptions  = ThingUtility.ThingStyles.FirstOrDefault(ts => ts.thingDef == thing.def).styleDefs;
                        var currentStyle  = styleOptions.FirstOrDefault(so => so.Key == thing.GetStyleDef());
                        if (UIUtility.ButtonText_WithIcon(r, currentStyle.Value?.LabelCap ?? "None",
                                currentStyle.Value?.Icon ?? Verse.Widgets.PlaceholderIconTex))
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
                        }
                    });
                }
                else
                    grid.GridItem(colSpan: 2);

                // ── Row 3: Hitpoints + Tainted ─────────────────────────────────
                grid.GridItem(draw: r => Verse.Widgets.Label(r, "Hitpoints"));
                grid.GridItem(draw: r =>
                {
                    float hitPoints    = thing.HitPoints;
                    float maxHitPoints = thing.MaxHitPoints;
                    thing.HitPoints = Mathf.CeilToInt(Verse.Widgets.HorizontalSlider(r,
                        thing.HitPoints, 1, thing.MaxHitPoints, true,
                        (hitPoints / maxHitPoints).ToStringPercent()));
                });
                if (isApparel)
                {
                    grid.GridItem(colSpan: 2, draw: r =>
                    {
                        var apparel   = (Apparel)thing;
                        var isTainted = apparel.WornByCorpse;
                        Verse.Widgets.CheckboxLabeled(r, "Tainted", ref isTainted);
                        if (isTainted != apparel.WornByCorpse) apparel.WornByCorpse = isTainted;
                    });
                }
                else
                    grid.GridItem(colSpan: 2);

                // ── Row 4: Count + Name (if needed) ────────────────────────────
                if (hasRow4)
                {
                    if (hasCount)
                    {
                        grid.GridItem(draw: r => Verse.Widgets.Label(r, "Count"));
                        grid.GridItem(draw: r =>
                            Widgets.DelayedTextFieldNumeric(r, thing.stackCount, ref TextfieldBuffers[0],
                                1, thing.def.stackLimit, null, true));
                    }
                    else
                        grid.GridItem(colSpan: 2);

                    if (hasName)
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
                    else
                        grid.GridItem(colSpan: 2);
                }

                // ── Row 5: Persona traits (if needed) ──────────────────────────
                if (hasPersonaTraits)
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
                                    weaponTraitDef.LabelCap.Colorize(canAdd ? Color.white : ColoredText.SubtleGrayColor),
                                    () =>
                                    {
                                        if (canAdd) bladelink.traits.Add(weaponTraitDef);
                                        else Messages.Message(
                                            "TraitDisallowedByKind".Translate(weaponTraitDef.label, thing.Label),
                                            MessageTypeDefOf.RejectInput);
                                    });
                            }).ToList();

                        if (Verse.Widgets.ButtonImage(
                                r.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize),
                                TexButton.Add))
                            Find.WindowStack.Add(new FloatMenu(traitOptions));

                        const float elementHeight = 22f;
                        const float margin        = (UIUtility.ButtonHeight - elementHeight) / 2;
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

        var expectedHeight = rows * UIUtility.ButtonHeight + (rows - 1) * Gap;
        if (!Mathf.Approximately(windowRect.height, expectedHeight))
            windowRect.height = expectedHeight + Margin * 2;
    }
}
