using System.Collections.Generic;
using UnityEngine;

namespace FlexLayout;

/// <summary>
///     Base class for anything that participates in layout as a leaf node or container.
///     All sizing and layout properties live on <see cref="Style" />.
/// </summary>
public abstract class LayoutElement
{
    public ElementStyle Style { get; set; }

    /// <summary>
    ///     The resolved screen-space rect, populated by the layout solver after a layout pass.
    /// </summary>
    public Rect ComputedRect { get; internal set; }

    /// <summary>
    ///     If this element is itself a layout container, the solver will recursively
    ///     compute its children using this reference. Null for leaf elements.
    /// </summary>
    public virtual LayoutContainer? NestedContainer => null;
}

/// <summary>
///     Base class for a container that holds LayoutElements.
///     Container-specific layout properties (direction, wrap, gap) live on <see cref="LayoutElement.Style" />.
/// </summary>
public class LayoutContainer
{
    private readonly List<LayoutElement> _children = [];

    public IReadOnlyList<LayoutElement> Children => _children;

    public ElementStyle Style { get; set; }

    public Rect ComputedRect { get; internal set; }


    public void Add(LayoutElement child)
    {
        _children.Add(child);
    }

    public void Remove(LayoutElement child)
    {
        _children.Remove(child);
    }

    public void Clear()
    {
        _children.Clear();
    }
}