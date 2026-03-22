using System;
using UnityEngine;

namespace FlexLayout
{
    // ──────────────────────────────────────────────────────────────────────────
    // Enums
    // ──────────────────────────────────────────────────────────────────────────

    public enum FlexDirection
    {
        Row,           // main axis = horizontal, left → right
        RowReverse,    // main axis = horizontal, right → left
        Column,        // main axis = vertical,   top  → bottom
        ColumnReverse  // main axis = vertical,   bottom → top
    }

    public enum FlexWrap
    {
        NoWrap,       // single line, may overflow
        Wrap,         // wrap to new lines
        WrapReverse   // wrap to new lines, reversed
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FlexItem
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A child element in a flex layout.
    /// All sizing and flex properties live on <see cref="LayoutElement.Style"/>.
    /// </summary>
    public class FlexItem : LayoutElement
    {
        /// <summary>
        /// Optional callback invoked by <see cref="FlexSolver.Draw"/> once
        /// this item's <see cref="LayoutElement.ComputedRect"/> has been resolved.
        /// Null items act as invisible spacers.
        /// </summary>
        public Action<Rect>? OnDraw { get; set; }

        private LayoutContainer? _nestedContainer;

        /// <summary>
        /// If this item is itself a nested container, assign it here.
        /// The solver will recursively lay it out within the item's computed rect.
        /// </summary>
        public LayoutContainer? AsContainer
        {
            get => _nestedContainer;
            set => _nestedContainer = value;
        }

        public override LayoutContainer? NestedContainer => _nestedContainer;
    }
}
