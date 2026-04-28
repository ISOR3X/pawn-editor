using RimWorld;
using UnityEngine;
using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_Portrait(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var div = layout.ComponentById<DivElement>("portrait");
        var rot = ParseRot(layout.Attrs.GetValueOrDefault("dir"));

        div.Draw = r =>
        {
            var image = PortraitsCache.Get(pawn, new Vector2(r.width, r.height), rot,
                Dialog_StylingStation.PortraitOffset, 1.1f,
                renderHeadgear: Window_Editor.ShowHeadgear,
                renderClothes: Window_Editor.ShowClothes);
            GUI.DrawTexture(r, image);
        };
    }

    private static Rot4 ParseRot(string dir)
    {
        return dir switch
        {
            "north" => Rot4.North,
            "east" => Rot4.East,
            "west" => Rot4.West,
            _ => Rot4.South
        };
    }
}