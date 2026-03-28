using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Name(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p) => base.ShowSection(p) && p is { Faction: not null, Name: not null };

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text("Name", color: ColoredText.TipSectionTitleColor);
        builder.Div(new Style { flexDirection = FlexDirection.Row }, row =>
        {
            switch (pawn.Name)
            {
                case NameTriple triple:
                {
                    var first = triple.First;
                    var nick = triple.Nick;
                    var last = triple.Last;
                    row.Input(ref first, maxLength: 12, pattern: CharacterCardUtility.ValidNameRegex);

                    var c = triple.Nick == triple.First || triple.Nick == triple.Last
                        ? new Color(1f, 1f, 1f, 0.5f)
                        : Color.white;
                    row.Input(ref nick, maxLength: 16, color: c, pattern: CharacterCardUtility.ValidNameRegex);
                    row.Input(ref last, maxLength: 12, pattern: CharacterCardUtility.ValidNameRegex);

                    if (first != triple.First || nick != triple.Nick || last != triple.Last)
                        pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);
                    break;
                }
                case NameSingle single:
                {
                    var name = single.ToStringFull;
                    row.Input(ref name, maxLength: 16);
                    if (name != single.ToStringFull)
                        pawn.Name = new NameSingle(name);
                    break;
                }
                default:
                    row.Text(pawn.Name.ToStringFull);
                    break;
            }

            row.Button(icon: TexButton.Rename,
                onClick: r => { FloatWindow.ToggleState(r, () => new FloatWindow_NamePawn(r)); }, drawGraphic: false,
                style: new Style { margin = new Rect<LengthPercentageAuto>(4f, 0f, 0f, 0f) });
        });
    }

    // REF: CharacterCardUtility.DrawCharacterCard
    private static void DoNameInputRect(Rect inRect, Pawn pawn, bool advanced = false)
    {
        // TODO: Add proper icon
        if (advanced)
        {
            var iconRect = inRect.TakeRightPart(WidgetRow.IconSize);
            if (Verse.Widgets.ButtonImage(iconRect.CenteredVertically(WidgetRow.IconSize), TexButton.Add))
                FloatWindow.ToggleState(iconRect, () => new FloatWindow_NamePawn(iconRect));
        }

        var thirdWidth = inRect.width / 3f;
        var rect1 = inRect.TakeLeftPart(thirdWidth);
        switch (pawn.Name)
        {
            case NameTriple triple:
            {
                var rect2 = inRect.TakeLeftPart(thirdWidth);
                var rect3 = inRect.TakeLeftPart(thirdWidth);
                var first = triple.First;
                var nick = triple.Nick;
                var last = triple.Last;
                CharacterCardUtility.DoNameInputRect(rect1, ref first, 12);
                if (triple.Nick == triple.First || triple.Nick == triple.Last) GUI.color = new Color(1f, 1f, 1f, 0.5f);
                CharacterCardUtility.DoNameInputRect(rect2, ref nick, 16);
                GUI.color = Color.white;
                CharacterCardUtility.DoNameInputRect(rect3, ref last, 12);
                if (triple.First != first || triple.Nick != nick || triple.Last != last)
                    pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);

                TooltipHandler.TipRegionByKey(rect2, "ShortIdentifierDesc");
                TooltipHandler.TipRegionByKey(rect3, "LastNameDesc");
                break;
            }
            case NameSingle single:
            {
                var first = single.ToStringFull;
                CharacterCardUtility.DoNameInputRect(rect1, ref first, 16);
                if (pawn.Name.ToStringFull != first) pawn.Name = new NameSingle(first);
                break;
            }
            default:
                Verse.Widgets.Label(rect1, pawn.Name.ToStringFull);
                break;
        }

        TooltipHandler.TipRegionByKey(rect1, "FirstNameDesc");
    }
}