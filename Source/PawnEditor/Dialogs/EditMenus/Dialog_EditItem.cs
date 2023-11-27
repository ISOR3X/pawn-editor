using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class Dialog_EditItem : Window
{
    protected readonly Pawn Pawn;
    private readonly UITable<Pawn> table;

    protected Dialog_EditItem(Pawn pawn = null, UITable<Pawn> table = null)
    {
        Pawn = pawn;
        this.table = table;
        onlyOneOfTypeAllowed = true;
        absorbInputAroundWindow = false;
        forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
    }

    public override Vector2 InitialSize => new(790f - 12f, 160f); // -12f because of the insets on the table.

    public override void SetInitialSizeAndPosition()
    {
        if (Find.WindowStack.WindowOfType<Dialog_PawnEditor>() is { } window)
            windowRect = new(window.windowRect.xMax + 12, window.windowRect.yMin, UI.screenWidth - window.windowRect.xMax - 24, window.windowRect.height);
        else base.SetInitialSizeAndPosition();
    }

    protected abstract void DoContents(Listing_Standard listing);

    public override void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, null)) DoContents(listing);
        listing.End();

        if (Event.current.type is not EventType.Layout and not EventType.Ignore and not EventType.Repaint) ClearCaches();
    }

    protected virtual void ClearCaches()
    {
        table?.ClearCache();
    }
}

public abstract class Dialog_EditItem<T> : Dialog_EditItem
{
    protected const float LABEL_WIDTH_PCT = 0.3f;
    protected const float CELL_HEIGHT = 30f;
    protected T Selected;

    protected Dialog_EditItem(T item, Pawn pawn = null, UITable<Pawn> table = null) : base(pawn, table) => Selected = item;

    public override void DoWindowContents(Rect inRect)
    {
        if (Selected == null) return;
        base.DoWindowContents(inRect);
    }

    public virtual void Select(T item)
    {
        Selected = item;
    }

    public virtual bool IsSelected(T item) => ReferenceEquals(Selected, item);
}
