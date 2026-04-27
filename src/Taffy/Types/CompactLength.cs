// Port of taffy/src/style/compact_length.rs
//
// Rust's CompactLength uses tagged-pointer tricks for a 64-bit representation.
// In C# we use a plain struct with an explicit tag enum + float value - same
// semantics, no unsafe code required.

namespace Taffy;

/// <summary>
///     A compact representation of a CSS length value.
///     Wraps a <see cref="Tag" /> discriminant and an optional <c>float</c> value.
///     All dimension types (<see cref="LengthPercentage" />, <see cref="LengthPercentageAuto" />,
///     <see cref="Dimension" />) are thin wrappers around this type.
/// </summary>
public readonly struct CompactLength : IEquatable<CompactLength>
{
    // Tag constants mirror the Rust source exactly (for cross-reference ease).
    public const byte LENGTH_TAG = 0b0000_0001; // 1
    public const byte PERCENT_TAG = 0b0000_0010; // 2
    public const byte AUTO_TAG = 0b0000_0011; // 3
    public const byte FR_TAG = 0b0000_0100; // 4
    public const byte MIN_CONTENT_TAG = 0b0000_0111; // 7
    public const byte MAX_CONTENT_TAG = 0b0000_1111; // 15
    public const byte FIT_CONTENT_PX_TAG = 0b0001_0111; // 23
    public const byte FIT_CONTENT_PCT_TAG = 0b0001_1111; // 31

    private CompactLength(byte tag, float value = 0f)
    {
        Tag = tag;
        Value = value;
    }

    // ── Factory methods ───────────────────────────────────────────────────

    /// <summary>An absolute length (pixels, logical pixels, etc.).</summary>
    public static CompactLength Length(float val)
    {
        return new CompactLength(LENGTH_TAG, val);
    }

    /// <summary>
    ///     A percentage of the containing block.
    ///     Values are in [0.0, 1.0], NOT [0.0, 100.0].
    /// </summary>
    public static CompactLength Percent(float val)
    {
        return new CompactLength(PERCENT_TAG, val);
    }

    /// <summary>Automatically computed size (CSS <c>auto</c>).</summary>
    public static CompactLength Auto()
    {
        return new CompactLength(AUTO_TAG);
    }

    /// <summary>A fraction of available grid space (<c>fr</c> unit).</summary>
    public static CompactLength Fr(float val)
    {
        return new CompactLength(FR_TAG, val);
    }

    /// <summary>The min-content intrinsic size.</summary>
    public static CompactLength MinContent()
    {
        return new CompactLength(MIN_CONTENT_TAG);
    }

    /// <summary>The max-content intrinsic size.</summary>
    public static CompactLength MaxContent()
    {
        return new CompactLength(MAX_CONTENT_TAG);
    }

    /// <summary>fit-content(<paramref name="limitPx" /> px).</summary>
    public static CompactLength FitContentPx(float limitPx)
    {
        return new CompactLength(FIT_CONTENT_PX_TAG, limitPx);
    }

    /// <summary>fit-content(<paramref name="limitPct" /> %).</summary>
    public static CompactLength FitContentPercent(float limitPct)
    {
        return new CompactLength(FIT_CONTENT_PCT_TAG, limitPct);
    }

    // ── Well-known constants ──────────────────────────────────────────────

    public static readonly CompactLength ZERO = Length(0f);
    public static readonly CompactLength AUTO = Auto();
    public static readonly CompactLength MIN_CONTENT = MinContent();
    public static readonly CompactLength MAX_CONTENT = MaxContent();

    // ── Accessors ─────────────────────────────────────────────────────────

    public byte Tag { get; }

    /// <summary>The numeric value for Length / Percent / Fr / FitContent variants.</summary>
    public float Value { get; }

    // ── Query helpers ─────────────────────────────────────────────────────

    public bool IsZero()
    {
        return Tag == LENGTH_TAG && Value == 0f;
    }

    public bool IsAuto()
    {
        return Tag == AUTO_TAG;
    }

    public bool IsLengthOrPercent()
    {
        return Tag == LENGTH_TAG || Tag == PERCENT_TAG;
    }

    public bool IsMinContent()
    {
        return Tag == MIN_CONTENT_TAG;
    }

    public bool IsMaxContent()
    {
        return Tag == MAX_CONTENT_TAG;
    }

    public bool IsFitContent()
    {
        return Tag == FIT_CONTENT_PX_TAG || Tag == FIT_CONTENT_PCT_TAG;
    }

    public bool IsMaxOrFitContent()
    {
        return Tag == MAX_CONTENT_TAG || Tag == FIT_CONTENT_PX_TAG || Tag == FIT_CONTENT_PCT_TAG;
    }

    public bool IsFr()
    {
        return Tag == FR_TAG;
    }

    public bool IsMinOrMaxContent()
    {
        return Tag == MIN_CONTENT_TAG || Tag == MAX_CONTENT_TAG;
    }

    /// <summary>
    ///     True for Auto, MaxContent, FitContentPx, FitContentPercent.
    ///     "In all cases, treat auto and fit-content() as max-content…" - CSS Grid spec.
    /// </summary>
    public bool IsMaxContentAlike()
    {
        return Tag == AUTO_TAG ||
               Tag == MAX_CONTENT_TAG ||
               Tag == FIT_CONTENT_PX_TAG ||
               Tag == FIT_CONTENT_PCT_TAG;
    }

    public bool IsIntrinsic()
    {
        return Tag == AUTO_TAG ||
               Tag == MIN_CONTENT_TAG ||
               Tag == MAX_CONTENT_TAG ||
               Tag == FIT_CONTENT_PX_TAG ||
               Tag == FIT_CONTENT_PCT_TAG;
    }

    public bool UsesPercentage()
    {
        return Tag == PERCENT_TAG || Tag == FIT_CONTENT_PCT_TAG;
    }

    /// <summary>
    ///     Resolves percentage values against <paramref name="parentSize" />.
    ///     Returns <c>null</c> for non-percentage variants.
    /// </summary>
    public float? ResolvedPercentageSize(float parentSize)
    {
        if (Tag == PERCENT_TAG) return Value * parentSize;
        return null;
    }

    // ── Equality ──────────────────────────────────────────────────────────

    public bool Equals(CompactLength other)
    {
        return Tag == other.Tag && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CompactLength other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Tag, Value);
    }

    public static bool operator ==(CompactLength a, CompactLength b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(CompactLength a, CompactLength b)
    {
        return !a.Equals(b);
    }

    public override string ToString()
    {
        return Tag switch
        {
            LENGTH_TAG => $"{Value}px",
            PERCENT_TAG => $"{Value * 100f}%",
            AUTO_TAG => "auto",
            FR_TAG => $"{Value}fr",
            MIN_CONTENT_TAG => "min-content",
            MAX_CONTENT_TAG => "max-content",
            FIT_CONTENT_PX_TAG => $"fit-content({Value}px)",
            FIT_CONTENT_PCT_TAG => $"fit-content({Value * 100f}%)",
            _ => $"unknown(tag={Tag})"
        };
    }
}