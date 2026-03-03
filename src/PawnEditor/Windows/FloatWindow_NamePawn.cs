using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_NamePawn : FloatWindow
{
    protected override FloatWindowAlignment alignment => FloatWindowAlignment.BottomRight;
    private Listing_Horizontal listing = new Listing_Horizontal();

    private CultureDef selectedCulture;
    private XenotypeDef selectedXenotype;
    private Gender selectedGender = Gender.Male;
    private static bool forceNoNick;
    private static bool keepLastName;

    public override Vector2 InitialSize => new Vector2(750, 200);

    public FloatWindow_NamePawn(Rect boundWidgetRect) : base(boundWidgetRect)
    {
    }


    public override void DoWindowContents(Rect inRect)
    {
        Pawn p = Window_Editor.GetSelectedPawn();
        if (p == null) return;
        selectedCulture ??= p.Faction?.ideos?.PrimaryCulture;


        var cultures = DefDatabase<CultureDef>.AllDefsListForReading;
        var xenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;


        listing.Begin(inRect);
        if (selectedCulture != null)
        {
            if (listing.ButtonTextLabeled("Culture", selectedCulture.LabelCap, 6))
            {
                Find.WindowStack.Add(new FloatMenu(cultures.Select(c => new FloatMenuOption(c.LabelCap, () => selectedCulture = c)).ToList()));
            }
        }

        if (ModsConfig.BiotechActive)
        {
            if (listing.ButtonTextLabeled("Xenotype", selectedXenotype?.LabelCap ?? "None", 6))
            {
                Find.WindowStack.Add(new FloatMenu(xenotypes.Select(x => new FloatMenuOption(x.LabelCap, () => selectedXenotype = x))
                    .Append(new FloatMenuOption("None", () => selectedXenotype = null)).ToList()));
            }
        }

        if (listing.ButtonTextLabeled("Gender", selectedGender.GetLabel().CapitalizeFirst(), 4))
        {
            var genders = new List<Gender>()
            {
                Gender.Male, Gender.Female
            };
            Find.WindowStack.Add(new FloatMenu(genders.Select(g => new FloatMenuOption(g.GetLabel().CapitalizeFirst(), () => selectedGender = g)).ToList()));
        }

        listing.CheckboxLabeled("Keep last name", ref keepLastName, 4);
        listing.CheckboxLabeled("Force no nickname", ref forceNoNick, 4);

        Rect nameRect = listing.GetRect(8); // We use GetRect instead of RectLabeled so the label width isnt used for the listing.
        Widgets.Label(nameRect, p.Name.ToStringFull);
        if (listing.ButtonText("Generate", 4))
        {
            string lastName = null;
            if (keepLastName && p.Name is NameTriple triple)
            {
                lastName = triple.Last;
            }

            p.Name = PawnBioAndNameGenerator.GenerateFullPawnName(p.def, p.kindDef.GetNameMaker(p.gender), p.story, selectedXenotype, p.RaceProps.GetNameGenerator(selectedGender),
                selectedCulture, p.IsCreepJoiner, selectedGender, p.RaceProps.nameCategory, lastName, forceNoNick);
        }

        listing.End();

        var height = listing.totalHeight;
        if (!Mathf.Approximately(windowRect.height, height)) windowRect.height = height + this.Margin + 2f;
    }
}