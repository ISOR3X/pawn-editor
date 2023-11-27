using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class Dialog_EditThing : Dialog_EditItem<Thing>
{
    private string _buffer;
    private float traitsHeight;

    public Dialog_EditThing(Thing item, Pawn pawn = null, UITable<Pawn> table = null) : base(item, pawn, table) => _buffer = "";

    public override void Select(Thing item)
    {
        base.Select(item);
        _buffer = "";
    }

    protected override void DoContents(Listing_Standard listing)
    {
        // Stuff
        if (Selected.def.stuffCategories is { Count: > 1 })
            if (UIUtility.ButtonTextImage(listing.GetRectLabeled("StatsReport_Material".Translate(), CELL_HEIGHT), Selected.Stuff))
                Find.WindowStack.Add(new FloatMenu(GenStuff.AllowedStuffsFor(Selected.def)
                   .Select(stuff => new FloatMenuOption(stuff.LabelCap, () =>
                    {
                        Selected.SetStuffDirect(stuff);
                        Selected.Notify_ColorChanged();
                        PawnEditor.Notify_PointsUsed();
                    }, Widgets.GetIconFor(stuff), stuff.uiIconColor))
                   .ToList()));


        // Quality
        if (Selected.TryGetComp<CompQuality>() is { } compQuality)
        {
            var buttonLabel = compQuality.Quality.GetLabel().CapitalizeFirst();
            if (listing.ButtonTextLabeledPct("Quality".Translate(), buttonLabel, LABEL_WIDTH_PCT, TextAnchor.MiddleLeft))
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
            var widgetRect = listing.GetRectLabeled("Color".Translate(), CELL_HEIGHT);
            var colorRect = widgetRect.TakeRightPart(24f);
            widgetRect.xMax -= 4f;
            colorRect.height = 24f;
            colorRect.y += 3f;
            var curColor = colorComp.Color != Color.white ? colorComp.Color : apparel2.Stuff.stuffProps.color;

            if (Widgets.ButtonText(widgetRect, "PawnEditor.PickColor".Translate()))
                Find.WindowStack.Add(new Dialog_ColorPicker(color => apparel2.SetColor(color),
                    DefDatabase<ColorDef>.AllDefs.Select(cd => cd.color).ToList(), curColor, apparel2.Stuff.stuffProps.color,
                    Pawn.story.favoriteColor));

            Widgets.DrawRectFast(colorRect, curColor);
        }


        // Style
        if (ListingMenu_Items.ThingStyles.Select(ts => ts.ThingDef).Contains(Selected.def))
        {
            var styleOptions = ListingMenu_Items.ThingStyles.FirstOrDefault(ts => ts.ThingDef == Selected.def).StyleDefs;
            if (UIUtility.ButtonTextImage(listing.GetRectLabeled("Stat_Thing_StyleLabel".Translate(), CELL_HEIGHT),
                    styleOptions.FirstOrDefault(so => so.Key == Selected.GetStyleDef()).Value))
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
        hitPoints = listing.SliderLabeled("HitPointsBasic".Translate().CapitalizeFirst(), hitPoints, 1, maxHitPoints, LABEL_WIDTH_PCT,
            (hitPoints / maxHitPoints).ToStringPercent());
        if (Selected.HitPoints != (int)hitPoints)
        {
            Selected.HitPoints = (int)hitPoints;
            PawnEditor.Notify_PointsUsed();
        }

        // Tainted
        if (Selected is Apparel apparel)
        {
            var widgetRect = listing.GetRectLabeled("PawnEditor.Tainted".Translate(), CELL_HEIGHT);
            var isTainted = apparel.WornByCorpse;
            Widgets.Checkbox(new(widgetRect.x + (widgetRect.width - Widgets.CheckboxSize) / 2, widgetRect.y + 3f), ref isTainted);
            if (apparel.WornByCorpse != isTainted)
            {
                apparel.wornByCorpseInt = isTainted;
                PawnEditor.Notify_PointsUsed();
            }
        }

        // Count
        if (Selected.def.stackLimit > 1)
        {
            var widgetRect = listing.GetRectLabeled("PenFoodTab_Count".Translate(), CELL_HEIGHT);
            var stackCount = Selected.stackCount;
            UIUtility.IntField(widgetRect, ref Selected.stackCount, 1, Selected.def.stackLimit, ref _buffer, true);
            if (stackCount != Selected.stackCount) PawnEditor.Notify_PointsUsed();
        }

        // Persona traits
        if (Selected.TryGetComp<CompBladelinkWeapon>() is { } bladelink)
        {
            if (Event.current.type == EventType.Layout) traitsHeight = listing.listingRect.height - listing.CurHeight;
            var traitsRect = listing.GetRectLabeled("Traits".Translate(), traitsHeight, LABEL_WIDTH_PCT / 2);

            var options4 = DefDatabase<WeaponTraitDef>.AllDefs.Select(weaponTraitDef => new FloatMenuOption(weaponTraitDef.LabelCap, () =>
                {
                    if (bladelink.CanAddTrait(weaponTraitDef)) bladelink.traits.Add(weaponTraitDef);
                    else Messages.Message("PawnEditor.TraitDisallowedByKind".Translate(weaponTraitDef.label, Selected.Label), MessageTypeDefOf.RejectInput);
                }))
               .ToList();

            if (UIUtility.DefaultButtonText(ref traitsRect, "Add".Translate().CapitalizeFirst() + " " + "Trait".Translate(), rightAlign: true))
                Find.WindowStack.Add(new FloatMenu(options4));

            var rect = GenUI.DrawElementStack(traitsRect, 30, bladelink.traits, delegate(Rect r, WeaponTraitDef weaponTraitDef)
            {
                r = r.ContractedBy(0f, 4f);
                GUI.color = CharacterCardUtility.StackElementBackground;
                GUI.DrawTexture(r, BaseContent.WhiteTex);
                GUI.color = Color.white;
                if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                Widgets.Label(new(r.x + 5f, r.y, r.width - 10f, r.height), weaponTraitDef.LabelCap);
                if (Mouse.IsOver(r))
                {
                    var weaponTraitDefLocal = weaponTraitDef;
                    TooltipHandler.TipRegion(r, weaponTraitDefLocal.description);
                    if (Widgets.ButtonImage(r.RightPartPixels(r.height).ContractedBy(4), TexButton.DeleteX))
                    {
                        bladelink.traits.Remove(weaponTraitDefLocal);
                        PawnEditor.Notify_PointsUsed();
                    }
                }
            }, weaponTraitDef => Text.CalcSize(weaponTraitDef.LabelCap).x + 10f, 5f);

            if (Event.current.type == EventType.Layout) traitsHeight = rect.height;
        }

        // Name
        var name = Selected.TryGetComp<CompGeneratedNames>();
        if (name != null)
        {
            var widgetRect = listing.GetRectLabeled("PawnEditor.Name".Translate(), CELL_HEIGHT);
            if (Widgets.ButtonImage(widgetRect.TakeRightPart(30f).ContractedBy(4f), TexPawnEditor.Randomize)) name.Initialize(name.Props);
            name.name = Widgets.TextField(widgetRect, name.name);
        }
    }
}
