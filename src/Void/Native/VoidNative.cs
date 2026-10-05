using System.Runtime.InteropServices;
using System.Text;
using Taffy;

namespace Void.Native;

/// <summary>
///     One node of a frame's description, in pre-order. Mirrors <c>NodeRecord</c> in <c>native/void-native/src/tree.rs</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NodeRecord
{
    /// <summary>Stable identity across frames: a hash of the path from the root.</summary>
    public ulong key;

    /// <summary>Index one past this node's last descendant.</summary>
    public uint subtreeEnd;

    /// <summary>Handle from <see cref="StyleRegistry" />, 0 = default style.</summary>
    public uint style;

    /// <summary>Handle from <see cref="MeasureRegistry" />, 0 = not measured.</summary>
    public uint measure;

    private uint _pad;
}

/// <summary>
///     Layout of one record, written by the native side. Mirrors <c>NodeOut</c>.
///     Positions are absolute, except children of a scroll container, which are relative to its content.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NodeOut
{
    public const uint ScrollX = 1 << 0;
    public const uint ScrollY = 1 << 1;

    public float x;
    public float y;
    public float width;
    public float height;
    public float contentWidth;
    public float contentHeight;
    public float scrollbarWidth;
    public float scrollbarHeight;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct FrameResult
{
    public uint removedCount;
    public uint laidOut;
    public float rootWidth;
    public float rootHeight;
}

/// <summary>
///     Width-independent measurements of a piece of content, computed once at registration. Mirrors <c>MeasureDesc</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MeasureDesc
{
    public float maxWidth;
    public float maxHeight;
    public float minContentWidth;
    public uint wraps;
}

/// <summary>
///     Flat description of a layout style. Mirrors <c>StyleDesc</c> in <c>native/void-native/src/style.rs</c>;
///     the Taffy value structs are laid out identically on both sides.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct StyleDesc
{
    public ulong setMask;

    public TaffyTrackSizingFunction* gridTemplateColumns;
    public TaffyTrackSizingFunction* gridTemplateRows;
    public TaffyTrackSizingFunction* gridAutoColumns;
    public TaffyTrackSizingFunction* gridAutoRows;
    public uint gridTemplateColumnsLen;
    public uint gridTemplateRowsLen;
    public uint gridAutoColumnsLen;
    public uint gridAutoRowsLen;

    public uint display;
    public uint flexDirection;
    public uint flexWrap;
    public uint alignItems;
    public uint alignSelf;
    public uint alignContent;
    public uint justifyContent;
    public uint justifyItems;
    public uint justifySelf;
    public uint overflowX;
    public uint overflowY;
    public uint gridAutoFlow;

    public float flexGrow;
    public float flexShrink;
    public float scrollbarWidth;

    public TaffyDimension flexBasis;
    public TaffyDimension width;
    public TaffyDimension height;
    public TaffyDimension minWidth;
    public TaffyDimension minHeight;
    public TaffyDimension maxWidth;
    public TaffyDimension maxHeight;

    public TaffyAxes gap;
    public TaffyEdges padding;
    public TaffyEdges margin;

    public TaffyGridPlacement gridColumn;
    public TaffyGridPlacement gridRow;
}

/// <summary>Bits of <see cref="StyleDesc.setMask" />. Mirrors <c>style::set</c>.</summary>
internal static class StyleSet
{
    public const ulong Display = 1UL << 0;
    public const ulong FlexDirection = 1UL << 1;
    public const ulong FlexWrap = 1UL << 2;
    public const ulong AlignItems = 1UL << 3;
    public const ulong AlignSelf = 1UL << 4;
    public const ulong AlignContent = 1UL << 5;
    public const ulong JustifyContent = 1UL << 6;
    public const ulong JustifyItems = 1UL << 7;
    public const ulong JustifySelf = 1UL << 8;
    public const ulong OverflowX = 1UL << 9;
    public const ulong OverflowY = 1UL << 10;
    public const ulong GridAutoFlow = 1UL << 11;
    public const ulong FlexGrow = 1UL << 12;
    public const ulong FlexShrink = 1UL << 13;
    public const ulong ScrollbarWidth = 1UL << 14;
    public const ulong FlexBasis = 1UL << 15;
    public const ulong Width = 1UL << 16;
    public const ulong Height = 1UL << 17;
    public const ulong MinWidth = 1UL << 18;
    public const ulong MinHeight = 1UL << 19;
    public const ulong MaxWidth = 1UL << 20;
    public const ulong MaxHeight = 1UL << 21;
    public const ulong Gap = 1UL << 22;
    public const ulong Padding = 1UL << 23;
    public const ulong Margin = 1UL << 24;
    public const ulong GridColumn = 1UL << 25;
    public const ulong GridRow = 1UL << 26;
    public const ulong GridTemplateColumns = 1UL << 27;
    public const ulong GridTemplateRows = 1UL << 28;
    public const ulong GridAutoColumns = 1UL << 29;
    public const ulong GridAutoRows = 1UL << 30;
}

[StructLayout(LayoutKind.Sequential)]
internal struct AbiSizes
{
    public uint nodeRecord;
    public uint nodeOut;
    public uint frameResult;
    public uint styleDesc;
    public uint measureDesc;
    public uint dimension;
    public uint track;
    public uint gridPlacement;
}

/// <summary>
///     Raw bindings for <c>void_native</c>. Every call returning <see cref="int" /> returns a status code;
///     use <see cref="Check" /> to turn failures into exceptions carrying the native error message.
/// </summary>
internal static unsafe class VoidNative
{
    private const string Lib = "void_native";

    /// <summary>Must match <c>ABI_VERSION</c> in <c>lib.rs</c>.</summary>
    public const uint AbiVersion = 1;

    public const int StatusOk = 0;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float HeightCallback(uint handle, float width);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint void_abi_version();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int void_abi_sizes(AbiSizes* sizes);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint void_last_error(byte* buffer, uint capacity);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int void_set_height_callback(HeightCallback? callback);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int void_style_register(StyleDesc* desc, uint* handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int void_measure_register(MeasureDesc* desc, uint* handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int void_measure_release(uint handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong void_height_callback_count();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr void_tree_new();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void void_tree_free(IntPtr tree);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int void_frame(IntPtr tree, NodeRecord* records, uint count, float availableWidth,
        float availableHeight, float originX, float originY, NodeOut* output, FrameResult* result);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint void_take_removed(IntPtr tree, ulong* buffer, uint capacity);

    public static void Check(int status, string call)
    {
        if (status != StatusOk) throw new InvalidOperationException($"{call} failed ({status}): {LastError()}");
    }

    public static string LastError()
    {
        var length = void_last_error(null, 0);
        if (length == 0) return "";
        var buffer = new byte[length];
        fixed (byte* p = buffer) void_last_error(p, length);
        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>
    ///     Verifies that the loaded library speaks the same ABI as these declarations. A mismatched struct
    ///     would not fail loudly, it would silently corrupt every frame, so this runs before anything else.
    /// </summary>
    /// <returns>null when everything matches, otherwise a description of the mismatch.</returns>
    public static string? VerifyAbi()
    {
        var version = void_abi_version();
        if (version != AbiVersion) return $"ABI version {version}, expected {AbiVersion}";

        AbiSizes native;
        Check(void_abi_sizes(&native), nameof(void_abi_sizes));

        var mismatches = new List<string>();
        void Compare(string name, uint nativeSize, int managedSize)
        {
            if (nativeSize != managedSize) mismatches.Add($"{name}: native {nativeSize}, managed {managedSize}");
        }

        Compare(nameof(NodeRecord), native.nodeRecord, sizeof(NodeRecord));
        Compare(nameof(NodeOut), native.nodeOut, sizeof(NodeOut));
        Compare(nameof(FrameResult), native.frameResult, sizeof(FrameResult));
        Compare(nameof(StyleDesc), native.styleDesc, sizeof(StyleDesc));
        Compare(nameof(MeasureDesc), native.measureDesc, sizeof(MeasureDesc));
        Compare(nameof(TaffyDimension), native.dimension, sizeof(TaffyDimension));
        Compare(nameof(TaffyTrackSizingFunction), native.track, sizeof(TaffyTrackSizingFunction));
        Compare(nameof(TaffyGridPlacement), native.gridPlacement, sizeof(TaffyGridPlacement));

        return mismatches.Count == 0 ? null : string.Join("; ", mismatches);
    }
}
