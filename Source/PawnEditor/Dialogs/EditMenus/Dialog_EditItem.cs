using RimUI;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class Dialog_EditItem : Window
{
    protected readonly Pawn Pawn;
    private readonly UIElement element;
    private Listing_Horizontal listing = new();
    public Rect TableRect;
    private bool floatMenuLast;
    private bool setPosition;
    

    protected Dialog_EditItem(Pawn pawn = null, UIElement element = null)
    {
        Pawn = pawn;
        this.element = element;
        layer = WindowLayer.SubSuper;
        onlyOneOfTypeAllowed = true;
        absorbInputAroundWindow = false;
        forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
        closeOnClickedOutside = true;
    }

    protected virtual float MinWidth => 400;

    public override Vector2 InitialSize => new(790f - 12f, 500);

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        var rect = UI.GUIToScreenRect(TableRect);
        windowRect.width = Mathf.Max(MinWidth, element?.Width ?? InitialSize.x);
        windowRect.x = rect.x + 3;
        windowRect.y = rect.y - InitialSize.y;
        setPosition = false;
    }

    public override void PostClose()
    {
        base.PostClose();
        listing.ClearCache();
    }

    protected abstract void DoContents(Listing_Horizontal listing);

    public override void DoWindowContents(Rect inRect)
    {
        listing.Begin(inRect);
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) DoContents(listing);
        listing.End();
        
        if (Event.current.type is not EventType.Layout and not EventType.Ignore and not EventType.Repaint) ClearCaches();

        if (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.IsOpen)
        {
            if (floatMenuLast) ClearCaches();
            floatMenuLast = false;
        }
        else floatMenuLast = true;

        if (Event.current.type == EventType.Layout && !setPosition)
        {
            float newHeight = listing.curHeight + Margin * 2;
            windowRect.y += windowRect.height - newHeight;
            windowRect.height = newHeight;
            setPosition = true;
        }
    }

    protected virtual void ClearCaches()
    {
        if (element is UITable<Pawn> table) table?.ClearCache();
    }
}

public abstract class Dialog_EditItem<T> : Dialog_EditItem
{
    protected T Selected;

    protected Dialog_EditItem(T item, Pawn pawn = null, UIElement element = null) : base(pawn, element) => Selected = item;

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