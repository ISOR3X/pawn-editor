using UnityEngine;
using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_Ideology(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var curIdeo = pawn.ideo.Ideo;
        var btn = layout.ComponentById<ButtonElement>("button");
        
        btn.Label = curIdeo?.name ?? "None";
        btn.Icon = curIdeo?.icon ?? Verse.Widgets.PlaceholderIconTex;
        btn.IconColor = curIdeo?.Color ?? Color.white;

        btn.OnClick = _ =>
        {
            List<FloatMenuOption> opts =
            [
                ..Find.IdeoManager.IdeosInViewOrder.Select(ideo =>
                    new FloatMenuOption(ideo.name, () => pawn.ideo.SetIdeo(ideo), ideo.Icon,
                        ideo.Color)),
                new("None".Colorize(ColoredText.SubtleGrayColor), () => pawn.ideo.SetIdeo(null),
                    Verse.Widgets.PlaceholderIconTex, Color.white)
            ];
            Find.WindowStack.Add(new FloatMenu(opts));
        };
    }
}