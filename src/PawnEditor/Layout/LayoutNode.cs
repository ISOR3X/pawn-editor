using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace PawnEditor.Layout;

public enum FlexDirection
{
    Col,
    Row
}

public class LayoutNode<TLeaf>
{
    public FlexDirection direction = FlexDirection.Col;
    public float flexBasis = 1f;
    public float flexGrow = 1f;
    public bool wrap;
    public float gap = 4f;
    public TLeaf? leaf;
    public List<LayoutNode<TLeaf>> children = [];
    public bool IsLeaf => leaf != null;
    public virtual bool IsActive => true;

    public virtual void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        if (xmlRoot.Attributes?["direction"]?.Value is { } dir)
            direction = (FlexDirection)Enum.Parse(typeof(FlexDirection), dir, ignoreCase: true);
        if (xmlRoot.Attributes?["flexBasis"]?.Value is { } basis)
            flexBasis = ParseHelper.FromString<float>(basis);
        if (xmlRoot.Attributes?["flexGrow"]?.Value is { } grow)
            flexGrow = ParseHelper.FromString<float>(grow);
        if (xmlRoot.Attributes?["gap"]?.Value is { } flexGap)
            gap = ParseHelper.FromString<float>(flexGap);
        if (xmlRoot.Attributes?["wrap"]?.Value is { } flexWrap)
            wrap = ParseHelper.FromString<bool>(flexWrap);
    }

    public void InvalidateCache()
    {
        _cachedMeasure = null;
        foreach (var child in children)
            child.InvalidateCache();
    }

    [Unsaved] internal float? _cachedMeasure;
}