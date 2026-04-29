using RimWorld;
using UnityEngine;
using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_Ideology(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var curIdeo = pawn.ideo.Ideo;
        var btn = layout.ComponentById<ButtonElement>("button");

        btn.Label = curIdeo?.name ?? "None";
        btn.Icon = curIdeo?.Icon ?? Verse.Widgets.PlaceholderIconTex;
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
        btn.OnHover = r =>
        {
            if (curIdeo == null) return;
            var tip = MakeIdeoTooltip(pawn, curIdeo);
            TooltipHandler.TipRegion(r, tip);
        };
    }

    /// <summary>
    ///     Based on <see cref="IdeoUIUtility.DrawIdeoPlate" />
    /// </summary>
    private static TaggedString MakeIdeoTooltip(Pawn pawn, Ideo ideo)
    {
        TaggedString text = ideo.name.Colorize(ColoredText.TipSectionTitleColor);

        text += "\n" + "Certainty".Translate().CapitalizeFirst() + ": " + pawn.ideo.Certainty.ToStringPercent();

        if (pawn.ideo.PreviousIdeos.Any<Ideo>())
            text += "\n\n" + "Formerly".Translate().CapitalizeFirst() + ": \n" + pawn.ideo.PreviousIdeos
                .Select<Ideo, string>((Func<Ideo, string>)(x => x.name)).ToLineList("  - ");

        return text;
    }
}