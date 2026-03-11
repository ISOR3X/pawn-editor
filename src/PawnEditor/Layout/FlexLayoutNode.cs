using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace PawnEditor.Layout;

public class FlexLayoutNode<TLeaf> : LayoutNode<TLeaf>
{
    public List<LayoutNode<TLeaf>> children = [];
    public FlexDirection direction = FlexDirection.Col;
    public float gap = 4f;
    public float? gapX;
    public float? gapY;

    public Dictionary<string, Func<LayoutNode<TLeaf>>>? Registry;
    private FlexLayoutNode<TLeaf>? Root;
    public bool wrap;

    private LayoutNode<TLeaf>? Create(string tag)
    {
        var registry = Root?.Registry ?? Registry;
        return registry?.TryGetValue(tag, out var factory) == true ? factory() : null;
    }

    public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        base.LoadDataFromXmlCustom(xmlRoot);
        if (xmlRoot.Attributes?["direction"]?.Value is { } dir)
            direction = (FlexDirection)Enum.Parse(typeof(FlexDirection), dir, true);
        if (xmlRoot.Attributes?["gap"]?.Value is { } flexGap)
            gap = ParseHelper.FromString<float>(flexGap);
        if (xmlRoot.Attributes?["gap-x"]?.Value is { } flexGapX)
            gapX = ParseHelper.FromString<float>(flexGapX);
        if (xmlRoot.Attributes?["gap-y"]?.Value is { } flexGapY)
            gapY = ParseHelper.FromString<float>(flexGapY);
        if (xmlRoot.Attributes?["wrap"]?.Value is { } flexWrap)
            wrap = ParseHelper.FromString<bool>(flexWrap);

        foreach (XmlNode child in xmlRoot.ChildNodes)
        {
            if (child is XmlComment) continue;
            var node = Create(child.Name);
            if (node == null)
            {
                var knownElements = (Root?.Registry ?? Registry)?.Keys.ToList() ?? [];
                Log.Error(
                    $"[{PawnEditorMod.ModName}] Unknown layout element <{child.Name}> in {xmlRoot.Name}. Known elements: {string.Join(", ", knownElements)}");
                continue;
            }

            if (node is FlexLayoutNode<TLeaf> flex)
                flex.Root = Root ?? this;
            node.LoadDataFromXmlCustom(child);
            children.Add(node);
        }
    }
}