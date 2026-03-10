using System;
using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class FloatWindow : Window
{
    private static readonly Vector2 InitialPositionShift = new(0, 8f);
    private readonly Rect _boundWidgetRect;
    protected virtual Window? Owner => null;
    private Window? _ownerInstance;

    protected FloatWindow(Rect boundWidgetRect)
    {
        _boundWidgetRect = boundWidgetRect;
        onlyOneOfTypeAllowed = true;
        layer = WindowLayer.Super;
        closeOnClickedOutside = true;
    }

    protected virtual FloatWindowAlignment Alignment => FloatWindowAlignment.BottomRight;

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
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


    public static void ToggleState<T>(Rect widgetRect) where T : FloatWindow
    {
        var window = Find.WindowStack.WindowOfType<T>();
        if (window != null && window.CalculatePositionFromBoundWidget(widgetRect) == window.windowRect.position)
        {
            window.Close();
        }
        else
        {
            var newWindow = (FloatWindow)Activator.CreateInstance(typeof(T), widgetRect);
            Find.WindowStack.Add(newWindow);
        }
    }
}