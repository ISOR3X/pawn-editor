using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_IdeologyRole(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var btn = layout.ComponentById<ButtonElement>("button");
        var curTitle = pawn.Ideo.GetRole(pawn);

        btn.Label = curTitle?.Label ?? "None";
        btn.OnClick = _ =>
        {
            List<FloatMenuOption> opts =
            [
                ..pawn.Ideo.cachedPossibleRoles.Select(ideoRole =>
                    new FloatMenuOption(ideoRole.LabelForPawn(pawn),
                        () =>
                        {
                            if (curTitle == ideoRole) return;
                            curTitle?.Unassign(pawn, false);
                            ideoRole.Assign(pawn, true);
                        }
                    )),
                new("None", () => { curTitle?.Unassign(pawn, false); })
            ];

            Find.WindowStack.Add(new FloatMenu(opts));
        };
    }
}

public class t : GameComponent
{
    
}