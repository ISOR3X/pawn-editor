using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class Window_AddItemNew(Table<BackstoryDef> table, ContentSourceFilter<BackstoryDef> filter, Pawn pawn) : Window
{
    public override Vector2 InitialSize => Page.StandardSize - new Vector2(128f, 128f);

    public override void DoWindowContents(Rect inRect)
    {
        var footerRect = inRect.TakeBottomPart(UIUtility.ButtonHeight);
        inRect.Gap();
        var leftRect = inRect.TakeLeftPart(200f);
        inRect.Indent();

        var listing = new Listing_Standard();
        listing.Begin(leftRect);

        var filterLabel = filter.Selected?.Name ?? "Any";
        if (listing.ButtonText($"Source: {filterLabel}"))
        {
            var opts = LoadedModManager.RunningMods
                .Select(pack => new FloatMenuOption(pack.Name, () => { filter.Selected = pack; table.SetDirty(); }))
                .Prepend(new FloatMenuOption("Any", () => { filter.Selected = null; table.SetDirty(); }))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        listing.End();

        table.Draw(inRect);

        Verse.Widgets.Label(footerRect, table.Selected?.TitleCapFor(pawn.gender) ?? "No item selected");
    }
}
