using HotSwap;
using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ThingColumnWorker_Edit : ThingColumnWorker
{
    protected override void DrawCellContent(Rect r, Thing row)
    {
        if (Verse.Widgets.ButtonImage(r.ContractedBy(4f), TexButton.NewItem, tooltip: "Edit item"))
        {
            FloatWindow.ToggleState(r, () => new FloatWindow_EditThing(r, row, Find.WindowStack.WindowOfType<Window_Editor>()));
        }
    }
}