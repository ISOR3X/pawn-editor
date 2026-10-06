using System.Text;

namespace Void.Native;

/// <summary>
///     Hand-written additions to the generated <c>NativeMethods.g.cs</c> (see <c>native/void-native/build.rs</c>).
///     Every native call returning <see cref="int" /> returns a <c>STATUS_*</c> code; <see cref="Check" /> turns
///     failures into exceptions carrying the native error message.
/// </summary>
public static unsafe partial class NativeMethods
{
    public static void Check(int status, string call)
    {
        if (status != STATUS_OK) throw new InvalidOperationException($"{call} failed ({status}): {LastError()}");
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
    ///     Verifies that the loaded library was built from the same source as these bindings. The declarations are
    ///     generated, so a mismatch means a stale <c>void_native</c> library rather than a wrong declaration, but it
    ///     would still silently corrupt every frame.
    /// </summary>
    /// <returns>null when everything matches, otherwise a description of the mismatch.</returns>
    public static string? VerifyAbi()
    {
        var version = void_abi_version();
        if (version != ABI_VERSION) return $"ABI version {version}, expected {ABI_VERSION}";

        AbiSizes native;
        Check(void_abi_sizes(&native), nameof(void_abi_sizes));

        var mismatches = new List<string>();

        void Compare(string name, uint nativeSize, int managedSize)
        {
            if (nativeSize != managedSize) mismatches.Add($"{name}: native {nativeSize}, managed {managedSize}");
        }

        Compare(nameof(NodeRecord), native.node_record, sizeof(NodeRecord));
        Compare(nameof(NodeOut), native.node_out, sizeof(NodeOut));
        Compare(nameof(FrameResult), native.frame_result, sizeof(FrameResult));
        Compare(nameof(StyleDesc), native.style_desc, sizeof(StyleDesc));
        Compare(nameof(MeasureSize), native.measure_size, sizeof(MeasureSize));
        Compare(nameof(TaffyDimension), native.dimension, sizeof(TaffyDimension));
        Compare(nameof(TaffyTrackSizingFunction), native.track, sizeof(TaffyTrackSizingFunction));
        Compare(nameof(TaffyGridPlacement), native.grid_placement, sizeof(TaffyGridPlacement));

        return mismatches.Count == 0 ? null : string.Join("; ", mismatches);
    }
}
