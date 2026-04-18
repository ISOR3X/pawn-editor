// Port of taffy/src/style/dimension.rs
//
// Three thin-wrapper types around CompactLength, each allowing a different
// subset of variants:
//
//   LengthPercentage     — Length | Percent
//   LengthPercentageAuto — Length | Percent | Auto
//   Dimension            — Length | Percent | Auto  (same variants; wider usage)

namespace Taffy;
// ── LengthPercentage ─────────────────────────────────────────────────────

/// <summary>
///     A CSS length that is either a fixed value or a percentage.
///     Valid variants: <c>Length</c>, <c>Percent</c>.
/// </summary>
public readonly struct LengthPercentage : IEquatable<LengthPercentage>
{
    internal readonly CompactLength Inner;

    private LengthPercentage(CompactLength inner)
    {
        Inner = inner;
    }

    // ── Factories ─────────────────────────────────────────────────────────

    public static LengthPercentage Length(float val)
    {
        return new LengthPercentage(CompactLength.Length(val));
    }

    public static LengthPercentage Percent(float val)
    {
        return new LengthPercentage(CompactLength.Percent(val));
    }

    // ── Constants ─────────────────────────────────────────────────────────

    public static readonly LengthPercentage ZERO = Length(0f);

    // ── Accessors ─────────────────────────────────────────────────────────

    public byte Tag => Inner.Tag;
    public float Value => Inner.Value;

    // ── Equality ──────────────────────────────────────────────────────────

    public bool Equals(LengthPercentage other)
    {
        return Inner == other.Inner;
    }

    public override bool Equals(object? obj)
    {
        return obj is LengthPercentage other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Inner.GetHashCode();
    }

    public static bool operator ==(LengthPercentage a, LengthPercentage b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(LengthPercentage a, LengthPercentage b)
    {
        return !a.Equals(b);
    }

    public override string ToString()
    {
        return Inner.ToString();
    }

    public static implicit operator LengthPercentage(float val)
    {
        return Length(val);
    }
}

// ── LengthPercentageAuto ─────────────────────────────────────────────────

/// <summary>
///     A CSS length that can be a fixed value, a percentage, or <c>auto</c>.
///     Valid variants: <c>Length</c>, <c>Percent</c>, <c>Auto</c>.
/// </summary>
public readonly struct LengthPercentageAuto : IEquatable<LengthPercentageAuto>
{
    internal readonly CompactLength Inner;

    private LengthPercentageAuto(CompactLength inner)
    {
        Inner = inner;
    }

    // ── Factories ─────────────────────────────────────────────────────────

    public static LengthPercentageAuto Length(float val)
    {
        return new LengthPercentageAuto(CompactLength.Length(val));
    }

    public static LengthPercentageAuto Percent(float val)
    {
        return new LengthPercentageAuto(CompactLength.Percent(val));
    }

    public static LengthPercentageAuto Auto()
    {
        return new LengthPercentageAuto(CompactLength.Auto());
    }

    // ── Constants ─────────────────────────────────────────────────────────

    public static readonly LengthPercentageAuto ZERO = Length(0f);
    public static readonly LengthPercentageAuto AUTO = Auto();

    // ── Conversions ───────────────────────────────────────────────────────

    public static implicit operator LengthPercentageAuto(LengthPercentage lp)
    {
        return new LengthPercentageAuto(lp.Inner);
    }

    public static implicit operator LengthPercentageAuto(float val)
    {
        return new LengthPercentageAuto(CompactLength.Length(val));
    }

    // ── Accessors ─────────────────────────────────────────────────────────

    public byte Tag => Inner.Tag;
    public float Value => Inner.Value;

    public bool IsAuto()
    {
        return Inner.IsAuto();
    }

    /// <summary>
    ///     Resolves to <c>Some(length)</c> for Length, <c>Some(context * pct)</c> for Percent,
    ///     or <c>null</c> for Auto.
    /// </summary>
    public float? ResolveToOption(float context)
    {
        if (Inner.Tag == CompactLength.LENGTH_TAG) return Inner.Value;
        if (Inner.Tag == CompactLength.PERCENT_TAG) return context * Inner.Value;
        return null; // Auto
    }

    // ── Equality ──────────────────────────────────────────────────────────

    public bool Equals(LengthPercentageAuto other)
    {
        return Inner == other.Inner;
    }

    public override bool Equals(object? obj)
    {
        return obj is LengthPercentageAuto other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Inner.GetHashCode();
    }

    public static bool operator ==(LengthPercentageAuto a, LengthPercentageAuto b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(LengthPercentageAuto a, LengthPercentageAuto b)
    {
        return !a.Equals(b);
    }

    public override string ToString()
    {
        return Inner.ToString();
    }
}

// ── Dimension ─────────────────────────────────────────────────────────────

/// <summary>
///     A CSS size dimension: Length, Percent, or Auto.
///     Used for width, height, min/max sizing, flex-basis, etc.
/// </summary>
public readonly struct Dimension : IEquatable<Dimension>
{
    internal readonly CompactLength Inner;

    private Dimension(CompactLength inner)
    {
        Inner = inner;
    }

    // ── Factories ─────────────────────────────────────────────────────────

    public static Dimension Length(float val)
    {
        return new Dimension(CompactLength.Length(val));
    }

    public static Dimension Percent(float val)
    {
        return new Dimension(CompactLength.Percent(val));
    }

    public static Dimension Auto()
    {
        return new Dimension(CompactLength.Auto());
    }

    // ── Constants ─────────────────────────────────────────────────────────

    public static readonly Dimension ZERO = Length(0f);
    public static readonly Dimension AUTO = Auto();

    // ── Conversions ───────────────────────────────────────────────────────

    public static implicit operator Dimension(LengthPercentage lp)
    {
        return new Dimension(lp.Inner);
    }

    public static implicit operator Dimension(LengthPercentageAuto l)
    {
        return new Dimension(l.Inner);
    }

    public static implicit operator Dimension(float val)
    {
        return Length(val);
    }

    // ── Accessors ─────────────────────────────────────────────────────────

    public byte Tag => Inner.Tag;
    public float Value => Inner.Value;

    public bool IsAuto()
    {
        return Inner.IsAuto();
    }

    /// <summary>
    ///     Returns the length value if this is a <c>Length</c> variant, otherwise <c>null</c>.
    /// </summary>
    public float? IntoOption()
    {
        return Inner.Tag == CompactLength.LENGTH_TAG ? Inner.Value : null;
    }

    // ── Equality ──────────────────────────────────────────────────────────

    public bool Equals(Dimension other)
    {
        return Inner == other.Inner;
    }

    public override bool Equals(object? obj)
    {
        return obj is Dimension other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Inner.GetHashCode();
    }

    public static bool operator ==(Dimension a, Dimension b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Dimension a, Dimension b)
    {
        return !a.Equals(b);
    }

    public override string ToString()
    {
        return Inner.ToString();
    }
}