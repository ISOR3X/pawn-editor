using UnityEngine;
using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_IdeologyRole(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var btn = layout.ComponentById<ButtonElement>("button");
        var curRole = pawn.Ideo.GetRole(pawn);

        btn.Label = curRole?.Label ?? "None";
        btn.Icon = curRole?.Icon ?? Verse.Widgets.PlaceholderIconTex;
        btn.IconColor = curRole != null ? pawn.Ideo?.Color : Color.white;
        
        btn.OnClick = _ =>
        {
            if (pawn.Ideo == null) return;

            List<FloatMenuOption> opts =
            [
                ..pawn.Ideo.cachedPossibleRoles.Select(ideoRole =>
                    new FloatMenuOption(ideoRole.LabelForPawn(pawn),
                        () =>
                        {
                            if (curRole == ideoRole) return;
                            curRole?.Unassign(pawn, false);
                            ideoRole.Assign(pawn, true);
                        },
                        ideoRole.Icon, pawn.Ideo.Color
                    )),
                new("None".Colorize(ColoredText.SubtleGrayColor), () => { curRole?.Unassign(pawn, false); },
                    Verse.Widgets.PlaceholderIconTex, Color.white)
            ];

            Find.WindowStack.Add(new FloatMenu(opts));
        };
        btn.OnHover = r =>
        {
            if (curRole == null) return;
            var tip = curRole.GetTip();
            TooltipHandler.TipRegion(r, tip);
        };
    }
}