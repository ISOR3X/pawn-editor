using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace PawnEditor.Layout;

public class FlexLayoutNode<TLeaf> : LayoutNode<TLeaf>
{
    public FlexDirection direction = FlexDirection.Col;
    public bool wrap;
    public float gap = 4f;
    public List<LayoutNode<TLeaf>> children = [];
    
    public Dictionary<string, Func<LayoutNode<TLeaf>>>? Registry;
    private FlexLayoutNode<TLeaf>? Root;
    
    private LayoutNode<TLeaf>? Create(string tag)
    {
        var registry = Root?.Registry ?? Registry;
        return registry?.TryGetValue(tag, out var factory) == true ? factory() : null;
    }
    
    public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        base.LoadDataFromXmlCustom(xmlRoot);
        if (xmlRoot.Attributes?["direction"]?.Value is { } dir)
            direction = (FlexDirection)Enum.Parse(typeof(FlexDirection), dir, ignoreCase: true);
        if (xmlRoot.Attributes?["gap"]?.Value is { } flexGap)
            gap = ParseHelper.FromString<float>(flexGap);
        if (xmlRoot.Attributes?["wrap"]?.Value is { } flexWrap)
            wrap = ParseHelper.FromString<bool>(flexWrap);
        
        foreach (XmlNode child in xmlRoot.ChildNodes)
        {
            if (child is XmlComment) continue;
            var node = Create(child.Name);
            if (node == null) continue;
            if (node is FlexLayoutNode<TLeaf> flex)
                flex.Root = Root ?? this;
            node.LoadDataFromXmlCustom(child);
            children.Add(node);
        }
    }
}