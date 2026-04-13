using System.Globalization;
using Taffy;

namespace PawnEditor;

internal static class TaffyStyleParser
{
    /// <summary>
    /// Parses a CSS inline style string (e.g. <c>"flex-direction: row; gap: 4px"</c>) into a
    /// <see cref="StyleOverride"/>. Unknown properties are logged as warnings and skipped.
    /// </summary>
    public static StyleOverride ParseInlineStyle(string css)
    {
        var style = new StyleOverride();
        foreach (var declaration in css.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIdx = declaration.IndexOf(':');
            if (colonIdx < 0) continue;
            var name = declaration[..colonIdx].Trim();
            var value = declaration[(colonIdx + 1)..].Trim();
            if (name.Length > 0 && value.Length > 0)
                ApplyProperty(name, value, style);
        }
        return style;
    }

    /// <summary>
    /// Applies a single CSS property name/value pair to <paramref name="target"/>.
    /// Unknown properties are logged as warnings and skipped.
    /// </summary>
    public static void ApplyProperty(string name, string value, StyleOverride target)
    {
        switch (name)
        {
            case "display":
                target.display = ParseDisplay(value);
                break;
            case "flex-direction":
                target.flexDirection = ParseFlexDirection(value);
                break;
            case "flex-wrap":
                target.flexWrap = ParseFlexWrap(value);
                break;
            case "flex-grow":
                target.flexGrow = ParseFloat(value);
                break;
            case "flex-shrink":
                target.flexShrink = ParseFloat(value);
                break;
            case "flex-basis":
                target.flexBasis = ParseDimension(value);
                break;
            case "width":
                target.width = ParseDimension(value);
                break;
            case "height":
                target.height = ParseDimension(value);
                break;
            case "min-width":
                target.minWidth = ParseDimension(value);
                break;
            case "min-height":
                target.minHeight = ParseDimension(value);
                break;
            case "max-width":
                target.maxWidth = ParseMaxDimension(value);
                break;
            case "max-height":
                target.maxHeight = ParseMaxDimension(value);
                break;
            case "gap":
            {
                var v = LengthPercentage.Length(ParsePx(value));
                target.gap = new Size<LengthPercentage>(v, v);
                break;
            }
            case "column-gap":
            {
                var g = target.gap ?? default;
                g.Width = LengthPercentage.Length(ParsePx(value));
                target.gap = g;
                break;
            }
            case "row-gap":
            {
                var g = target.gap ?? default;
                g.Height = LengthPercentage.Length(ParsePx(value));
                target.gap = g;
                break;
            }
            case "padding":
            {
                var v = LengthPercentage.Length(ParsePx(value));
                target.padding = new Rect<LengthPercentage>(v, v, v, v);
                break;
            }
            case "padding-top":
            {
                var p = target.padding ?? default;
                p.Top = LengthPercentage.Length(ParsePx(value));
                target.padding = p;
                break;
            }
            case "padding-right":
            {
                var p = target.padding ?? default;
                p.Right = LengthPercentage.Length(ParsePx(value));
                target.padding = p;
                break;
            }
            case "padding-bottom":
            {
                var p = target.padding ?? default;
                p.Bottom = LengthPercentage.Length(ParsePx(value));
                target.padding = p;
                break;
            }
            case "padding-left":
            {
                var p = target.padding ?? default;
                p.Left = LengthPercentage.Length(ParsePx(value));
                target.padding = p;
                break;
            }
            case "margin":
            {
                var v = LengthPercentageAuto.Length(ParsePx(value));
                target.margin = new Rect<LengthPercentageAuto>(v, v, v, v);
                break;
            }
            case "margin-top":
            {
                var m = target.margin ?? default;
                m.Top = ParseLPA(value);
                target.margin = m;
                break;
            }
            case "margin-right":
            {
                var m = target.margin ?? default;
                m.Right = ParseLPA(value);
                target.margin = m;
                break;
            }
            case "margin-bottom":
            {
                var m = target.margin ?? default;
                m.Bottom = ParseLPA(value);
                target.margin = m;
                break;
            }
            case "margin-left":
            {
                var m = target.margin ?? default;
                m.Left = ParseLPA(value);
                target.margin = m;
                break;
            }
            case "align-items":
                target.alignItems = ParseAlignItems(value);
                break;
            case "align-self":
                target.alignSelf = ParseAlignItems(value);
                break;
            case "align-content":
                target.alignContent = ParseAlignContent(value);
                break;
            case "justify-content":
                target.justifyContent = ParseAlignContent(value);
                break;
            case "justify-items":
                target.justifyItems = ParseAlignItems(value);
                break;
            case "justify-self":
                target.justifySelf = ParseAlignItems(value);
                break;
            case "grid-template-columns":
                target.gridTemplateColumns = ParseTrackList(value);
                break;
            case "grid-template-rows":
                target.gridTemplateRows = ParseTrackList(value);
                break;
            case "grid-column-start":
            {
                var gc = target.gridColumn ?? default;
                gc.Start = ParseGridPlacement(value);
                target.gridColumn = gc;
                break;
            }
            case "grid-column-end":
            {
                var gc = target.gridColumn ?? default;
                gc.End = ParseGridPlacement(value);
                target.gridColumn = gc;
                break;
            }
            case "grid-row-start":
            {
                var gr = target.gridRow ?? default;
                gr.Start = ParseGridPlacement(value);
                target.gridRow = gr;
                break;
            }
            case "grid-row-end":
            {
                var gr = target.gridRow ?? default;
                gr.End = ParseGridPlacement(value);
                target.gridRow = gr;
                break;
            }
            default:
                Verse.Log.Warning($"[{PawnEditorMod.ModName}] Unknown style property '{name}', skipping.");
                break;
        }
    }

    private static float ParseFloat(string s) =>
        float.Parse(s, CultureInfo.InvariantCulture);

    private static float ParsePx(string s)
    {
        if (s.EndsWith("px", StringComparison.Ordinal))
            return float.Parse(s[..^2], CultureInfo.InvariantCulture);
        return float.Parse(s, CultureInfo.InvariantCulture);
    }

    private static Dimension ParseDimension(string s)
    {
        if (s == "auto") return Dimension.AUTO;
        if (s.EndsWith("%", StringComparison.Ordinal))
            return Dimension.Percent(float.Parse(s[..^1], CultureInfo.InvariantCulture) / 100f);
        if (s.EndsWith("px", StringComparison.Ordinal))
            return Dimension.Length(float.Parse(s[..^2], CultureInfo.InvariantCulture));
        return Dimension.Length(float.Parse(s, CultureInfo.InvariantCulture));
    }

    // max-width/max-height: "none" maps to AUTO (unconstrained).
    private static Dimension ParseMaxDimension(string s)
    {
        if (s == "none") return Dimension.AUTO;
        return ParseDimension(s);
    }

    private static LengthPercentageAuto ParseLPA(string s)
    {
        if (s == "auto") return LengthPercentageAuto.AUTO;
        if (s.EndsWith("%", StringComparison.Ordinal))
            return LengthPercentageAuto.Percent(float.Parse(s[..^1], CultureInfo.InvariantCulture) / 100f);
        return LengthPercentageAuto.Length(ParsePx(s));
    }

    private static Display ParseDisplay(string s) => s switch
    {
        "flex" => Display.Flex,
        "grid" => Display.Grid,
        "block" => Display.Block,
        "none" => Display.None,
        _ => Display.Flex,
    };

    private static FlexDirection ParseFlexDirection(string s) => s switch
    {
        "row" => FlexDirection.Row,
        "column" => FlexDirection.Column,
        "row-reverse" => FlexDirection.RowReverse,
        "column-reverse" => FlexDirection.ColumnReverse,
        _ => FlexDirection.Row,
    };

    private static FlexWrap ParseFlexWrap(string s) => s switch
    {
        "nowrap" => FlexWrap.NoWrap,
        "wrap" => FlexWrap.Wrap,
        "wrap-reverse" => FlexWrap.WrapReverse,
        _ => FlexWrap.NoWrap,
    };

    private static AlignItems? ParseAlignItems(string s) => s switch
    {
        "start" => AlignItems.Start,
        "end" => AlignItems.End,
        "flex-start" => AlignItems.FlexStart,
        "flex-end" => AlignItems.FlexEnd,
        "center" => AlignItems.Center,
        "baseline" => AlignItems.Baseline,
        "stretch" => AlignItems.Stretch,
        _ => null,
    };

    private static AlignContent? ParseAlignContent(string s) => s switch
    {
        "start" => AlignContent.Start,
        "end" => AlignContent.End,
        "flex-start" => AlignContent.FlexStart,
        "flex-end" => AlignContent.FlexEnd,
        "center" => AlignContent.Center,
        "stretch" => AlignContent.Stretch,
        "space-between" => AlignContent.SpaceBetween,
        "space-evenly" => AlignContent.SpaceEvenly,
        "space-around" => AlignContent.SpaceAround,
        _ => null,
    };

    private static List<TrackSizingFunction> ParseTrackList(string s)
    {
        var result = new List<TrackSizingFunction>();
        foreach (var token in s.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            result.Add(ParseTrack(token));
        return result;
    }

    private static TrackSizingFunction ParseTrack(string s)
    {
        if (s == "auto") return TrackSizingFunction.Auto();
        if (s.EndsWith("fr", StringComparison.Ordinal))
            return TrackSizingFunction.Fr(float.Parse(s[..^2], CultureInfo.InvariantCulture));
        if (s.EndsWith("%", StringComparison.Ordinal))
            return TrackSizingFunction.Percent(float.Parse(s[..^1], CultureInfo.InvariantCulture) / 100f);
        if (s.EndsWith("px", StringComparison.Ordinal))
            return TrackSizingFunction.Px(float.Parse(s[..^2], CultureInfo.InvariantCulture));
        return TrackSizingFunction.Px(float.Parse(s, CultureInfo.InvariantCulture));
    }

    private static GridPlacement ParseGridPlacement(string s)
    {
        if (s == "auto") return GridPlacement.Auto;
        if (s.StartsWith("span ", StringComparison.Ordinal))
            return GridPlacement.Span(int.Parse(s[5..], CultureInfo.InvariantCulture));
        return GridPlacement.Line(int.Parse(s, CultureInfo.InvariantCulture));
    }
}
