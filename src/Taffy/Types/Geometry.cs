// Port of taffy/src/geometry.rs
// Geometric primitives used throughout the layout engine.

namespace Taffy
{
    // ── Axes ─────────────────────────────────────────────────────────────────

    /// <summary>The simple absolute horizontal and vertical axis.</summary>
    public enum AbsoluteAxis : byte
    {
        Horizontal,
        Vertical,
    }

    /// <summary>The CSS abstract axis (inline = horizontal, block = vertical in LTR).</summary>
    public enum AbstractAxis : byte
    {
        /// <summary>The inline dimension (horizontal in horizontal writing modes).</summary>
        Inline,

        /// <summary>The block dimension (vertical in horizontal writing modes).</summary>
        Block,
    }

    // ── Size<T> ───────────────────────────────────────────────────────────────

    /// <summary>The width and height of a rectangular region.</summary>
    public struct Size<T>
    {
        public T Width;
        public T Height;

        public Size(T width, T height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>Apply <paramref name="f"/> to both Width and Height, returning a new Size.</summary>
        public Size<R> Map<R>(Func<T, R> f) => new Size<R>(f(Width), f(Height));

        /// <summary>Apply <paramref name="f"/> to Width and Height paired with the corresponding element of <paramref name="other"/>.</summary>
        public Size<R> ZipMap<U, R>(Size<U> other, Func<T, U, R> f) =>
            new Size<R>(f(Width, other.Width), f(Height, other.Height));

        public Size<T> MapWidth(Func<T, T> f) => new Size<T>(f(Width), Height);
        public Size<T> MapHeight(Func<T, T> f) => new Size<T>(Width, f(Height));

        // Flexbox axis helpers
        public T Main(FlexDirection dir) => dir.IsRow() ? Width : Height;
        public T Cross(FlexDirection dir) => dir.IsRow() ? Height : Width;

        public void SetMain(FlexDirection dir, T value)
        {
            if (dir.IsRow()) Width = value;
            else Height = value;
        }

        public void SetCross(FlexDirection dir, T value)
        {
            if (dir.IsRow()) Height = value;
            else Width = value;
        }

        public Size<T> WithMain(FlexDirection dir, T value) =>
            dir.IsRow() ? new Size<T>(value, Height) : new Size<T>(Width, value);

        public Size<T> WithCross(FlexDirection dir, T value) =>
            dir.IsRow() ? new Size<T>(Width, value) : new Size<T>(value, Height);

        public Size<T> MapMain(FlexDirection dir, Func<T, T> f) =>
            dir.IsRow() ? new Size<T>(f(Width), Height) : new Size<T>(Width, f(Height));

        public Size<T> MapCross(FlexDirection dir, Func<T, T> f) =>
            dir.IsRow() ? new Size<T>(Width, f(Height)) : new Size<T>(f(Width), Height);

        // Grid axis helpers
        public T Get(AbstractAxis axis) => axis == AbstractAxis.Inline ? Width : Height;

        public void Set(AbstractAxis axis, T value)
        {
            if (axis == AbstractAxis.Inline) Width = value;
            else Height = value;
        }

        // AbsoluteAxis helper
        public T GetAbs(AbsoluteAxis axis) => axis == AbsoluteAxis.Horizontal ? Width : Height;

        /// <summary>Returns a new Size with the given axis set to <paramref name="value"/>.</summary>
        public Size<T> WithAxis(AbstractAxis axis, T value) =>
            axis == AbstractAxis.Inline ? new Size<T>(value, Height) : new Size<T>(Width, value);

        /// <summary>Returns a new Size with the given axis set to <paramref name="value"/>.</summary>
        public Size<T> WithAxisAbs(AbsoluteAxis axis, T value) =>
            axis == AbsoluteAxis.Horizontal ? new Size<T>(value, Height) : new Size<T>(Width, value);

        public override string ToString() => $"Size({Width}, {Height})";
    }

    /// <summary>Static helpers for <see cref="Size{T}"/> with float components.</summary>
    public static class SizeF
    {
        public static readonly Size<float> ZERO = new Size<float>(0f, 0f);

        public static Size<float> Add(in Size<float> a, in Size<float> b) =>
            new Size<float>(a.Width + b.Width, a.Height + b.Height);

        public static Size<float> Sub(in Size<float> a, in Size<float> b) =>
            new Size<float>(a.Width - b.Width, a.Height - b.Height);

        public static Size<float> Max(in Size<float> a, in Size<float> b) =>
            new Size<float>(MathF.Max(a.Width, b.Width), MathF.Max(a.Height, b.Height));

        public static Size<float> Min(in Size<float> a, in Size<float> b) =>
            new Size<float>(MathF.Min(a.Width, b.Width), MathF.Min(a.Height, b.Height));

        public static bool HasNonZeroArea(in Size<float> s) => s.Width > 0f && s.Height > 0f;

        /// <summary>A <see cref="Size{T}"/> with <c>None</c> width and height.</summary>
        public static readonly Size<float?> NONE = new Size<float?>(null, null);

        public static Size<float?> NewOptional(float width, float height) =>
            new Size<float?>(width, height);

        public static Size<float> UnwrapOr(in Size<float?> s, in Size<float> alt) =>
            new Size<float>(s.Width ?? alt.Width, s.Height ?? alt.Height);

        public static bool BothAxisDefined(in Size<float?> s) => s.Width.HasValue && s.Height.HasValue;

        /// <summary>
        /// Applies aspect_ratio to the size: if only one axis is set and aspect_ratio is provided,
        /// computes the other axis.
        /// </summary>
        public static Size<float?> MaybeApplyAspectRatio(in Size<float?> s, float? aspectRatio)
        {
            if (aspectRatio == null) return s;
            var ratio = aspectRatio.Value;
            if (s.Width.HasValue && !s.Height.HasValue)
                return new Size<float?>(s.Width, s.Width.Value / ratio);
            if (!s.Width.HasValue && s.Height.HasValue)
                return new Size<float?>(s.Height.Value * ratio, s.Height);
            return s;
        }
    }

    // ── Rect<T> ───────────────────────────────────────────────────────────────

    /// <summary>An axis-aligned rectangle represented by its four edges.</summary>
    public struct Rect<T>
    {
        public T Left;
        public T Right;
        public T Top;
        public T Bottom;

        public Rect(T left, T right, T top, T bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        /// <summary>Apply <paramref name="f"/> to all four sides, returning a new Rect.</summary>
        public Rect<R> Map<R>(Func<T, R> f) =>
            new Rect<R>(f(Left), f(Right), f(Top), f(Bottom));

        /// <summary>Apply <paramref name="f"/> to each side paired with the corresponding component of <paramref name="size"/>.</summary>
        public Rect<R> ZipSize<U, R>(Size<U> size, Func<T, U, R> f) =>
            new Rect<R>(f(Left, size.Width), f(Right, size.Width), f(Top, size.Height), f(Bottom, size.Height));

        public Line<T> HorizontalComponents() => new Line<T>(Left, Right);
        public Line<T> VerticalComponents() => new Line<T>(Top, Bottom);

        // Flexbox axis helpers
        public T MainStart(FlexDirection dir) => dir.IsRow() ? Left : Top;
        public T MainEnd(FlexDirection dir) => dir.IsRow() ? Right : Bottom;
        public T CrossStart(FlexDirection dir) => dir.IsRow() ? Top : Left;
        public T CrossEnd(FlexDirection dir) => dir.IsRow() ? Bottom : Right;

        public override string ToString() => $"Rect(L={Left}, R={Right}, T={Top}, B={Bottom})";
    }

    /// <summary>Static helpers for <see cref="Rect{T}"/> with float components.</summary>
    public static class RectF
    {
        public static readonly Rect<float> ZERO = new Rect<float>(0f, 0f, 0f, 0f);

        public static float HorizontalAxisSum(in Rect<float> r) => r.Left + r.Right;
        public static float VerticalAxisSum(in Rect<float> r) => r.Top + r.Bottom;

        public static Size<float> SumAxes(in Rect<float> r) =>
            new Size<float>(HorizontalAxisSum(r), VerticalAxisSum(r));

        public static float MainAxisSum(in Rect<float> r, FlexDirection dir) =>
            dir.IsRow() ? HorizontalAxisSum(r) : VerticalAxisSum(r);

        public static float CrossAxisSum(in Rect<float> r, FlexDirection dir) =>
            dir.IsRow() ? VerticalAxisSum(r) : HorizontalAxisSum(r);

        public static float GridAxisSum(in Rect<float> r, AbsoluteAxis axis) =>
            axis == AbsoluteAxis.Horizontal ? HorizontalAxisSum(r) : VerticalAxisSum(r);

        public static Rect<float> Add(in Rect<float> a, in Rect<float> b) =>
            new Rect<float>(a.Left + b.Left, a.Right + b.Right, a.Top + b.Top, a.Bottom + b.Bottom);
    }

    // ── Point<T> ──────────────────────────────────────────────────────────────

    /// <summary>A 2-dimensional coordinate (top-left corner of a Rect when used with one).</summary>
    public struct Point<T>
    {
        public T X;
        public T Y;

        public Point(T x, T y)
        {
            X = x;
            Y = y;
        }

        public Point<R> Map<R>(Func<T, R> f) => new Point<R>(f(X), f(Y));

        public Point<T> Transpose() => new Point<T>(Y, X);

        // Grid axis helpers
        public T Get(AbstractAxis axis) => axis == AbstractAxis.Inline ? X : Y;

        public void Set(AbstractAxis axis, T value)
        {
            if (axis == AbstractAxis.Inline) X = value;
            else Y = value;
        }

        // Flexbox axis helpers
        public T Main(FlexDirection dir) => dir.IsRow() ? X : Y;
        public T Cross(FlexDirection dir) => dir.IsRow() ? Y : X;

        public Size<T> ToSize() => new Size<T>(X, Y);

        public override string ToString() => $"Point({X}, {Y})";
    }

    /// <summary>Static helpers for <see cref="Point{T}"/> with float components.</summary>
    public static class PointF
    {
        public static readonly Point<float> ZERO = new Point<float>(0f, 0f);
        public static readonly Point<float?> NONE = new Point<float?>(null, null);

        public static Point<float> Add(in Point<float> a, in Point<float> b) =>
            new Point<float>(a.X + b.X, a.Y + b.Y);
    }

    // ── Line<T> ───────────────────────────────────────────────────────────────

    /// <summary>An abstract "line" with a start and an end value.</summary>
    public struct Line<T>
    {
        public T Start;
        public T End;

        public Line(T start, T end)
        {
            Start = start;
            End = end;
        }

        public Line<R> Map<R>(Func<T, R> f) => new Line<R>(f(Start), f(End));

        public override string ToString() => $"Line({Start}, {End})";
    }

    public static class LineHelpers
    {
        public static readonly Line<bool> TRUE = new Line<bool>(true, true);
        public static readonly Line<bool> FALSE = new Line<bool>(false, false);

        public static float Sum(in Line<float> l) => l.Start + l.End;
    }

    // ── MinMax<Min, Max> ──────────────────────────────────────────────────────

    /// <summary>Holds a minimum and maximum value.</summary>
    public struct MinMax<TMin, TMax>
    {
        public TMin Min;
        public TMax Max;

        public MinMax(TMin min, TMax max)
        {
            Min = min;
            Max = max;
        }
    }

    // ── InBothAbsAxis<T> ─────────────────────────────────────────────────────

    /// <summary>Holds one item per absolute axis (used internally in grid layout).</summary>
    internal struct InBothAbsAxis<T>
    {
        public T Horizontal;
        public T Vertical;

        public InBothAbsAxis(T horizontal, T vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }

        public T Get(AbsoluteAxis axis) => axis == AbsoluteAxis.Horizontal ? Horizontal : Vertical;
    }
}