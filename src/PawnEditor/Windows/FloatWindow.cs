using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
public abstract class FloatWindow : Window
{
    private static readonly Vector2 InitialPositionShift = new(0, 8f);
    private readonly Rect _boundWidgetRect;

    protected FloatWindow(Rect boundWidgetRect)
    {
        _boundWidgetRect = boundWidgetRect;
        onlyOneOfTypeAllowed = true;
        layer = WindowLayer.Super;
        // closeOnClickedOutside = true;
        // doCloseX = true;
    }

    protected virtual FloatWindowAlignment Alignment => FloatWindowAlignment.BottomRight;
    protected virtual bool UseWidgetWidth => false;

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        if (UseWidgetWidth) windowRect.width = _boundWidgetRect.width;
        windowRect.position = CalculatePositionFromBoundWidget(_boundWidgetRect);
    }

    public override void ExtraOnGUI()
    {
        base.ExtraOnGUI();
        CloseIfOutOfBounds();
        CloseIfClickedOutside();
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

    private void CloseIfOutOfBounds()
    {
        if (windowRect.Contains(Event.current.mousePosition))
            return;
        var num = GenUI.DistFromRect(windowRect, Event.current.mousePosition);
        if (num <= 95.0)
            return;
        Close(false);
    }

    private void CloseIfClickedOutside()
    {
        // TODO: If the boundWidget is inside of a WidgetGroup, GUIUtility.GUIToScreenRect will not work.
        // var widgetRect = GUIUtility.GUIToScreenRect(boundWidgetRect);
        // Vector2 groupPosition = GUIUtility.GUIToScreenPoint(Vector2.zero);
        // if (!widgetRect.Contains(Event.current.mousePosition))
        //     Close();
    }
}