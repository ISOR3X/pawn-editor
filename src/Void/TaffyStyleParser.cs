using System.Globalization;
using Taffy;
using Verse;

namespace Void;

public static class TaffyStyleParser
{
    /// <summary>
    ///     Parses a CSS inline style string (e.g. <c>"flex-direction: row; gap: 4px"</c>) into a
    ///     <see cref="StyleOverride" />. Unknown properties are logged as warnings and skipped.
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
    ///     Applies a single CSS property name/value pair to <paramref name="target" />.
    ///     Unknown properties are logged as warnings and skipped.
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
                var v = Dimension.Px(ParsePx(value));
                target.gap = new TaffyGap(v);
                break;
            }
            case "column-gap":
            {
                var g = target.gap ?? default;
                target.gap = new TaffyGap(Dimension.Px(ParsePx(value)), g.Row);
                break;
            }
            case "row-gap":
            {
                var g = target.gap ?? default;
                target.gap = new TaffyGap(g.Column, Dimension.Px(ParsePx(value)));
                break;
            }
            case "padding":
            {
                var v = Dimension.Px(ParsePx(value));
                target.padding = new TaffyEdges(v);
                break;
            }
            case "padding-top":
            {
                var p = target.padding ?? default;
                target.padding = p with { Top = Dimension.Px(ParsePx(value)) };
                break;
            }
            case "padding-right":
            {
                var p = target.padding ?? default;
                target.padding = p with { Right = Dimension.Px(ParsePx(value)) };
                break;
            }
            case "padding-bottom":
            {
                var p = target.padding ?? default;
                target.padding = p with { Bottom = Dimension.Px(ParsePx(value)) };
                break;
            }
            case "padding-left":
            {
                var p = target.padding ?? default;
                target.padding = p with { Left = Dimension.Px(ParsePx(value)) };
                break;
            }
            case "margin":
            {
                var v = ParseMarginDimension(value);
                target.margin = new TaffyEdges(v);
                break;
            }
            case "margin-top":
            {
                var m = target.margin ?? default;
                target.margin = m with { Top = ParseMarginDimension(value) };
                break;
            }
            case "margin-right":
            {
                var m = target.margin ?? default;
                target.margin = m with { Right = ParseMarginDimension(value) };
                break;
            }
            case "margin-bottom":
            {
                var m = target.margin ?? default;
                target.margin = m with { Bottom = ParseMarginDimension(value) };
                break;
            }
            case "margin-left":
            {
                var m = target.margin ?? default;
                target.margin = m with { Left = ParseMarginDimension(value) };
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
                SetGridStart(ref gc, value);
                target.gridColumn = gc;
                break;
            }
            case "grid-column-end":
            {
                var gc = target.gridColumn ?? default;
                SetGridEnd(ref gc, value);
                target.gridColumn = gc;
                break;
            }
            case "grid-row-start":
            {
                var gr = target.gridRow ?? default;
                SetGridStart(ref gr, value);
                target.gridRow = gr;
                break;
            }
            case "grid-row-end":
            {
                var gr = target.gridRow ?? default;
                SetGridEnd(ref gr, value);
                target.gridRow = gr;
                break;
            }
            case "font-size":
                target.fontSize = ParseGameFont(value);
                break;
            default:
                Log.Warning($"[{VoidMod.ModName}] Unknown style property '{name}', skipping.");
                break;
        }
    }

    private static GameFont? ParseGameFont(string s)
    {
        return s switch
        {
            "tiny" => GameFont.Tiny,
            "small" => GameFont.Small,
            "medium" => GameFont.Medium,
            _ => GameFont.Small
        };
    }

    private static float ParseFloat(string s)
    {
        return float.Parse(s, CultureInfo.InvariantCulture);
    }

    private static float ParsePx(string s)
    {
        if (s.EndsWith("px", StringComparison.Ordinal))
            return float.Parse(s[..^2], CultureInfo.InvariantCulture);
        return float.Parse(s, CultureInfo.InvariantCulture);
    }

    private static TaffyDimension ParseDimension(string s)
    {
        if (s == "auto") return Dimension.Auto();
        if (s.EndsWith("%", StringComparison.Ordinal))
            return Dimension.Percent(float.Parse(s[..^1], CultureInfo.InvariantCulture) / 100f);
        if (s.EndsWith("px", StringComparison.Ordinal))
            return Dimension.Px(float.Parse(s[..^2], CultureInfo.InvariantCulture));
        return Dimension.Px(float.Parse(s, CultureInfo.InvariantCulture));
    }

    // max-width / max-height: "none" maps to Auto (unconstrained).
    private static TaffyDimension ParseMaxDimension(string s)
    {
        if (s == "none") return Dimension.Auto();
        return ParseDimension(s);
    }

    private static TaffyDimension ParseMarginDimension(string s)
    {
        if (s == "auto") return Dimension.Auto();
        if (s.EndsWith("%", StringComparison.Ordinal))
            return Dimension.Percent(float.Parse(s[..^1], CultureInfo.InvariantCulture) / 100f);
        return Dimension.Px(ParsePx(s));
    }

    private static TaffyDisplay ParseDisplay(string s)
    {
        return s switch
        {
            "flex" => TaffyDisplay.Flex,
            "grid" => TaffyDisplay.Grid,
            "block" => TaffyDisplay.Block,
            "none" => TaffyDisplay.None,
            _ => TaffyDisplay.Flex
        };
    }

    private static TaffyFlexDirection ParseFlexDirection(string s)
    {
        return s switch
        {
            "row" => TaffyFlexDirection.Row,
            "column" => TaffyFlexDirection.Column,
            "row-reverse" => TaffyFlexDirection.RowReverse,
            "column-reverse" => TaffyFlexDirection.ColumnReverse,
            _ => TaffyFlexDirection.Row
        };
    }

    private static TaffyFlexWrap ParseFlexWrap(string s)
    {
        return s switch
        {
            "nowrap" => TaffyFlexWrap.NoWrap,
            "wrap" => TaffyFlexWrap.Wrap,
            "wrap-reverse" => TaffyFlexWrap.WrapReverse,
            _ => TaffyFlexWrap.NoWrap
        };
    }

    private static TaffyAlignItems? ParseAlignItems(string s)
    {
        return s switch
        {
            "start" => TaffyAlignItems.Start,
            "end" => TaffyAlignItems.End,
            "flex-start" => TaffyAlignItems.FlexStart,
            "flex-end" => TaffyAlignItems.FlexEnd,
            "center" => TaffyAlignItems.Center,
            "baseline" => TaffyAlignItems.Baseline,
            "stretch" => TaffyAlignItems.Stretch,
            _ => null
        };
    }

    private static TaffyAlignContent? ParseAlignContent(string s)
    {
        return s switch
        {
            "start" => TaffyAlignContent.Start,
            "end" => TaffyAlignContent.End,
            "flex-start" => TaffyAlignContent.FlexStart,
            "flex-end" => TaffyAlignContent.FlexEnd,
            "center" => TaffyAlignContent.Center,
            "stretch" => TaffyAlignContent.Stretch,
            "space-between" => TaffyAlignContent.SpaceBetween,
            "space-evenly" => TaffyAlignContent.SpaceEvenly,
            "space-around" => TaffyAlignContent.SpaceAround,
            _ => null
        };
    }

    private static TaffyTrackSizingFunction[] ParseTrackList(string s)
    {
        var result = new List<TaffyTrackSizingFunction>();
        foreach (var token in TokenizeTrackList(s))
            result.Add(ParseTrack(token));
        return result.ToArray();
    }

    // Splits a track list on spaces while respecting balanced parentheses.
    private static IEnumerable<string> TokenizeTrackList(string s)
    {
        var depth = 0;
        var start = 0;
        for (var i = 0; i < s.Length; i++)
            if (s[i] == '(')
            {
                depth++;
            }
            else if (s[i] == ')')
            {
                depth--;
            }
            else if (s[i] == ' ' && depth == 0)
            {
                if (i > start) yield return s[start..i];
                start = i + 1;
            }

        if (start < s.Length) yield return s[start..];
    }

    private static int FindTopLevelComma(string s)
    {
        var depth = 0;
        for (var i = 0; i < s.Length; i++)
            if (s[i] == '(') depth++;
            else if (s[i] == ')') depth--;
            else if (s[i] == ',' && depth == 0) return i;
        return -1;
    }

    private static TaffyTrackSizingFunction ParseTrack(string s)
    {
        if (s == "auto") return TrackSizingFunction.AutoTrack();

        if (s.StartsWith("minmax(", StringComparison.Ordinal) && s.EndsWith(')'))
        {
            var inner = s[7..^1];
            var commaIdx = FindTopLevelComma(inner);
            if (commaIdx >= 0)
            {
                var minDim = ParseMinTrackDimension(inner[..commaIdx].Trim());
                var maxDim = ParseMaxTrackDimension(inner[(commaIdx + 1)..].Trim());
                return TrackSizingFunction.MinMax(minDim, maxDim);
            }
        }

        if (s.EndsWith("fr", StringComparison.Ordinal))
            return TrackSizingFunction.Fr(float.Parse(s[..^2], CultureInfo.InvariantCulture));
        if (s.EndsWith("%", StringComparison.Ordinal))
            return TrackSizingFunction.Percent(float.Parse(s[..^1], CultureInfo.InvariantCulture) / 100f);
        if (s.EndsWith("px", StringComparison.Ordinal))
            return TrackSizingFunction.Px(float.Parse(s[..^2], CultureInfo.InvariantCulture));
        return TrackSizingFunction.Px(float.Parse(s, CultureInfo.InvariantCulture));
    }

    private static TaffyDimension ParseMinTrackDimension(string s)
    {
        return s switch
        {
            "auto" => Dimension.Auto(),
            "min-content" => Dimension.MinContent(),
            "max-content" => Dimension.MaxContent(),
            _ when s.EndsWith("%") => Dimension.Percent(float.Parse(s[..^1], CultureInfo.InvariantCulture) / 100f),
            _ when s.EndsWith("px") => Dimension.Px(float.Parse(s[..^2], CultureInfo.InvariantCulture)),
            _ => Dimension.Px(float.Parse(s, CultureInfo.InvariantCulture))
        };
    }

    private static TaffyDimension ParseMaxTrackDimension(string s)
    {
        return s switch
        {
            "auto" => Dimension.Auto(),
            "min-content" => Dimension.MinContent(),
            "max-content" => Dimension.MaxContent(),
            _ when s.EndsWith("fr") => Dimension.Fr(float.Parse(s[..^2], CultureInfo.InvariantCulture)),
            _ when s.EndsWith("%") => Dimension.Percent(float.Parse(s[..^1], CultureInfo.InvariantCulture) / 100f),
            _ when s.EndsWith("px") => Dimension.Px(float.Parse(s[..^2], CultureInfo.InvariantCulture)),
            _ => Dimension.Px(float.Parse(s, CultureInfo.InvariantCulture))
        };
    }

    // Encodes a CSS grid-line value into the start field of a TaffyGridPlacement.
    private static void SetGridStart(ref TaffyGridPlacement p, string value)
    {
        if (value == "auto") { p.start = 0; p.span = 0; return; }
        if (value.StartsWith("span ", StringComparison.Ordinal))
        {
            p.start = 0;
            p.span = (ushort)int.Parse(value[5..], CultureInfo.InvariantCulture);
            return;
        }
        p.start = short.Parse(value, CultureInfo.InvariantCulture);
    }

    // Encodes a CSS grid-line value into the end field of a TaffyGridPlacement.
    private static void SetGridEnd(ref TaffyGridPlacement p, string value)
    {
        p.end = value == "auto" ? (short)0 : short.Parse(value, CultureInfo.InvariantCulture);
    }
}
