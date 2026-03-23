// Port of taffy/src/style/compact_length.rs
//
// Rust's CompactLength uses tagged-pointer tricks for a 64-bit representation.
// In C# we use a plain struct with an explicit tag enum + float value — same
// semantics, no unsafe code required.

namespace PawnEditor.TaffySharp
{
    /// <summary>
    /// A compact representation of a CSS length value.
    /// Wraps a <see cref="Tag"/> discriminant and an optional <c>float</c> value.
    /// All dimension types (<see cref="LengthPercentage"/>, <see cref="LengthPercentageAuto"/>,
    /// <see cref="Dimension"/>) are thin wrappers around this type.
    /// </summary>
    public readonly struct CompactLength : System.IEquatable<CompactLength>
    {
        // Tag constants mirror the Rust source exactly (for cross-reference ease).
        public const byte LENGTH_TAG           = 0b0000_0001; // 1
        public const byte PERCENT_TAG          = 0b0000_0010; // 2
        public const byte AUTO_TAG             = 0b0000_0011; // 3
        public const byte FR_TAG               = 0b0000_0100; // 4
        public const byte MIN_CONTENT_TAG      = 0b0000_0111; // 7
        public const byte MAX_CONTENT_TAG      = 0b0000_1111; // 15
        public const byte FIT_CONTENT_PX_TAG   = 0b0001_0111; // 23
        public const byte FIT_CONTENT_PCT_TAG  = 0b0001_1111; // 31

        private readonly byte _tag;
        private readonly float _value;

        private CompactLength(byte tag, float value = 0f) { _tag = tag; _value = value; }

        // ── Factory methods ───────────────────────────────────────────────────

        /// <summary>An absolute length (pixels, logical pixels, etc.).</summary>
        public static CompactLength Length(float val) => new CompactLength(LENGTH_TAG, val);

        /// <summary>
        /// A percentage of the containing block.
        /// Values are in [0.0, 1.0], NOT [0.0, 100.0].
        /// </summary>
        public static CompactLength Percent(float val) => new CompactLength(PERCENT_TAG, val);

        /// <summary>Automatically computed size (CSS <c>auto</c>).</summary>
        public static CompactLength Auto() => new CompactLength(AUTO_TAG);

        /// <summary>A fraction of available grid space (<c>fr</c> unit).</summary>
        public static CompactLength Fr(float val) => new CompactLength(FR_TAG, val);

        /// <summary>The min-content intrinsic size.</summary>
        public static CompactLength MinContent() => new CompactLength(MIN_CONTENT_TAG);

        /// <summary>The max-content intrinsic size.</summary>
        public static CompactLength MaxContent() => new CompactLength(MAX_CONTENT_TAG);

        /// <summary>fit-content(<paramref name="limitPx"/> px).</summary>
        public static CompactLength FitContentPx(float limitPx) => new CompactLength(FIT_CONTENT_PX_TAG, limitPx);

        /// <summary>fit-content(<paramref name="limitPct"/> %).</summary>
        public static CompactLength FitContentPercent(float limitPct) => new CompactLength(FIT_CONTENT_PCT_TAG, limitPct);

        // ── Well-known constants ──────────────────────────────────────────────

        public static readonly CompactLength ZERO        = Length(0f);
        public static readonly CompactLength AUTO        = Auto();
        public static readonly CompactLength MIN_CONTENT = MinContent();
        public static readonly CompactLength MAX_CONTENT = MaxContent();

        // ── Accessors ─────────────────────────────────────────────────────────

        public byte Tag => _tag;

        /// <summary>The numeric value for Length / Percent / Fr / FitContent variants.</summary>
        public float Value => _value;

        // ── Query helpers ─────────────────────────────────────────────────────

        public bool IsZero()              => _tag == LENGTH_TAG && _value == 0f;
        public bool IsAuto()              => _tag == AUTO_TAG;
        public bool IsLengthOrPercent()   => _tag == LENGTH_TAG || _tag == PERCENT_TAG;
        public bool IsMinContent()        => _tag == MIN_CONTENT_TAG;
        public bool IsMaxContent()        => _tag == MAX_CONTENT_TAG;
        public bool IsFitContent()        => _tag == FIT_CONTENT_PX_TAG || _tag == FIT_CONTENT_PCT_TAG;
        public bool IsMaxOrFitContent()   => _tag == MAX_CONTENT_TAG || _tag == FIT_CONTENT_PX_TAG || _tag == FIT_CONTENT_PCT_TAG;
        public bool IsFr()                => _tag == FR_TAG;
        public bool IsMinOrMaxContent()   => _tag == MIN_CONTENT_TAG || _tag == MAX_CONTENT_TAG;

        /// <summary>
        /// True for Auto, MaxContent, FitContentPx, FitContentPercent.
        /// "In all cases, treat auto and fit-content() as max-content…" — CSS Grid spec.
        /// </summary>
        public bool IsMaxContentAlike() =>
            _tag == AUTO_TAG ||
            _tag == MAX_CONTENT_TAG ||
            _tag == FIT_CONTENT_PX_TAG ||
            _tag == FIT_CONTENT_PCT_TAG;

        public bool IsIntrinsic() =>
            _tag == AUTO_TAG ||
            _tag == MIN_CONTENT_TAG ||
            _tag == MAX_CONTENT_TAG ||
            _tag == FIT_CONTENT_PX_TAG ||
            _tag == FIT_CONTENT_PCT_TAG;

        public bool UsesPercentage() =>
            _tag == PERCENT_TAG || _tag == FIT_CONTENT_PCT_TAG;

        /// <summary>
        /// Resolves percentage values against <paramref name="parentSize"/>.
        /// Returns <c>null</c> for non-percentage variants.
        /// </summary>
        public float? ResolvedPercentageSize(float parentSize)
        {
            if (_tag == PERCENT_TAG) return _value * parentSize;
            return null;
        }

        // ── Equality ──────────────────────────────────────────────────────────

        public bool Equals(CompactLength other) => _tag == other._tag && _value == other._value;
        public override bool Equals(object? obj) => obj is CompactLength other && Equals(other);
        public override int GetHashCode() => System.HashCode.Combine(_tag, _value);
        public static bool operator ==(CompactLength a, CompactLength b) => a.Equals(b);
        public static bool operator !=(CompactLength a, CompactLength b) => !a.Equals(b);

        public override string ToString() => _tag switch
        {
            LENGTH_TAG          => $"{_value}px",
            PERCENT_TAG         => $"{_value * 100f}%",
            AUTO_TAG            => "auto",
            FR_TAG              => $"{_value}fr",
            MIN_CONTENT_TAG     => "min-content",
            MAX_CONTENT_TAG     => "max-content",
            FIT_CONTENT_PX_TAG  => $"fit-content({_value}px)",
            FIT_CONTENT_PCT_TAG => $"fit-content({_value * 100f}%)",
            _                   => $"unknown(tag={_tag})",
        };
    }
}
