using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class FloatWindow : Window
{
    protected virtual FloatWindowAlignment alignment => FloatWindowAlignment.BottomRight;
    protected virtual bool UseWidgetWidth => false;
    private static readonly Vector2 InitialPositionShift = new(0, 8f);
    private readonly Rect boundWidgetRect;

    protected FloatWindow(Rect boundWidgetRect)
    {
        this.boundWidgetRect = boundWidgetRect;
        onlyOneOfTypeAllowed = true;
        layer = WindowLayer.Super;
        // closeOnClickedOutside = true;
        // doCloseX = true;
    }

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        if (UseWidgetWidth) windowRect.width = boundWidgetRect.width;
        this.windowRect.position = CalculatePositionFromBoundWidget(boundWidgetRect);
    }

    public override void ExtraOnGUI()
    {
        base.ExtraOnGUI();
        this.CloseIfOutOfBouds();
        this.CloseIfClickedOutside();
    }

    public Vector2 CalculatePositionFromBoundWidget(Rect widgetRect)
    {
        widgetRect = UI.GUIToScreenRect(widgetRect);
        Vector2 position = widgetRect.position;
        switch (alignment)
        {
            case FloatWindowAlignment.BottomRight:
                position.x += widgetRect.width - windowRect.width + InitialPositionShift.x;
                position.y += widgetRect.height + InitialPositionShift.y;
                break;
            case FloatWindowAlignment.BottomLeft:
                position.x += InitialPositionShift.x;
                position.y += widgetRect.height + InitialPositionShift.y;
                break;
            default:
                throw new ArgumentOutOfRangeException(alignment.ToString());
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

    private void CloseIfOutOfBouds()
    {
        if (windowRect.Contains(Event.current.mousePosition))
            return;
        float num = GenUI.DistFromRect(windowRect, Event.current.mousePosition);
        if (num <= 95.0)
            return;
        this.Close(false);
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