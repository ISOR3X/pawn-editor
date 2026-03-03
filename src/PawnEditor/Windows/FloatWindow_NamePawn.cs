using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_NamePawn(Rect boundWidgetRect) : FloatWindow(boundWidgetRect)
{
    private static bool forceNoNick;
    private static bool keepLastName;
    private readonly Listing_Horizontal _listing = new();

    private CultureDef? _selectedCulture;
    private Gender _selectedGender = Gender.Male;
    private XenotypeDef? _selectedXenotype;

    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomCenter;

    public override Vector2 InitialSize => new(750, 200);


    public override void DoWindowContents(Rect inRect)
    {
        var p = Window_Editor.GetSelectedPawn();
        if (p == null) return;
        _selectedCulture ??= p.Faction?.ideos?.PrimaryCulture;


        var cultures = DefDatabase<CultureDef>.AllDefsListForReading;
        var xenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;


        _listing.Begin(inRect);
        if (_selectedCulture != null)
            if (_listing.ButtonTextLabeled("Culture", _selectedCulture.LabelCap, 6))
                Find.WindowStack.Add(new FloatMenu(cultures
                    .Select(c => new FloatMenuOption(c.LabelCap, () => _selectedCulture = c)).ToList()));

        if (ModsConfig.BiotechActive)
            if (_listing.ButtonTextLabeled("Xenotype", _selectedXenotype?.LabelCap ?? "None", 6))
                Find.WindowStack.Add(new FloatMenu(xenotypes
                    .Select(x => new FloatMenuOption(x.LabelCap, () => _selectedXenotype = x))
                    .Append(new FloatMenuOption("None", () => _selectedXenotype = null)).ToList()));

        if (_listing.ButtonTextLabeled("Gender", _selectedGender.GetLabel().CapitalizeFirst(), 4))
        {
            var genders = new List<Gender>
            {
                Gender.Male, Gender.Female
            };
            Find.WindowStack.Add(new FloatMenu(genders
                .Select(g => new FloatMenuOption(g.GetLabel().CapitalizeFirst(), () => _selectedGender = g)).ToList()));
        }

        _listing.CheckboxLabeled("Keep last name", ref keepLastName, 4);
        _listing.CheckboxLabeled("Force no nickname", ref forceNoNick, 4);

        var nameRect =
            _listing.GetRect(8); // We use GetRect instead of RectLabeled so the label width isnt used for the listing.
        Widgets.Label(nameRect, p.Name.ToStringFull);
        if (_listing.ButtonText("Generate", 4))
        {
            string? lastName = null;
            if (keepLastName && p.Name is NameTriple triple) lastName = triple.Last;

            p.Name = PawnBioAndNameGenerator.GenerateFullPawnName(p.def, p.kindDef.GetNameMaker(p.gender), p.story,
                _selectedXenotype, p.RaceProps.GetNameGenerator(_selectedGender),
                _selectedCulture, p.IsCreepJoiner, _selectedGender, p.RaceProps.nameCategory, lastName, forceNoNick);
        }

        _listing.End();

        var height = _listing.TotalHeight;
        if (!Mathf.Approximately(windowRect.height, height)) windowRect.height = height + Margin + 2f;
    }
}