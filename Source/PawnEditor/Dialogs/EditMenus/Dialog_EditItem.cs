using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class Dialog_EditItem : Window
{
    protected const float LABEL_WIDTH_PCT = 0.3f;
    protected const float CELL_HEIGHT = 30f;
    protected readonly Pawn Pawn;
    private readonly UITable<Pawn> table;
    public Rect TableRect;
    private bool setPosition;

    protected Dialog_EditItem(Pawn pawn = null, UITable<Pawn> table = null)
    {
        Pawn = pawn;
        this.table = table;
        onlyOneOfTypeAllowed = true;
        absorbInputAroundWindow = false;
        forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
        layer = WindowLayer.Super;
    }

    protected virtual float MinWidth => 400;

    public override Vector2 InitialSize => new(790f - 12f, 500);

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        var rect = UI.GUIToScreenRect(TableRect);
        windowRect.width = Mathf.Max(MinWidth, table?.Width ?? InitialSize.x);
        windowRect.x = rect.x - windowRect.width + 124;
        windowRect.y = rect.y - InitialSize.y;
        setPosition = false;
    }

    protected abstract void DoContents(Listing_Standard listing);
    protected virtual int GetColumnCount(Rect inRect) => Mathf.FloorToInt(inRect.width / 372);

    public override void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        var columnCount = GetColumnCount(inRect);
        if (columnCount == 1)
            listing.maxOneColumn = true;
        else
        {
            listing.ColumnWidth /= columnCount;
            listing.ColumnWidth -= 17;
        }

        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, null)) DoContents(listing);
        listing.End();

        if (Event.current.type is not EventType.Layout and not EventType.Ignore and not EventType.Repaint) ClearCaches();

        if (Event.current.type == EventType.Layout && !setPosition)
        {
            var cellCount = Mathf.Ceil((listing.CurHeight - 4) / CELL_HEIGHT);
            var cellsPerColumn = Mathf.Ceil(cellCount / columnCount);
            var newHeight = cellsPerColumn * (CELL_HEIGHT + 1) + Margin * 2;
            windowRect.y += windowRect.height - newHeight;
            windowRect.height = newHeight;
            setPosition = true;
        }
    }

    protected virtual void ClearCaches()
    {
        table?.ClearCache();
    }
}

public abstract class Dialog_EditItem<T> : Dialog_EditItem
{
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
        SetInitialSizeAndPosition();
    }

    public virtual bool IsSelected(T item) => ReferenceEquals(Selected, item);
}
