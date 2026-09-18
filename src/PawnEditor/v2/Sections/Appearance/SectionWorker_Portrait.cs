using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;
using Void.XMLComponents;

namespace PawnEditor.v2;

public class SectionWorker_Portrait : SectionWorker<Thing>
{
    private static readonly Style _portaitStyle = new() { width = Dimension.Px(200f), height = Dimension.Px(200f) };

    public override void DoSectionContents(UIBranch b, Thing thing)
    {
        if (thing is Pawn p)
        {
            b.Div(
                draw: r =>
                {
                    var image = PortraitsCache.Get(p, new Vector2(r.width, r.height), Rot4.South,
                        Dialog_StylingStation.PortraitOffset, 1.1f,
                        renderHeadgear: true,
                        renderClothes: true);
                    GUI.DrawTexture(r, image);
                },
                style: _portaitStyle
            );
        }
    }
}
