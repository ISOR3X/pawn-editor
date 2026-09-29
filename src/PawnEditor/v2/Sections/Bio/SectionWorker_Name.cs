using RimWorld;
using Taffy;
using Verse;
using Void.Taffy;

namespace PawnEditor.v2;

public class SectionWorker_Name : SectionWorker<Pawn>
{
    public override bool ShowFor(object obj) => base.ShowFor(obj) && obj is Pawn { Faction: not null, Name: not null };

    public override void DoSectionContents(UIBranch b, Pawn pawn)
    {
        // TODO: Title renaming?
        b.SectionLabel("Name");

        b.Div(b2 =>
        {
            switch (pawn.Name)
            {
                case NameTriple triple:
                    {
                        b2.Input(triple.First, t => SetName(first: t), 12, CharacterCardUtility.ValidNameRegex,
                            onHover: r => TooltipHandler.TipRegionByKey(r, "FirstNameDesc"),
                            style: new Style { minWidth = Dimension.Px(0) });
                        b2.Input(triple.Nick, t => SetName(nick: t), 16, CharacterCardUtility.ValidNameRegex,
                            onHover: r => TooltipHandler.TipRegionByKey(r, "ShortIdentifierDesc"),
                            disabled: triple.Nick == triple.First || triple.Nick == triple.Last,
                            style: new Style { minWidth = Dimension.Px(0) });
                        b2.Input(triple.Last, t => SetName(last: t), 12, CharacterCardUtility.ValidNameRegex,
                            onHover: r => TooltipHandler.TipRegionByKey(r, "LastNameDesc"),
                            style: new Style { minWidth = Dimension.Px(0) });
                        break;
                    }
                case NameSingle single:
                    b2.Input(single.Name, t => pawn.Name = new NameSingle(t), 16,
                        CharacterCardUtility.ValidNameRegex);
                    break;
                default:
                    b2.Text(pawn.Name.ToStringFull);
                    break;
            }
        }, style: new Style
        {
            display = TaffyDisplay.Flex,
            flexDirection = TaffyFlexDirection.Row,
            flexWrap = TaffyFlexWrap.Wrap,
            gap = new TaffyAxes(Dimension.Px(GenUI.GapTiny))
        });

        b.Button(icon: TexButton.Rename, variant: VoidComponents.ButtonVariant.Ghost, onClick: r =>
        {
            Messages.Message("Not implemented yet", MessageTypeDefOf.RejectInput);
            // FloatWindow.ToggleState(r, () => new FloatWindow_NamePawn(r, pawn, Find.WindowStack.WindowOfType<Window_Editor>()));
        });

        return;

        /// <summary>
        /// Convinience setter for a name. Void inputs must write directly in order to work.
        /// </summary>
        void SetName(string? first = null, string? nick = null, string? last = null)
        {
            if (pawn.Name is not NameTriple current) return;
            var f = first ?? current.First;
            var n = nick ?? current.Nick;
            var l = last ?? current.Last;
            pawn.Name = new NameTriple(f, string.IsNullOrEmpty(n) ? f : n, l);
        }
    }
}
