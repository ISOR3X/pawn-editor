using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace PawnEditor.Layout;

public class FlexLayoutNode<TLeaf> : GroupLayoutNode<TLeaf>
{
    public FlexDirection direction = FlexDirection.Col;
    
    public float? childFlexBasis;
    public float? childFlexGrow;

    protected Dictionary<string, Func<LayoutNode<TLeaf>>>? registry;
    private FlexLayoutNode<TLeaf>? _root;
    public bool wrap;

    private LayoutNode<TLeaf>? Create(string tag)
    {
        var rootRegistry = _root?.registry ?? registry;
        return rootRegistry?.TryGetValue(tag, out var factory) == true ? factory() : null;
    }

    public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        base.LoadDataFromXmlCustom(xmlRoot);
        if (xmlRoot.Attributes?["direction"]?.Value is { } dir)
            direction = (FlexDirection)Enum.Parse(typeof(FlexDirection), dir, true);
        if (xmlRoot.Attributes?["wrap"]?.Value is { } flexWrap)
            wrap = ParseHelper.FromString<bool>(flexWrap);

        if (xmlRoot.Attributes?["childFlexBasis"]?.Value is { } childBasis)
            childFlexBasis = ParseHelper.FromString<float>(childBasis);
        if (xmlRoot.Attributes?["childFlexGrow"]?.Value is { } childGrow)
            childFlexGrow = ParseHelper.FromString<float>(childGrow);

        foreach (XmlNode child in xmlRoot.ChildNodes)
        {
            if (child is XmlComment) continue;
            var node = Create(child.Name);

            if (node == null)
            {
                var knownElements = (_root?.registry ?? registry)?.Keys.ToList() ?? [];
                Log.Error(
                    $"[{PawnEditorMod.ModName}] Unknown layout element <{child.Name}> in {xmlRoot.Name}. Known elements: {string.Join(", ", knownElements)}");
                continue;
            }

            if (node is FlexLayoutNode<TLeaf> flex)
                flex._root = _root ?? this;

            node.LoadDataFromXmlCustom(child);
            children.Add(node);
        }

        ApplyChildDefaults();
    }

    public void ApplyChildDefaults()
    {
        foreach (var child in children)
        {
            if (childFlexBasis.HasValue && child.flexBasis == 0f)
                child.flexBasis = childFlexBasis.Value;
            if (childFlexGrow.HasValue && child.flexGrow == 0f)
                child.flexGrow = childFlexGrow.Value;
        }
    }
    
    public float Draw(Rect rect, Func<TLeaf, Rect, float> runLeaf, Func<TLeaf, bool>? isVisible = null)
        => FlexLayoutEngine.Draw(this, rect, runLeaf, isVisible);
}