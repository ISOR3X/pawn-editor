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
    
    public float? childFlexBasis;
    public float? childFlexGrow;

    protected Dictionary<string, Func<LayoutNode<TLeaf>>>? registry;
    private FlexLayoutNode<TLeaf>? _root;
    public bool wrap;

    private LayoutNode<TLeaf>? Create(string tag)
    {
        var rootRegistry = _root?.registry ?? this.registry;
        return rootRegistry?.TryGetValue(tag, out var factory) == true ? factory() : null;
    }

    public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        base.LoadDataFromXmlCustom(xmlRoot);
        if (xmlRoot.Attributes?["direction"]?.Value is { } dir)
            direction = (FlexDirection)Enum.Parse(typeof(FlexDirection), dir, true);
        if (xmlRoot.Attributes?["wrap"]?.Value is { } flexWrap)
            wrap = ParseHelper.FromString<bool>(flexWrap);
        
        if (xmlRoot.Attributes?["gap"]?.Value is { } flexGap)
            gap = ParseHelper.FromString<float>(flexGap);
        if (xmlRoot.Attributes?["gap-x"]?.Value is { } flexGapX)
            gapX = ParseHelper.FromString<float>(flexGapX);
        if (xmlRoot.Attributes?["gap-y"]?.Value is { } flexGapY)
            gapY = ParseHelper.FromString<float>(flexGapY);
        
        if (xmlRoot.Attributes?["childFlexBasis"]?.Value is { } childBasis)
            childFlexBasis = ParseHelper.FromString<float>(childBasis);
        if (xmlRoot.Attributes?["childFlexGrow"]?.Value is { } childGrow)
            childFlexGrow = ParseHelper.FromString<float>(childGrow);

        foreach (XmlNode child in xmlRoot.ChildNodes)
        {
            if (child is XmlComment) continue;
            var node = Create(child.Name);
            
            // Check if the layout element exists.
            if (node == null)
            {
                var knownElements = (_root?.registry ?? registry)?.Keys.ToList() ?? [];
                Log.Error(
                    $"[{PawnEditorMod.ModName}] Unknown layout element <{child.Name}> in {xmlRoot.Name}. Known elements: {string.Join(", ", knownElements)}");
                continue;
            }
            if (node is FlexLayoutNode<TLeaf> flex)
                flex._root = _root ?? this;
            
            // Pass through child styles.
            if (childFlexBasis.HasValue && node.flexBasis == 0f)
                node.flexBasis = childFlexBasis.Value;
            if (childFlexGrow.HasValue && node.flexGrow == 0f)
                node.flexGrow = childFlexGrow.Value;
            
            node.LoadDataFromXmlCustom(child);
            children.Add(node);
        }
    }
}