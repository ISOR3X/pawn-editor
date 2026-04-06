// Port of taffy/src/style/flex.rs (enums only; traits are in Style.cs)

namespace Taffy
{
    /// <summary>
    /// Controls the direction of the main flexbox axis.
    /// Defaults to <see cref="Row"/>.
    /// </summary>
    public enum FlexDirection : byte
    {
        /// <summary>Items flow left-to-right along a horizontal main axis.</summary>
        Row,

        /// <summary>Items flow top-to-bottom along a vertical main axis.</summary>
        Column,

        /// <summary>Items flow right-to-left along a horizontal main axis.</summary>
        RowReverse,

        /// <summary>Items flow bottom-to-top along a vertical main axis.</summary>
        ColumnReverse,
    }

    /// <summary>
    /// Controls whether flex items wrap onto multiple lines.
    /// Defaults to <see cref="NoWrap"/>.
    /// </summary>
    public enum FlexWrap : byte
    {
        /// <summary>Items will not wrap and stay on a single line.</summary>
        NoWrap,

        /// <summary>Items will wrap onto multiple lines in the forward direction.</summary>
        Wrap,

        /// <summary>Items will wrap onto multiple lines in the reverse direction.</summary>
        WrapReverse,
    }

    /// <summary>Extension helpers mirroring the Rust <c>FlexDirection</c> impl.</summary>
    public static class FlexDirectionExt
    {
        /// <summary>Returns true if the direction is <see cref="FlexDirection.Row"/> or <see cref="FlexDirection.RowReverse"/>.</summary>
        public static bool IsRow(this FlexDirection dir) =>
            dir == FlexDirection.Row || dir == FlexDirection.RowReverse;

        /// <summary>Returns true if the direction is <see cref="FlexDirection.Column"/> or <see cref="FlexDirection.ColumnReverse"/>.</summary>
        public static bool IsColumn(this FlexDirection dir) =>
            dir == FlexDirection.Column || dir == FlexDirection.ColumnReverse;

        /// <summary>Returns true if the direction is reversed (RowReverse or ColumnReverse).</summary>
        public static bool IsReverse(this FlexDirection dir) =>
            dir == FlexDirection.RowReverse || dir == FlexDirection.ColumnReverse;

        /// <summary>Returns the <see cref="AbsoluteAxis"/> of the main axis.</summary>
        public static AbsoluteAxis MainAxis(this FlexDirection dir) =>
            dir.IsRow() ? AbsoluteAxis.Horizontal : AbsoluteAxis.Vertical;

        /// <summary>Returns the <see cref="AbsoluteAxis"/> of the cross axis.</summary>
        public static AbsoluteAxis CrossAxis(this FlexDirection dir) =>
            dir.IsRow() ? AbsoluteAxis.Vertical : AbsoluteAxis.Horizontal;
    }
}