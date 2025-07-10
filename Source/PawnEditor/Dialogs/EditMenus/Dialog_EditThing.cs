using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimUI;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class Dialog_EditThing : Dialog_EditItem<Thing>
{
    private string _buffer;

    public Dialog_EditThing(Thing item, Pawn pawn = null, UIElement element = null) : base(item, pawn, element) => _buffer = "";

    public override void Select(Thing item)
    {
        base.Select(item);
        _buffer = "";
    }

    protected override void DoContents(Listing_Horizontal listing)
    {
        // Stuff
        if (Selected.def.MadeFromStuff)
            if (listing.ButtonDefLabeled("StatsReport_Material".Translate(), Selected.Stuff, 6))
                Find.WindowStack.Add(new FloatMenu(GenStuff.AllowedStuffsFor(Selected.def)
                    .Select(stuff => new FloatMenuOption(stuff.LabelCap, () =>
                    {
                        Selected.SetStuffDirect(stuff);
                        Selected.SetColor(stuff.stuffProps.color);
                        Selected.Notify_ColorChanged();
                        PawnEditor.Notify_PointsUsed();
                    }, Widgets.GetIconFor(stuff), stuff.uiIconColor))
                    .ToList()));


        // Quality
        if (Selected.TryGetComp<CompQuality>() is { } compQuality)
        {
            var buttonLabel = compQuality.Quality.GetLabel().CapitalizeFirst();
            if (listing.ButtonTextLabeled("Quality".Translate(), buttonLabel, 6))
                Find.WindowStack.Add(new FloatMenu(QualityUtility.AllQualityCategories.Select(quality =>
                        new FloatMenuOption(quality.GetLabel().CapitalizeFirst(), () =>
                        {
                            buttonLabel = quality.GetLabel().CapitalizeFirst();
                            compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
                            PawnEditor.Notify_PointsUsed();
                        }))
                    .ToList()));
        }


        // Color
        if (Selected is Apparel apparel2 && Selected.TryGetComp<CompColorable>() is { } colorComp)
        {
            var widgetRect = listing.RectLabeled("Color".Translate(), 6);
            var colorRect = widgetRect.TakeRightPart(24f);
            widgetRect.xMax -= 4f;
            colorRect.height = 24f;
            colorRect.y += 3f;
            var curColor = apparel2.DrawColor;
            var defaultColor = apparel2.Stuff != null ? apparel2.Stuff.stuffProps.color : apparel2.def.colorGenerator?.NewRandomizedColor() ?? Color.white;
            if (Widgets.ButtonText(widgetRect, "PawnEditor.PickColor".Translate()))
            {
                var specialColors = new Dictionary<string, Color>
                {
                    { "Default", defaultColor }
                };
                if (Pawn.story.favoriteColor != null)
                {
                    specialColors.Add("Favorite", Pawn.story.favoriteColor.color);
                }

                Find.WindowStack.Add(new Dialog_ColorPicker(color => apparel2.SetColor(color),
                    DefDatabase<ColorDef>.AllDefs.Select(cd => cd.color).ToList(), curColor, specialColors));
            }

            Widgets.DrawRectFast(colorRect, curColor);
        }


        // Style
        if (ListingMenu_Items.ThingStyles.Select(ts => ts.ThingDef).Contains(Selected.def))
        {
            var styleOptions = ListingMenu_Items.ThingStyles.FirstOrDefault(ts => ts.ThingDef == Selected.def).StyleDefs;
            if (listing.ButtonDefLabeled("Stat_Thing_StyleLabel".Translate(), styleOptions.FirstOrDefault(so => so.Key == Selected.GetStyleDef()).Value))
                Find.WindowStack.Add(new FloatMenu(styleOptions.Select(style => new FloatMenuOption(style.Value.LabelCap, () =>
                    {
                        Selected.SetStyleDef(style.Key);
                        Selected.Notify_ColorChanged();
                    }, style.Value.Icon, Color.white))
                    .Append(new("None", () =>
                    {
                        Selected.SetStyleDef(null);
                        Selected.Notify_ColorChanged();
                    }))
                    .ToList()));
        }


        // Hit Points
        float hitPoints = Selected.HitPoints;
        float maxHitPoints = Selected.MaxHitPoints;
        hitPoints = listing.SliderLabeled("HitPointsBasic".Translate().CapitalizeFirst(), hitPoints, 1, maxHitPoints, (hitPoints / maxHitPoints).ToStringPercent(), null, null, 6);
        if (Selected.HitPoints != (int)hitPoints)
        {
            Selected.HitPoints = (int)hitPoints;
            PawnEditor.Notify_PointsUsed();
        }

        // Tainted
        if (Selected is Apparel apparel)
        {
            var isTainted = apparel.WornByCorpse;
            listing.CheckboxLabeled("PawnEditor.Tainted".Translate(), ref isTainted, 6);
            if (apparel.WornByCorpse != isTainted)
            {
                apparel.WornByCorpse = isTainted;
                Selected.Notify_ColorChanged();
                PawnEditor.Notify_PointsUsed();
            }
        }

        // Count
        if (Selected.def.stackLimit > 1)
        {
            var widgetRect = listing.RectLabeled("PenFoodTab_Count".Translate(), 6);
            var stackCount = Selected.stackCount;
            UIUtility.IntField(widgetRect, ref Selected.stackCount, 1, Selected.def.stackLimit, ref _buffer, true);
            if (stackCount != Selected.stackCount) PawnEditor.Notify_PointsUsed();
        }

        // Persona traits
        if (Selected.TryGetComp<CompBladelinkWeapon>() is { } bladelink)
        {
            var traitsRect = listing.RectLabeled("Traits".Translate(), height: 60f);

            var options4 = DefDatabase<WeaponTraitDef>.AllDefs.Select(weaponTraitDef => new FloatMenuOption(weaponTraitDef.LabelCap, () =>
                {
                    if (bladelink.CanAddTrait(weaponTraitDef)) bladelink.traits.Add(weaponTraitDef);
                    else Messages.Message("PawnEditor.TraitDisallowedByKind".Translate(weaponTraitDef.label, Selected.Label), MessageTypeDefOf.RejectInput);
                }))
                .ToList();

            string label = "Add".Translate().CapitalizeFirst() + " " + "Trait".Translate();
            var labelWidth = Text.CalcSize(label).x;
            if (Widgets.ButtonText(traitsRect.TakeRightPart(labelWidth + 48).TopPartPixels(30f), label))
                Find.WindowStack.Add(new FloatMenu(options4));

            traitsRect.yMin += 4f;
            GenUI.DrawElementStack(traitsRect, 22f, bladelink.traits, delegate(Rect r, WeaponTraitDef weaponTraitDef)
            {
                GUI.color = CharacterCardUtility.StackElementBackground;
                GUI.DrawTexture(r, BaseContent.WhiteTex);
                GUI.color = Color.white;
                if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                Widgets.Label(new(r.x + 5f, r.y, r.width - 10f, r.height), weaponTraitDef.LabelCap);
                if (Mouse.IsOver(r))
                {
                    var weaponTraitDefLocal = weaponTraitDef;
                    TooltipHandler.TipRegion(r, weaponTraitDefLocal.description);
                    if (Widgets.ButtonImage(r.RightPartPixels(r.height).ContractedBy(4), TexButton.Delete))
                    {
                        bladelink.traits.Remove(weaponTraitDefLocal);
                        PawnEditor.Notify_PointsUsed();
                    }
                }
            }, weaponTraitDef => Text.CalcSize(weaponTraitDef.LabelCap).x + 10f, 5f);
        }

        // Name
        var name = Selected.TryGetComp<CompGeneratedNames>();
        if (name != null)
        {
            var widgetRect = listing.RectLabeled("PawnEditor.Name".Translate(), 6, grow: false);
            if (Widgets.ButtonImage(widgetRect.TakeRightPart(30f).ContractedBy(4f), TexPawnEditor.Randomize)) name.Initialize(name.Props);
            name.name = Widgets.TextField(widgetRect, name.name);
        }
    }
}