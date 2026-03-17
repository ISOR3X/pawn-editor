using System;
using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class FloatWindow : Window
{
    protected virtual Vector2 InitialPositionShift => new(0, 8f);
    protected virtual bool UseWidgetWidth => false;
    private readonly Rect _boundWidgetRect;
    private Window? _ownerInstance;

    protected FloatWindow(Rect boundWidgetRect)
    {
        _boundWidgetRect = boundWidgetRect;
        onlyOneOfTypeAllowed = true;
        layer = WindowLayer.SubSuper;
        closeOnClickedOutside = true;
    }

    protected virtual Window? Owner => null;

    protected virtual FloatWindowAlignment Alignment => FloatWindowAlignment.BottomRight;

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        if (UseWidgetWidth)
            windowRect.width = _boundWidgetRect.width;
        windowRect.position = CalculatePositionFromBoundWidget(_boundWidgetRect);
    }


    private Vector2 CalculatePositionFromBoundWidget(Rect widgetRect)
    {
        widgetRect = UI.GUIToScreenRect(widgetRect);
        var position = widgetRect.position;
        switch (Alignment)
        {
            case FloatWindowAlignment.BottomRight:
                position.x += widgetRect.width - windowRect.width + InitialPositionShift.x;
                position.y += widgetRect.height + InitialPositionShift.y;
                break;
            case FloatWindowAlignment.BottomLeft:
                position.x += InitialPositionShift.x;
                position.y += widgetRect.height + InitialPositionShift.y;
                break;
            case FloatWindowAlignment.BottomCenter:
                position.x += (widgetRect.width - windowRect.width) / 2 + InitialPositionShift.x;
                position.y += widgetRect.height + InitialPositionShift.y;
                break;
            case FloatWindowAlignment.TopLeft:
                position.x += InitialPositionShift.x;
                position.y -= windowRect.height + InitialPositionShift.y;
                break;
            case FloatWindowAlignment.TopRight:
                position.x += widgetRect.width - windowRect.width + InitialPositionShift.x;
                position.y -= windowRect.height + InitialPositionShift.y;
                break;
            case FloatWindowAlignment.TopCenter:
                position.x += (widgetRect.width - windowRect.width) / 2 + InitialPositionShift.x;
                position.y -= windowRect.height + InitialPositionShift.y;
                break;
            case FloatWindowAlignment.CenterLeft:
                position.x -= windowRect.width + InitialPositionShift.x;
                position.y += (widgetRect.height - windowRect.height) / 2 + InitialPositionShift.y;
                break;
            case FloatWindowAlignment.CenterRight:
                position.x += widgetRect.width + InitialPositionShift.x;
                position.y += (widgetRect.height - windowRect.height) / 2 + InitialPositionShift.y;
                break;
            default:
                throw new ArgumentOutOfRangeException(Alignment.ToString());
        }

        const float margin = 10f;
        position.x = Mathf.Clamp(position.x, 0 + margin, Screen.width - margin);
        position.y = Mathf.Clamp(position.y, 0 + margin, Screen.height - margin);

        return position;
    }

    public override void PostOpen()
    {
        base.PostOpen();
        _ownerInstance = Owner;
    }

    public override void ExtraOnGUI()
    {
        base.ExtraOnGUI();
        if (_ownerInstance != null && !Find.WindowStack.IsOpen(_ownerInstance))
            Close(false);
    }

    public static void ToggleState<T>(Rect widgetRect, Func<T> factory) where T : FloatWindow
    {
        var window = Find.WindowStack.WindowOfType<T>();
        if (window != null && window.CalculatePositionFromBoundWidget(widgetRect) == window.windowRect.position)
        {
            window.Close();
        }
        else
        {
            Find.WindowStack.Add(factory());
        }
    }
}