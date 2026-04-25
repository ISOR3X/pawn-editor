using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using FlexDirection = Taffy.FlexDirection;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Name(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p) => base.ShowSection(p) && p is { Faction: not null, Name: not null };

    public override void OnLayout(Layout layout, Pawn pawn)
    {
        DoNameInputs(layout.ComponentById<DivElement>("name_block"), pawn);

        layout.ComponentById<ButtonElement>("generator").OnClick = r =>
            FloatWindow.ToggleState(r,
                () => new FloatWindow_NamePawn(r, pawn, Find.WindowStack.WindowOfType<Window_Editor>()));
    }

    private static void DoNameInputs(DivElement div, Pawn pawn)
    {
        // TODO: Add title renaming
        div.Children = b =>
        {
            switch (pawn.Name)
            {
                case NameTriple triple:
                {
                    var first = triple.First;
                    var nick = triple.Nick;
                    var last = triple.Last;
                    b.Input(ref first, 12, CharacterCardUtility.ValidNameRegex,
                        onHover: r => TooltipHandler.TipRegionByKey(r, "FirstNameDesc"), style: new StyleOverride {minWidth = 0});
                    b.Input(ref nick, 16, pattern: CharacterCardUtility.ValidNameRegex,
                        onHover: r => TooltipHandler.TipRegionByKey(r, "ShortIdentifierDesc"),
                        disabled: triple.Nick == triple.First || triple.Nick == triple.Last, style: new StyleOverride {minWidth = 0});
                    b.Input(ref last, 12, CharacterCardUtility.ValidNameRegex,
                        onHover: r => TooltipHandler.TipRegionByKey(r, "LastNameDesc"), style: new StyleOverride {minWidth = 0});

                    if (first != triple.First || nick != triple.Nick || last != triple.Last)
                        pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);
                    break;
                }
                case NameSingle single:
                {
                    var name = single.ToStringFull;
                    b.Input(ref name, 16);
                    if (name != single.ToStringFull)
                        pawn.Name = new NameSingle(name);
                    break;
                }
                default:
                    b.Text(pawn.Name.ToStringFull);
                    break;
            }
        };
    }
}