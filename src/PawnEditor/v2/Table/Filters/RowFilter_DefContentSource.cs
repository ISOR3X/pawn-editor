using UnityEngine;
using Verse;
using Void.Native;
using static Void.Native.Components;

namespace PawnEditor.v2;

/// <summary>
///     Lets through the defs of one mod, picked from a menu.
/// </summary>
public sealed class RowFilter_DefContentSource<TDef> : RowFilter<TDef> where TDef : Def
{
    private static readonly Style LabelStyle = new() { fontSize = GameFont.Tiny };

    private static readonly Style ButtonStyle = new()
    {
        margin = new TaffyEdges(Dimension.Px(0f), Dimension.Px(0f), Dimension.Px(GenUI.GapSmall), Dimension.Px(0f))
    };

    private ModContentPack? _selected;

    public override bool Passes(TDef row)
    {
        return _selected == null || row.modContentPack == _selected;
    }

    public override void DrawFilter(UIBranch builder)
    {
        builder.Text("Content source", LabelStyle);
        builder.Button(_selected?.Name ?? "Any", block: true, style: ButtonStyle, onClick: _ =>
        {
            var options = LoadedModManager.RunningMods
                .Where(pack => pack.AllDefs.OfType<TDef>().Any())
                .Select(pack => new FloatMenuOption(pack.Name, () =>
                {
                    _selected = pack;
                    MarkDirty();
                }))
                .Prepend(new FloatMenuOption("Any", () =>
                {
                    _selected = null;
                    MarkDirty();
                }))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        });
    }
}
