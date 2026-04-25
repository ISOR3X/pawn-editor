using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

public class SectionWorker_Name(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p)
    {
        return base.ShowSection(p) && p is { Faction: not null, Name: not null };
    }

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        // TODO: Add title renaming
        builder.Text("Name", color: ColoredText.TipSectionTitleColor);
        builder.Div(row =>
        {
            switch (pawn.Name)
            {
                case NameTriple triple:
                {
                    var first = triple.First;
                    var nick = triple.Nick;
                    var last = triple.Last;
                    row.Input(ref first, 12, CharacterCardUtility.ValidNameRegex,
                        onHover: r => TooltipHandler.TipRegionByKey(r, "FirstNameDesc"));

                    var c = triple.Nick == triple.First || triple.Nick == triple.Last
                        ? new Color(1f, 1f, 1f, 0.5f)
                        : Color.white;
                    row.Input(ref nick, 16, color: c, pattern: CharacterCardUtility.ValidNameRegex,
                        onHover: r => TooltipHandler.TipRegionByKey(r, "ShortIdentifierDesc"));
                    row.Input(ref last, 12, CharacterCardUtility.ValidNameRegex,
                        onHover: r => TooltipHandler.TipRegionByKey(r, "LastNameDesc"));

                    if (first != triple.First || nick != triple.Nick || last != triple.Last)
                        pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);
                    break;
                }
                case NameSingle single:
                {
                    var name = single.ToStringFull;
                    row.Input(ref name, 16);
                    if (name != single.ToStringFull)
                        pawn.Name = new NameSingle(name);
                    break;
                }
                default:
                    row.Text(pawn.Name.ToStringFull);
                    break;
            }

            row.Button(icon: TexButton.Rename,
                onClick: r =>
                {
                    FloatWindow.ToggleState(r,
                        () => new FloatWindow_NamePawn(r, pawn, Find.WindowStack.WindowOfType<Window_Editor>()));
                },
                variant: TaffyExtensions.ButtonVariant.Ghost,
                style: new StyleOverride { margin = new Rect<LengthPercentageAuto>(4f, 0f, 0f, 0f) });
        }, new StyleOverride { flexDirection = FlexDirection.Row });
    }
}