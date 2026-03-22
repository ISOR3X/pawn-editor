using System;

namespace FlexLayout
{
    /// <summary>
    /// A CSS-style length value that can be a pixel value, a percentage of a parent size, or Auto.
    /// Auto means the layout engine decides the size based on content or available space.
    /// </summary>
    public readonly struct StyleLength : IEquatable<StyleLength>
    {
        public enum Kind
        {
            Px,
            Pct,
            Auto
        }

        public readonly Kind Type;

        /// <summary>
        /// The raw value. For Px: pixels. For Pct: 0..100. Unused for Auto.
        /// </summary>
        public readonly float Value;

        private StyleLength(Kind type, float value)
        {
            Type = type;
            Value = value;
        }

        /// <summary>Fixed pixel size.</summary>
        public static StyleLength Px(float pixels) => new StyleLength(Kind.Px, pixels);

        /// <summary>Percentage of the parent's size along the same axis. 0..100.</summary>
        public static StyleLength Pct(float percent) => new StyleLength(Kind.Pct, percent);

        /// <summary>Let the layout engine decide.</summary>
        public static StyleLength Auto() => new StyleLength(Kind.Auto, 0f);

        public bool IsAuto => Type == Kind.Auto;
        public bool IsPx => Type == Kind.Px;
        public bool IsPct => Type == Kind.Pct;

        /// <summary>
        /// Resolves this length to an absolute pixel value given a parent size.
        /// Returns null if Auto (caller must handle).
        /// </summary>
        public float? Resolve(float parentSize)
        {
            switch (Type)
            {
                case Kind.Px: return Value;
                case Kind.Pct: return parentSize * (Value / 100f);
                case Kind.Auto: return null;
                default: throw new InvalidOperationException($"Unknown StyleLength kind: {Type}");
            }
        }

        /// <summary>
        /// Resolves to a pixel value, returning the provided fallback if Auto.
        /// </summary>
        public float ResolveOr(float parentSize, float fallback)
            => Resolve(parentSize) ?? fallback;

        public bool Equals(StyleLength other) => Type == other.Type && Value == other.Value;
        public override bool Equals(object obj) => obj is StyleLength s && Equals(s);
        public override int GetHashCode() => HashCode.Combine(Type, Value);
        public static bool operator ==(StyleLength a, StyleLength b) => a.Equals(b);
        public static bool operator !=(StyleLength a, StyleLength b) => !a.Equals(b);

        public override string ToString() => Type switch
        {
            Kind.Px => $"Px({Value})",
            Kind.Pct => $"Pct({Value}%)",
            Kind.Auto => "Auto",
            _ => "Unknown"
        };

        // Convenience: implicit from float → Px
        public static implicit operator StyleLength(float px) => Px(px);
    }
}