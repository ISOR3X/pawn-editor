using UnityEngine;
using Verse;

namespace PawnEditor;

public static class ColorUtility
{
    
    private static Texture2D? _hueTexture;
    private static bool _hueSliderDragging;
    private static (float hue, Texture2D tex) _colorRectCache;
    private static bool _colorRectDragging;

    public static void HueSlider(Rect inRect, ref Color color)
    {
        if (_hueTexture == null)
        {
            _hueTexture = new Texture2D(1, 360, TextureFormat.RGBA32, false);
            for (var y = 0; y < 360; y++)
                _hueTexture.SetPixel(0, y, Color.HSVToRGB((359 - y) / 360f, 1f, 1f));
            _hueTexture.Apply();
        }

        Color.RGBToHSV(color, out var h, out _, out _);
        if (h >= 1f) h = 0f; // Prevent wrapping around.

        var range = inRect.yMax - inRect.yMin;
        var yPosition = inRect.yMin + h * range;

        var ev = Event.current;
        if (ev.button == 0)
        {
            if (ev.type == EventType.MouseDown && inRect.Contains(ev.mousePosition))
                _hueSliderDragging = true;
            if (ev.type == EventType.MouseUp)
                _hueSliderDragging = false;
        }

        if (_hueSliderDragging && (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag))
        {
            var my = Mathf.Clamp(ev.mousePosition.y, inRect.yMin, inRect.yMax);
            var newH = Mathf.Min((my - inRect.yMin) / range, 359f / 360f); // Clamp to prevent wraparound
            Color.RGBToHSV(color, out _, out var cs, out var cv);
            color = Color.HSVToRGB(newH, cs, cv);
            yPosition = my;
            if (ev.type == EventType.MouseDrag) ev.Use();
        }

        GUI.DrawTexture(inRect, _hueTexture);
        GUI.DrawTextureWithTexCoords(new Rect(inRect.center.x - 6f, yPosition - 6f, 12f, 12f),
            Verse.Widgets.SelectionArrow, new Rect(0f, 0f, 1f, 1f), true);
    }



    public static void ColorRect(Rect inRect, ref Color color)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);

        // Regenerate texture when hue changes by more than half a degree.
        if (_colorRectCache.tex == null || Mathf.Abs(_colorRectCache.hue - h) > 1f / 720f)
        {
            var w = Mathf.Max(1, (int)inRect.width);
            var ht = Mathf.Max(1, (int)inRect.height);
            var tex = new Texture2D(w, ht, TextureFormat.RGBA32, false);
            for (var x = 0; x < w; x++)
            {
                var sat = x / (w - 1f);
                for (var y = 0; y < ht; y++)
                {
                    var val = y / (ht - 1f); // y=0 → black, y=ht-1 → bright
                    tex.SetPixel(x, y, Color.HSVToRGB(h, sat, val));
                }
            }
            tex.Apply();
            _colorRectCache = (h, tex);
        }

        GUI.DrawTexture(inRect, _colorRectCache.tex);

        // Draw selection circle cursor at current S/V position (V=1 at top).
        var cursorX = inRect.x + s * inRect.width;
        var cursorY = inRect.y + (1f - v) * inRect.height;
        const float circleSize = 14f;
        GUI.DrawTexture(
            new Rect(cursorX - circleSize / 2f, cursorY - circleSize / 2f, circleSize, circleSize),
            Verse.Widgets.ColorSelectionCircle, ScaleMode.ScaleToFit, true, 1f,
            v > 0.5f ? Color.black : Color.white, 0f, 0f);

        // Interaction: update S/V on drag.
        var ev = Event.current;
        if (ev.button == 0)
        {
            if (ev.type == EventType.MouseDown && inRect.Contains(ev.mousePosition))
                _colorRectDragging = true;
            if (ev.type == EventType.MouseUp)
                _colorRectDragging = false;
        }

        if (_colorRectDragging && (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag))
        {
            var newS = Mathf.Clamp01((ev.mousePosition.x - inRect.x) / inRect.width);
            var newV = Mathf.Clamp01(1f - (ev.mousePosition.y - inRect.y) / inRect.height);
            color = Color.HSVToRGB(h, newS, newV);
            if (ev.type == EventType.MouseDrag) ev.Use();
        }
    }
    
    public static bool ApproximatelyEqual(Color a, Color b, float epsilon = 4f / 255f)
        => Mathf.Abs(a.r - b.r) < epsilon && Mathf.Abs(a.g - b.g) < epsilon && Mathf.Abs(a.b - b.b) < epsilon;

    public static void ColorToHSL(Color color, out float h, out float s, out float l)
    {
        Color.RGBToHSV(color, out h, out var sv, out var v);
        l = v * (1f - sv / 2f);
        s = (l == 0f || l == 1f) ? 0f : (v - l) / Mathf.Min(l, 1f - l);
    }

    public static Color HSLToColor(float h, float s, float l)
    {
        var v = l + s * Mathf.Min(l, 1f - l);
        var sv = v == 0f ? 0f : 2f * (1f - l / v);
        return Color.HSVToRGB(h, sv, v);
    }
}