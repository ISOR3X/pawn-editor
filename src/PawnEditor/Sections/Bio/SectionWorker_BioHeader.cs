using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioHeader : SectionWorker
{
    public SectionWorker_BioHeader(SectionDef def) : base(def)
    {
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        var inspectPaneRect = inRect.TakeTopPart(120f);
        UIComponents.InspectPane(inspectPaneRect.TakeLeftPart(400f), pawn);

        // Pawn preview
        Rect imageRect = inspectPaneRect.LeftPartPixels(inspectPaneRect.height);
        var image = PawnUtility.GetScaledPortrait(pawn, imageRect);
        GUI.DrawTexture(imageRect.ExpandedBy(20f), image);

        Rect footerRect = inRect.TakeTopPart(UIUtility.ButtonHeight);
        WidgetRow row = new WidgetRow(footerRect.x, footerRect.y, UIDirection.RightThenDown);
        if (row.ButtonText("Quick actions", fixedWidth: 120f)) Find.WindowStack.Add(new FloatMenu(this.tab.quickActions));

        if (PawnUtility.GetPawnCategory(pawn) is PawnUtility.PawnCategory.Humanlike)
        {
            if (row.ButtonText("Randomize", fixedWidth: 120f))
            {
                PawnUtility.RandomizeInPlace(pawn);
            }

            row.ButtonIcon(TexPawnEditor.Reroll, "Reroll");
            if (row.ButtonIcon(TexButton.Info, "Info")) Find.WindowStack.Add(new Dialog_InfoCard(pawn));
        }
    }
}