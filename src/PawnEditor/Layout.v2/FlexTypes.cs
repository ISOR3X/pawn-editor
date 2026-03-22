using System;
using UnityEngine;

namespace FlexLayout;
// ──────────────────────────────────────────────────────────────────────────
// FlexItem
// ──────────────────────────────────────────────────────────────────────────

/// <summary>
///     A child element in a flex layout.
///     All sizing and flex properties live on <see cref="LayoutElement.Style" />.
/// </summary>
public class FlexItem : LayoutElement
{
    /// <summary>
    ///     Optional callback invoked by <see cref="FlexSolver.Draw" /> once
    ///     this item's <see cref="LayoutElement.ComputedRect" /> has been resolved.
    ///     Null items act as invisible spacers.
    /// </summary>
    public Action<Rect>? OnDraw { get; set; }

    /// <summary>
    ///     If this item is itself a nested container, assign it here.
    ///     The solver will recursively lay it out within the item's computed rect.
    /// </summary>
    public LayoutContainer? AsContainer { get; set; }

    public override LayoutContainer? NestedContainer => AsContainer;
}