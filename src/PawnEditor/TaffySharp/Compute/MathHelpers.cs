// Port of taffy/src/util/math.rs
//
// Rust uses trait impls with generic bounds to express "maybe-math" operations.
// C# doesn't support extension methods with generic arithmetic constraints, so
// we provide concrete static helper classes for each type combination used in
// the compute algorithms.

using System;

namespace PawnEditor.TaffySharp
{
    // ── MaybeMath helpers ─────────────────────────────────────────────────────
    //
    // Convention:
    //   If the left-hand value is None, the result is None.
    //   If the right-hand value is None, it is treated as "no constraint" (identity for that op).

    /// <summary>
    /// "Maybe" arithmetic for <c>float?</c> values.
    /// <para>When the LHS is <c>null</c>, all operations return <c>null</c>.</para>
    /// </summary>
    public static class MaybeMathF
    {
        // ── float? op float? ──────────────────────────────────────────────────

        public static float? MaybeMin(this float? self, float? rhs)
        {
            if (self is null) return null;
            return rhs.HasValue ? MathF.Min(self.Value, rhs.Value) : self;
        }

        public static float? MaybeMax(this float? self, float? rhs)
        {
            if (self is null) return null;
            return rhs.HasValue ? MathF.Max(self.Value, rhs.Value) : self;
        }

        public static float? MaybeAdd(this float? self, float? rhs)
        {
            if (self is null) return null;
            return rhs.HasValue ? self.Value + rhs.Value : self;
        }

        public static float? MaybeSub(this float? self, float? rhs)
        {
            if (self is null) return null;
            return rhs.HasValue ? self.Value - rhs.Value : self;
        }

        public static float? MaybeClamp(this float? self, float? min, float? max)
        {
            if (self is null) return null;
            float v = self.Value;
            if (max.HasValue) v = MathF.Min(v, max.Value);
            if (min.HasValue) v = MathF.Max(v, min.Value);
            return v;
        }

        // ── float? op float ───────────────────────────────────────────────────

        public static float? MaybeMin(this float? self, float rhs) =>
            self.HasValue ? MathF.Min(self.Value, rhs) : null;

        public static float? MaybeMax(this float? self, float rhs) =>
            self.HasValue ? MathF.Max(self.Value, rhs) : null;

        public static float? MaybeAdd(this float? self, float rhs) =>
            self.HasValue ? self.Value + rhs : null;

        public static float? MaybeSub(this float? self, float rhs) =>
            self.HasValue ? self.Value - rhs : null;

        public static float? MaybeClamp(this float? self, float min, float max) =>
            self.HasValue ? MathF.Min(MathF.Max(self.Value, min), max) : null;

        // ── float op float? ───────────────────────────────────────────────────

        public static float MaybeMin(this float self, float? rhs) =>
            rhs.HasValue ? MathF.Min(self, rhs.Value) : self;

        public static float MaybeMax(this float self, float? rhs) =>
            rhs.HasValue ? MathF.Max(self, rhs.Value) : self;

        public static float MaybeAdd(this float self, float? rhs) =>
            rhs.HasValue ? self + rhs.Value : self;

        public static float MaybeSub(this float self, float? rhs) =>
            rhs.HasValue ? self - rhs.Value : self;

        public static float MaybeClamp(this float self, float? min, float? max)
        {
            float v = self;
            if (max.HasValue) v = MathF.Min(v, max.Value);
            if (min.HasValue) v = MathF.Max(v, min.Value);
            return v;
        }
    }

    /// <summary>
    /// "Maybe" arithmetic for <see cref="AvailableSpace"/> values.
    /// Constraints (MinContent / MaxContent) pass through unchanged for most ops.
    /// </summary>
    public static class MaybeMathAS
    {
        // ── AvailableSpace op float ───────────────────────────────────────────

        public static AvailableSpace MaybeMin(this AvailableSpace self, float rhs) =>
            self.IsDefinite ? AvailableSpace.Definite(MathF.Min(self.Unwrap(), rhs))
                            : AvailableSpace.Definite(rhs);

        public static AvailableSpace MaybeMax(this AvailableSpace self, float rhs) =>
            self.IsDefinite ? AvailableSpace.Definite(MathF.Max(self.Unwrap(), rhs)) : self;

        public static AvailableSpace MaybeAdd(this AvailableSpace self, float rhs) =>
            self.IsDefinite ? AvailableSpace.Definite(self.Unwrap() + rhs) : self;

        public static AvailableSpace MaybeSub(this AvailableSpace self, float rhs) =>
            self.IsDefinite ? AvailableSpace.Definite(self.Unwrap() - rhs) : self;

        public static AvailableSpace MaybeClamp(this AvailableSpace self, float min, float max) =>
            self.IsDefinite ? AvailableSpace.Definite(MathF.Min(MathF.Max(self.Unwrap(), min), max)) : self;

        // ── AvailableSpace op float? ──────────────────────────────────────────

        public static AvailableSpace MaybeMin(this AvailableSpace self, float? rhs)
        {
            if (!rhs.HasValue) return self;
            if (self.IsDefinite) return AvailableSpace.Definite(MathF.Min(self.Unwrap(), rhs.Value));
            return AvailableSpace.Definite(rhs.Value); // MinContent / MaxContent clamp to rhs
        }

        public static AvailableSpace MaybeMax(this AvailableSpace self, float? rhs)
        {
            if (!rhs.HasValue || !self.IsDefinite) return self;
            return AvailableSpace.Definite(MathF.Max(self.Unwrap(), rhs.Value));
        }

        public static AvailableSpace MaybeAdd(this AvailableSpace self, float? rhs)
        {
            if (!rhs.HasValue || !self.IsDefinite) return self;
            return AvailableSpace.Definite(self.Unwrap() + rhs.Value);
        }

        public static AvailableSpace MaybeSub(this AvailableSpace self, float? rhs)
        {
            if (!rhs.HasValue || !self.IsDefinite) return self;
            return AvailableSpace.Definite(self.Unwrap() - rhs.Value);
        }

        public static AvailableSpace MaybeClamp(this AvailableSpace self, float? min, float? max)
        {
            if (!self.IsDefinite) return self;
            float v = self.Unwrap();
            if (max.HasValue) v = MathF.Min(v, max.Value);
            if (min.HasValue) v = MathF.Max(v, min.Value);
            return AvailableSpace.Definite(v);
        }
    }

    /// <summary>
    /// "Maybe" arithmetic lifted over <see cref="Size{T}"/> — applies the operation component-wise.
    /// </summary>
    public static class MaybeMathSize
    {
        // ── Size<float?> ──────────────────────────────────────────────────────

        public static Size<float?> MaybeMin(this Size<float?> self, Size<float?> rhs) =>
            new Size<float?>(self.Width.MaybeMin(rhs.Width), self.Height.MaybeMin(rhs.Height));

        public static Size<float?> MaybeMax(this Size<float?> self, Size<float?> rhs) =>
            new Size<float?>(self.Width.MaybeMax(rhs.Width), self.Height.MaybeMax(rhs.Height));

        public static Size<float?> MaybeAdd(this Size<float?> self, Size<float?> rhs) =>
            new Size<float?>(self.Width.MaybeAdd(rhs.Width), self.Height.MaybeAdd(rhs.Height));

        public static Size<float?> MaybeSub(this Size<float?> self, Size<float?> rhs) =>
            new Size<float?>(self.Width.MaybeSub(rhs.Width), self.Height.MaybeSub(rhs.Height));

        // ── Size<float> op Size<float?> ───────────────────────────────────────

        public static Size<float> MaybeMin(this Size<float> self, Size<float?> rhs) =>
            new Size<float>(self.Width.MaybeMin(rhs.Width), self.Height.MaybeMin(rhs.Height));

        public static Size<float> MaybeMax(this Size<float> self, Size<float?> rhs) =>
            new Size<float>(self.Width.MaybeMax(rhs.Width), self.Height.MaybeMax(rhs.Height));

        public static Size<float> MaybeAdd(this Size<float> self, Size<float?> rhs) =>
            new Size<float>(self.Width.MaybeAdd(rhs.Width), self.Height.MaybeAdd(rhs.Height));

        public static Size<float> MaybeSub(this Size<float> self, Size<float?> rhs) =>
            new Size<float>(self.Width.MaybeSub(rhs.Width), self.Height.MaybeSub(rhs.Height));

        // ── Size<AvailableSpace> op Size<float?> ──────────────────────────────

        public static Size<AvailableSpace> MaybeMin(this Size<AvailableSpace> self, Size<float?> rhs) =>
            new Size<AvailableSpace>(self.Width.MaybeMin(rhs.Width), self.Height.MaybeMin(rhs.Height));

        public static Size<AvailableSpace> MaybeSub(this Size<AvailableSpace> self, Size<float?> rhs) =>
            new Size<AvailableSpace>(self.Width.MaybeSub(rhs.Width), self.Height.MaybeSub(rhs.Height));

        // ── Size<float?> op Size<float> ───────────────────────────────────────

        public static Size<float?> MaybeAdd(this Size<float?> self, Size<float> rhs) =>
            new Size<float?>(self.Width.MaybeAdd(rhs.Width), self.Height.MaybeAdd(rhs.Height));

        public static Size<float?> MaybeSub(this Size<float?> self, Size<float> rhs) =>
            new Size<float?>(self.Width.MaybeSub(rhs.Width), self.Height.MaybeSub(rhs.Height));

        public static Size<float?> MaybeMax(this Size<float?> self, Size<float> rhs) =>
            new Size<float?>(self.Width.MaybeMax(rhs.Width), self.Height.MaybeMax(rhs.Height));

        // ── Size<float?> coalescing / clamping ────────────────────────────────

        /// <summary>Per-axis: take <paramref name="self"/> if non-null, else <paramref name="other"/>.</summary>
        public static Size<float?> Or(this Size<float?> self, Size<float?> other) =>
            new Size<float?>(self.Width ?? other.Width, self.Height ?? other.Height);

        /// <summary>Per-axis maybe-clamp.</summary>
        public static Size<float?> MaybeClamp(this Size<float?> self, Size<float?> min, Size<float?> max) =>
            new Size<float?>(self.Width.MaybeClamp(min.Width, max.Width),
                             self.Height.MaybeClamp(min.Height, max.Height));

        // ── Size<float> op Size<float> (component-wise max for content_size) ──

        public static Size<float> F32Max(this Size<float> self, Size<float> rhs) =>
            new Size<float>(MathF.Max(self.Width, rhs.Width), MathF.Max(self.Height, rhs.Height));
    }
}
