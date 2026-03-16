using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Layout;
using RimWorld;
using UnityEngine;
using Verse;
using L = PawnEditor.Layout.LayoutHelper;

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
        var layout = L.Grid([GridTrack.Fr(), GridTrack.Fr(2), GridTrack.Fr(), GridTrack.Fr(2)], children:
        [
            // Stuff
            ..L.LabeledWidget("Stuff", rect =>
            {
                if (UIUtility.ButtonText_WithIcon(rect, thing.Stuff.LabelCap, Verse.Widgets.GetIconFor(thing.Stuff),
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
            }, () => thing.def.MadeFromStuff),
            // Quality
            ..L.LabeledWidget("Quality", rect =>
            {
                var compQuality = thing.TryGetComp<CompQuality>();
                if (Verse.Widgets.ButtonText(rect, compQuality.Quality.GetLabel().CapitalizeFirst()))
                {
                    Find.WindowStack.Add(new FloatMenu(QualityUtility.AllQualityCategories.Select(quality =>
                            new FloatMenuOption(quality.GetLabel().CapitalizeFirst(),
                                () => { compQuality.SetQuality(quality, ArtGenerationContext.Outsider); }))
                        .ToList()));
                }
            }, thing.HasComp<CompQuality>),
            // Color
            ..L.LabeledWidget("Color", rect =>
            {
                var apparel = thing as Apparel;
                var colorRect = rect.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize);
                var curColor = apparel?.DrawColor ?? Color.white;

                Verse.Widgets.DrawLightHighlight(colorRect);
                colorRect = colorRect.ContractedBy(2f);
                Verse.Widgets.DrawRectFast(colorRect, curColor);
                rect.xMax -= 2f;
                if (Verse.Widgets.ButtonText(rect, "Pick color"))
                {
                    Find.WindowStack.Add(new Dialog_ColorPicker(color => apparel.SetColor(color), curColor,
                        DefDatabase<ColorDef>.AllDefs.Select(cd => cd.color).ToList()));
                }
            }, () => thing is Apparel && thing.HasComp<CompColorable>()),
            // Style
            ..L.LabeledWidget("Style", rect =>
            {
                var styleOptions = ThingUtility.ThingStyles.FirstOrDefault(ts => ts.thingDef == thing.def).styleDefs;
                var currentStyle = styleOptions.FirstOrDefault(so => so.Key == thing.GetStyleDef());
                if (UIUtility.ButtonText_WithIcon(rect, currentStyle.Value?.LabelCap ?? "None",
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
            }, () => ThingUtility.ThingStyles.Select(ts => ts.thingDef).Contains(thing.def)),
            // Hitpoints
            ..L.LabeledWidget("Hitpoints", rect =>
            {
                float hitPoints = thing.HitPoints;
                float maxHitPoints = thing.MaxHitPoints;

                thing.HitPoints = Mathf.CeilToInt(Verse.Widgets.HorizontalSlider(rect, thing.HitPoints, 1,
                    thing.MaxHitPoints, true,
                    (hitPoints / maxHitPoints).ToStringPercent()));
            }),
            // Tainted
            L.Cell(rect =>
            {
                var apparel = thing as Apparel;
                var isTainted = apparel!.WornByCorpse;

                Verse.Widgets.CheckboxLabeled(rect, "Tainted", ref isTainted);
                if (isTainted != apparel.WornByCorpse) apparel.WornByCorpse = isTainted;
            }, colSpan: 2).When(() => thing is Apparel),
            // Count
            ..L.LabeledWidget("Count", rect =>
            {
                Widgets.DelayedTextFieldNumeric(rect, thing.stackCount, ref TextfieldBuffers[0], 1,
                    thing.def.stackLimit, null,
                    true);
            }, () => thing.def.stackLimit > 1),
            // Persona traits
            ..L.LabeledWidget("Persona traits", rect =>
            {
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

                if (Verse.Widgets.ButtonImage(
                        rect.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize), TexButton.Add))
                    Find.WindowStack.Add(new FloatMenu(traitOptions));

                const float elementHeight = 22f;
                const float margin = (UIUtility.ButtonHeight - elementHeight) / 2;
                var widgetRect = rect.ContractedBy(margin);
                var s = GenUI.DrawElementStack(widgetRect, elementHeight, bladelink.traits,
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
                    }, weaponTraitDef => Text.CalcSize(weaponTraitDef.LabelCap).x + 10f, 4f);

                if (toRemove != null) bladelink.traits.Remove(toRemove);
                return s.height + margin * 2;
            }, thing.HasComp<CompBladelinkWeapon>, colSpan: 4),
            ..L.LabeledWidget("Name", rect =>
            {
                var name = thing.TryGetComp<CompGeneratedNames>();

                if (Verse.Widgets.ButtonImage(rect.TakeRightPart(30f).ContractedBy(4f), TexPawnEditor.Reroll))
                    name.Initialize(name.Props);
                name.name = Verse.Widgets.TextField(rect, name.name);
            }, thing.HasComp<CompGeneratedNames>),
        ], gapX: 12f);

        var height = layout.Draw(inRect, (action, rect) => action(rect));

        if (!Mathf.Approximately(windowRect.height, height))
            windowRect.height = height + Margin * 2;
    }
}