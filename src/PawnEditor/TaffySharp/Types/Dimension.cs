// Port of taffy/src/style/dimension.rs
//
// Three thin-wrapper types around CompactLength, each allowing a different
// subset of variants:
//
//   LengthPercentage     — Length | Percent
//   LengthPercentageAuto — Length | Percent | Auto
//   Dimension            — Length | Percent | Auto  (same variants; wider usage)

namespace PawnEditor.TaffySharp
{
    // ── LengthPercentage ─────────────────────────────────────────────────────

    /// <summary>
    /// A CSS length that is either a fixed value or a percentage.
    /// Valid variants: <c>Length</c>, <c>Percent</c>.
    /// </summary>
    public readonly struct LengthPercentage : System.IEquatable<LengthPercentage>
    {
        internal readonly CompactLength Inner;

        private LengthPercentage(CompactLength inner) => Inner = inner;

        // ── Factories ─────────────────────────────────────────────────────────

        public static LengthPercentage Length(float val) => new LengthPercentage(CompactLength.Length(val));
        public static LengthPercentage Percent(float val) => new LengthPercentage(CompactLength.Percent(val));

        // ── Constants ─────────────────────────────────────────────────────────

        public static readonly LengthPercentage ZERO = Length(0f);

        // ── Accessors ─────────────────────────────────────────────────────────

        public byte Tag => Inner.Tag;
        public float Value => Inner.Value;

        // ── Equality ──────────────────────────────────────────────────────────

        public bool Equals(LengthPercentage other) => Inner == other.Inner;
        public override bool Equals(object? obj) => obj is LengthPercentage other && Equals(other);
        public override int GetHashCode() => Inner.GetHashCode();
        public static bool operator ==(LengthPercentage a, LengthPercentage b) => a.Equals(b);
        public static bool operator !=(LengthPercentage a, LengthPercentage b) => !a.Equals(b);

        public override string ToString() => Inner.ToString();

        public static implicit operator LengthPercentage(float val) => Length(val);
    }

    // ── LengthPercentageAuto ─────────────────────────────────────────────────

    /// <summary>
    /// A CSS length that can be a fixed value, a percentage, or <c>auto</c>.
    /// Valid variants: <c>Length</c>, <c>Percent</c>, <c>Auto</c>.
    /// </summary>
    public readonly struct LengthPercentageAuto : System.IEquatable<LengthPercentageAuto>
    {
        internal readonly CompactLength Inner;

        private LengthPercentageAuto(CompactLength inner) => Inner = inner;

        // ── Factories ─────────────────────────────────────────────────────────

        public static LengthPercentageAuto Length(float val) => new LengthPercentageAuto(CompactLength.Length(val));
        public static LengthPercentageAuto Percent(float val) => new LengthPercentageAuto(CompactLength.Percent(val));
        public static LengthPercentageAuto Auto() => new LengthPercentageAuto(CompactLength.Auto());

        // ── Constants ─────────────────────────────────────────────────────────

        public static readonly LengthPercentageAuto ZERO = Length(0f);
        public static readonly LengthPercentageAuto AUTO = Auto();

        // ── Conversions ───────────────────────────────────────────────────────

        public static implicit operator LengthPercentageAuto(LengthPercentage lp) =>
            new LengthPercentageAuto(lp.Inner);

        public static implicit operator LengthPercentageAuto(float val) =>
            new LengthPercentageAuto(CompactLength.Length(val));

        // ── Accessors ─────────────────────────────────────────────────────────

        public byte Tag => Inner.Tag;
        public float Value => Inner.Value;
        public bool IsAuto() => Inner.IsAuto();

        /// <summary>
        /// Resolves to <c>Some(length)</c> for Length, <c>Some(context * pct)</c> for Percent,
        /// or <c>null</c> for Auto.
        /// </summary>
        public float? ResolveToOption(float context)
        {
            if (Inner.Tag == CompactLength.LENGTH_TAG) return Inner.Value;
            if (Inner.Tag == CompactLength.PERCENT_TAG) return context * Inner.Value;
            return null; // Auto
        }

        // ── Equality ──────────────────────────────────────────────────────────

        public bool Equals(LengthPercentageAuto other) => Inner == other.Inner;
        public override bool Equals(object? obj) => obj is LengthPercentageAuto other && Equals(other);
        public override int GetHashCode() => Inner.GetHashCode();
        public static bool operator ==(LengthPercentageAuto a, LengthPercentageAuto b) => a.Equals(b);
        public static bool operator !=(LengthPercentageAuto a, LengthPercentageAuto b) => !a.Equals(b);

        public override string ToString() => Inner.ToString();
    }

    // ── Dimension ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A CSS size dimension: Length, Percent, or Auto.
    /// Used for width, height, min/max sizing, flex-basis, etc.
    /// </summary>
    public readonly struct Dimension : System.IEquatable<Dimension>
    {
        internal readonly CompactLength Inner;

        private Dimension(CompactLength inner) => Inner = inner;

        // ── Factories ─────────────────────────────────────────────────────────

        public static Dimension Length(float val) => new Dimension(CompactLength.Length(val));
        public static Dimension Percent(float val) => new Dimension(CompactLength.Percent(val));
        public static Dimension Auto() => new Dimension(CompactLength.Auto());

        // ── Constants ─────────────────────────────────────────────────────────

        public static readonly Dimension ZERO = Length(0f);
        public static readonly Dimension AUTO = Auto();

        // ── Conversions ───────────────────────────────────────────────────────

        public static implicit operator Dimension(LengthPercentage lp) => new Dimension(lp.Inner);
        public static implicit operator Dimension(LengthPercentageAuto l) => new Dimension(l.Inner);
        
        public static implicit operator Dimension(float val) => Length(val);

        // ── Accessors ─────────────────────────────────────────────────────────

        public byte Tag => Inner.Tag;
        public float Value => Inner.Value;
        public bool IsAuto() => Inner.IsAuto();

        /// <summary>
        /// Returns the length value if this is a <c>Length</c> variant, otherwise <c>null</c>.
        /// </summary>
        public float? IntoOption() =>
            Inner.Tag == CompactLength.LENGTH_TAG ? Inner.Value : null;

        // ── Equality ──────────────────────────────────────────────────────────

        public bool Equals(Dimension other) => Inner == other.Inner;
        public override bool Equals(object? obj) => obj is Dimension other && Equals(other);
        public override int GetHashCode() => Inner.GetHashCode();
        public static bool operator ==(Dimension a, Dimension b) => a.Equals(b);
        public static bool operator !=(Dimension a, Dimension b) => !a.Equals(b);

        public override string ToString() => Inner.ToString();
    }
}