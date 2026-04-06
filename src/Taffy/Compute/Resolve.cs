// Port of taffy/src/util/resolve.rs
//
// Extension methods for resolving CSS dimension types to concrete float values
// given a parent-size context.  We omit the `calc()` callback since CSS calc()
// is not supported in this port — percentages are resolved directly.

namespace Taffy
{
    public static class ResolveExt
    {
        // ── LengthPercentage ─────────────────────────────────────────────────

        /// <summary>
        /// Resolves to <c>Some(length)</c> for Length or <c>Some(context * pct)</c> for Percent.
        /// Returns <c>null</c> if the value is a percentage and context is <c>null</c>.
        /// </summary>
        public static float? MaybeResolve(this LengthPercentage self, float? context)
        {
            if (self.Tag == CompactLength.LENGTH_TAG) return self.Value;
            // PERCENT_TAG
            return context.HasValue ? context.Value * self.Value : (float?)null;
        }

        /// <summary>Resolves to a concrete float, returning 0 when resolution yields <c>null</c>.</summary>
        public static float ResolveOrZero(this LengthPercentage self, float? context) =>
            self.MaybeResolve(context) ?? 0f;

        // ── LengthPercentageAuto ─────────────────────────────────────────────

        /// <summary>
        /// Resolves to <c>Some(length)</c>, <c>Some(context * pct)</c>, or <c>null</c> for Auto /
        /// unresolvable percentage.
        /// </summary>
        public static float? MaybeResolve(this LengthPercentageAuto self, float? context)
        {
            if (self.Tag == CompactLength.LENGTH_TAG) return self.Value;
            if (self.Tag == CompactLength.PERCENT_TAG)
                return context.HasValue ? context.Value * self.Value : (float?)null;
            return null; // Auto
        }

        /// <summary>Resolves to a concrete float, returning 0 when resolution yields <c>null</c>.</summary>
        public static float ResolveOrZero(this LengthPercentageAuto self, float? context) =>
            self.MaybeResolve(context) ?? 0f;

        // ── Dimension ────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves to <c>Some(length)</c>, <c>Some(context * pct)</c>, or <c>null</c> for Auto /
        /// unresolvable percentage.
        /// </summary>
        public static float? MaybeResolve(this Dimension self, float? context)
        {
            if (self.Tag == CompactLength.LENGTH_TAG) return self.Value;
            if (self.Tag == CompactLength.PERCENT_TAG)
                return context.HasValue ? context.Value * self.Value : (float?)null;
            return null; // Auto
        }

        /// <summary>Resolves to a concrete float, returning 0 when resolution yields <c>null</c>.</summary>
        public static float ResolveOrZero(this Dimension self, float? context) =>
            self.MaybeResolve(context) ?? 0f;

        // ── Size<Dimension> ──────────────────────────────────────────────────

        /// <summary>Resolves each axis independently against the corresponding context axis.</summary>
        public static Size<float?> MaybeResolve(this Size<Dimension> self, Size<float?> context) =>
            new Size<float?>(self.Width.MaybeResolve(context.Width), self.Height.MaybeResolve(context.Height));

        // ── Size<Dimension> (concrete context) ──────────────────────────────

        /// <summary>Resolves width against context.Width and height against context.Height (non-nullable context).</summary>
        public static Size<float?> MaybeResolve(this Size<Dimension> self, Size<float> context) =>
            new Size<float?>(self.Width.MaybeResolve(context.Width), self.Height.MaybeResolve(context.Height));

        // ── Size<LengthPercentage> ───────────────────────────────────────────

        /// <summary>Resolves width against context.Width and height against context.Height.</summary>
        public static Size<float> ResolveOrZero(this Size<LengthPercentage> self, Size<float?> context) =>
            new Size<float>(self.Width.ResolveOrZero(context.Width), self.Height.ResolveOrZero(context.Height));

        // ── Rect<LengthPercentage> ───────────────────────────────────────────

        /// <summary>Resolves all four sides against <paramref name="context"/> (width-based), returning 0 for null.</summary>
        public static Rect<float> ResolveOrZero(this Rect<LengthPercentage> self, float? context) =>
            new Rect<float>(
                self.Left.ResolveOrZero(context),
                self.Right.ResolveOrZero(context),
                self.Top.ResolveOrZero(context),
                self.Bottom.ResolveOrZero(context));

        /// <summary>
        /// Resolves left/right sides against context.Width and top/bottom against context.Height.
        /// </summary>
        public static Rect<float> ResolveOrZero(this Rect<LengthPercentage> self, Size<float?> context) =>
            new Rect<float>(
                self.Left.ResolveOrZero(context.Width),
                self.Right.ResolveOrZero(context.Width),
                self.Top.ResolveOrZero(context.Height),
                self.Bottom.ResolveOrZero(context.Height));

        // ── Rect<LengthPercentageAuto> ───────────────────────────────────────

        /// <summary>Resolves all four sides, returning <c>null</c> for Auto / unresolvable percentage.</summary>
        public static Rect<float?> MaybeResolve(this Rect<LengthPercentageAuto> self, float? context) =>
            new Rect<float?>(
                self.Left.MaybeResolve(context),
                self.Right.MaybeResolve(context),
                self.Top.MaybeResolve(context),
                self.Bottom.MaybeResolve(context));

        /// <summary>Resolves all four sides against <paramref name="context"/>, returning 0 for null.</summary>
        public static Rect<float> ResolveOrZero(this Rect<LengthPercentageAuto> self, float? context) =>
            new Rect<float>(
                self.Left.ResolveOrZero(context),
                self.Right.ResolveOrZero(context),
                self.Top.ResolveOrZero(context),
                self.Bottom.ResolveOrZero(context));
    }
}