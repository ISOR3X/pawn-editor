// XML-driven layout node backed by TaffySharp.
//
// Defines a tree of layout nodes parsed from XML, using CSS-style attribute names
// that mirror Taffy's own XML test format. Container nodes use <div>, leaf nodes
// that reference SectionDefs use <section>. The root <layout> element is always
// an implicit flex-column container.
//
// Example XML:
//   <layout>
//       <div display="flex" flex-direction="row">
//           <section flex-basis="60%">PawnEditor_Info</section>
//           <section flex-grow="1">PawnEditor_Portrait</section>
//       </div>
//       <section>PawnEditor_Skills</section>
//   </layout>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using HotSwap;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TaffyLayoutNode
{
    private Style _style = new();

    // Public so DirectXmlCrossRefLoader can resolve it by field name after all defs load.
    public SectionDef? section;
    private readonly List<TaffyLayoutNode> _children = [];
    private Func<bool> _isActive = static () => true;


    // ── XML loading ────────────────────────────────────────────────────────────

    public void LoadDataFromXmlCustom(XmlNode xmlNode)
    {
        if (xmlNode.Attributes?["mayRequire"]?.Value is { } req
            && !ModLister.AllModsActiveNoSuffix(req.Split(',')))
        {
            _isActive = static () => false;
            return;
        }

        // Root <layout> defaults to a flex column (matching the old FlexLayoutEngine default).
        if (xmlNode.Name == "layout")
        {
            _style.gap = Taffy.Gap(GenUI.GapSmall, GenUI.GapSmall);
            _style.flexWrap = FlexWrap.Wrap;
        }

        ParseStyleAttributes(xmlNode);

        if (xmlNode.Name == "section")
        {
            var defName = xmlNode.InnerText?.Trim();
            if (!defName.NullOrEmpty())
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "section", defName);
        }
        else
        {
            foreach (XmlNode child in xmlNode.ChildNodes)
            {
                if (child is XmlComment) continue;
                if (child is XmlText txt && txt.Value?.Trim().Length == 0) continue;
                if (child.Name != "div" && child.Name != "section")
                {
                    Log.Error(
                        $"[{PawnEditorMod.ModName}] Unknown layout element <{child.Name}>. Expected <div> or <section>.");
                    continue;
                }

                var childNode = new TaffyLayoutNode();
                childNode.LoadDataFromXmlCustom(child);
                _children.Add(childNode);
            }
        }
    }

    private void ParseStyleAttributes(XmlNode xmlNode)
    {
        if (xmlNode.Attributes == null) return;

        foreach (XmlAttribute attr in xmlNode.Attributes)
        {
            switch (attr.Name)
            {
                case "display":
                    _style.display = ParseDisplay(attr.Value);
                    break;
                case "flex-direction":
                    _style.flexDirection = ParseFlexDirection(attr.Value);
                    break;
                case "flex-wrap":
                    _style.flexWrap = ParseFlexWrap(attr.Value);
                    break;
                case "flex-grow":
                    _style.flexGrow = ParseFloat(attr.Value);
                    break;
                case "flex-shrink":
                    _style.flexShrink = ParseFloat(attr.Value);
                    break;
                case "flex-basis":
                    _style.flexBasis = ParseDimension(attr.Value);
                    break;
                case "width":
                    _style.size = _style.size.MapWidth(_ => ParseDimension(attr.Value));
                    break;
                case "height":
                    _style.size = _style.size.MapHeight(_ => ParseDimension(attr.Value));
                    break;
                case "min-width":
                    _style.minSize = _style.minSize.MapWidth(_ => ParseDimension(attr.Value));
                    break;
                case "min-height":
                    _style.minSize = _style.minSize.MapHeight(_ => ParseDimension(attr.Value));
                    break;
                case "max-width":
                    _style.maxSize = _style.maxSize.MapWidth(_ => ParseMaxDimension(attr.Value));
                    break;
                case "max-height":
                    _style.maxSize = _style.maxSize.MapHeight(_ => ParseMaxDimension(attr.Value));
                    break;
                case "gap":
                {
                    var v = LengthPercentage.Length(ParsePx(attr.Value));
                    _style.gap = new Size<LengthPercentage>(v, v);
                    break;
                }
                case "column-gap":
                    _style.gap.Width = LengthPercentage.Length(ParsePx(attr.Value));
                    break;
                case "row-gap":
                    _style.gap.Height = LengthPercentage.Length(ParsePx(attr.Value));
                    break;
                case "padding":
                {
                    var v = LengthPercentage.Length(ParsePx(attr.Value));
                    _style.padding = new Rect<LengthPercentage>(v, v, v, v);
                    break;
                }
                case "padding-top":
                    _style.padding.Top = LengthPercentage.Length(ParsePx(attr.Value));
                    break;
                case "padding-right":
                    _style.padding.Right = LengthPercentage.Length(ParsePx(attr.Value));
                    break;
                case "padding-bottom":
                    _style.padding.Bottom = LengthPercentage.Length(ParsePx(attr.Value));
                    break;
                case "padding-left":
                    _style.padding.Left = LengthPercentage.Length(ParsePx(attr.Value));
                    break;
                case "margin":
                {
                    var v = LengthPercentageAuto.Length(ParsePx(attr.Value));
                    _style.margin = new Rect<LengthPercentageAuto>(v, v, v, v);
                    break;
                }
                case "margin-top":
                    _style.margin.Top = ParseLPA(attr.Value);
                    break;
                case "margin-right":
                    _style.margin.Right = ParseLPA(attr.Value);
                    break;
                case "margin-bottom":
                    _style.margin.Bottom = ParseLPA(attr.Value);
                    break;
                case "margin-left":
                    _style.margin.Left = ParseLPA(attr.Value);
                    break;
                case "align-items":
                    _style.alignItems = ParseAlignItems(attr.Value);
                    break;
                case "align-self":
                    _style.alignSelf = ParseAlignItems(attr.Value);
                    break;
                case "align-content":
                    _style.alignContent = ParseAlignContent(attr.Value);
                    break;
                case "justify-content":
                    _style.justifyContent = ParseAlignContent(attr.Value);
                    break;
                case "justify-items":
                    _style.justifyItems = ParseAlignItems(attr.Value);
                    break;
                case "justify-self":
                    _style.justifySelf = ParseAlignItems(attr.Value);
                    break;
                case "grid-template-columns":
                    _style.gridTemplateColumns = ParseTrackList(attr.Value);
                    break;
                case "grid-template-rows":
                    _style.gridTemplateRows = ParseTrackList(attr.Value);
                    break;
                case "grid-column-start":
                    _style.gridColumn.Start = ParseGridPlacement(attr.Value);
                    break;
                case "grid-column-end":
                    _style.gridColumn.End = ParseGridPlacement(attr.Value);
                    break;
                case "grid-row-start":
                    _style.gridRow.Start = ParseGridPlacement(attr.Value);
                    break;
                case "grid-row-end":
                    _style.gridRow.End = ParseGridPlacement(attr.Value);
                    break;
                // mayRequire is handled before this method is called
            }
        }
    }

    // ── Parser helpers ─────────────────────────────────────────────────────────

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

    private static TaffySharp.Display ParseDisplay(string s) => s switch
    {
        "flex" => TaffySharp.Display.Flex,
        "grid" => TaffySharp.Display.Grid,
        "block" => TaffySharp.Display.Block,
        "none" => TaffySharp.Display.None,
        _ => TaffySharp.Display.Flex,
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

    // ── Draw / BuildInto ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds this layout tree's nodes into an existing <paramref name="col"/> builder.
    /// Section XML nodes become flex-column containers whose items are populated by
    /// <see cref="SectionWorker.DoSectionContents"/>.
    /// </summary>
    public void BuildInto(TaffyBuilder col, Pawn pawn, Func<SectionDef, bool>? isVisible = null)
        => BuildNode(col, this, pawn, isVisible);

    private static void BuildNode(TaffyBuilder col, TaffyLayoutNode node, Pawn pawn,
        Func<SectionDef, bool>? isVisible)
    {
        if (node.section != null)
        {
            // Section node → flex-column container populated by the section worker.
            var sectionStyle = node._style;
            col.Container(sectionStyle, inner => node.section.Worker.BuildSection(inner, pawn));
            return;
        }

        col.Container(node._style, inner =>
        {
            foreach (var child in node._children)
            {
                if (!child._isActive()) continue;
                if (!child.HasVisibleContent(isVisible)) continue;
                BuildNode(inner, child, pawn, isVisible);
            }
        });
    }

    private bool HasVisibleContent(Func<SectionDef, bool>? isVisible)
    {
        if (section != null)
            return isVisible == null || isVisible(section);
        foreach (var child in _children)
            if (child._isActive() && child.HasVisibleContent(isVisible))
                return true;
        return false;
    }

    /// <summary>
    /// Standalone entry point: computes layout with unconstrained height, draws all visible
    /// sections inside <paramref name="rect"/>, and returns the total content height.
    /// </summary>
    public float Draw(Rect rect, Pawn pawn, Func<SectionDef, bool>? isVisible = null)
        => Taffy.MeasuredColumn(rect, col => BuildInto(col, pawn, isVisible));
}