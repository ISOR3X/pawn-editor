namespace Void.Native;

// Hand-written halves of the value types generated into NativeMethods.g.cs: constructors, equality and the
// Dimension/TrackSizingFunction helpers, matching the API of the Taffy.dll types they replace.

public partial struct TaffyDimension : IEquatable<TaffyDimension>
{
    public TaffyDimension(float value, TaffyUnit unit)
    {
        this.value = value;
        this.unit = (uint)unit;
    }

    public TaffyUnit Unit => (TaffyUnit)unit;

    public bool Equals(TaffyDimension other)
    {
        return value.Equals(other.value) && unit == other.unit;
    }

    public override bool Equals(object? obj)
    {
        return obj is TaffyDimension other && Equals(other);
    }

    public override int GetHashCode()
    {
        return unchecked(value.GetHashCode() * 31 + (int)unit);
    }

    public static bool operator ==(TaffyDimension a, TaffyDimension b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(TaffyDimension a, TaffyDimension b)
    {
        return !a.Equals(b);
    }
}

public partial struct TaffyTrackSizingFunction : IEquatable<TaffyTrackSizingFunction>
{
    public TaffyTrackSizingFunction(TaffyDimension min, TaffyDimension max)
    {
        this.min = min;
        this.max = max;
    }

    public bool Equals(TaffyTrackSizingFunction other)
    {
        return min == other.min && max == other.max;
    }

    public override bool Equals(object? obj)
    {
        return obj is TaffyTrackSizingFunction other && Equals(other);
    }

    public override int GetHashCode()
    {
        return unchecked(min.GetHashCode() * 31 + max.GetHashCode());
    }

    public static bool operator ==(TaffyTrackSizingFunction a, TaffyTrackSizingFunction b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(TaffyTrackSizingFunction a, TaffyTrackSizingFunction b)
    {
        return !a.Equals(b);
    }
}

public partial struct TaffyGridPlacement : IEquatable<TaffyGridPlacement>
{
    /// <param name="start">Grid line to start at, 0 = auto.</param>
    /// <param name="end">Grid line to end at, 0 = auto.</param>
    /// <param name="span">Number of tracks to span, only used when <paramref name="start" /> is 0.</param>
    public TaffyGridPlacement(short start = 0, short end = 0, ushort span = 0)
    {
        this.start = start;
        this.end = end;
        this.span = span;
    }

    public bool Equals(TaffyGridPlacement other)
    {
        return start == other.start && end == other.end && span == other.span;
    }

    public override bool Equals(object? obj)
    {
        return obj is TaffyGridPlacement other && Equals(other);
    }

    public override int GetHashCode()
    {
        return unchecked((start * 31 + end) * 31 + span);
    }

    public static bool operator ==(TaffyGridPlacement a, TaffyGridPlacement b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(TaffyGridPlacement a, TaffyGridPlacement b)
    {
        return !a.Equals(b);
    }
}

/// <summary>
///     A pair of values per axis, used for gaps: <see cref="Width" /> is the gap between columns, <see cref="Height" />
///     the gap between rows. A component with unit <see cref="TaffyUnit.None" /> keeps the default.
/// </summary>
public partial struct TaffyAxes : IEquatable<TaffyAxes>
{
    public TaffyAxes(TaffyDimension width, TaffyDimension height)
    {
        Width = width;
        Height = height;
    }

    public TaffyAxes(TaffyDimension all) : this(all, all)
    {
    }

    public bool Equals(TaffyAxes other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is TaffyAxes other && Equals(other);
    }

    public override int GetHashCode()
    {
        return unchecked(Width.GetHashCode() * 31 + Height.GetHashCode());
    }

    public static bool operator ==(TaffyAxes a, TaffyAxes b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(TaffyAxes a, TaffyAxes b)
    {
        return !a.Equals(b);
    }
}

/// <summary>
///     A value per edge, used for padding and margin. An edge with unit <see cref="TaffyUnit.None" /> keeps the default.
/// </summary>
public partial struct TaffyEdges : IEquatable<TaffyEdges>
{
    public TaffyEdges(TaffyDimension top, TaffyDimension right, TaffyDimension bottom, TaffyDimension left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public TaffyEdges(TaffyDimension all) : this(all, all, all, all)
    {
    }

    /// <summary>Like the two-value CSS shorthand: top and bottom, then left and right.</summary>
    public TaffyEdges(TaffyDimension vertical, TaffyDimension horizontal) : this(vertical, horizontal, vertical,
        horizontal)
    {
    }

    public bool Equals(TaffyEdges other)
    {
        return Top == other.Top && Right == other.Right && Bottom == other.Bottom && Left == other.Left;
    }

    public override bool Equals(object? obj)
    {
        return obj is TaffyEdges other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Top.GetHashCode() * 31 + Right.GetHashCode()) * 31 + Bottom.GetHashCode()) * 31 +
                   Left.GetHashCode();
        }
    }

    public static bool operator ==(TaffyEdges a, TaffyEdges b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(TaffyEdges a, TaffyEdges b)
    {
        return !a.Equals(b);
    }
}

public static class Dimension
{
    public static TaffyDimension Px(float value)
    {
        return new TaffyDimension(value, TaffyUnit.Length);
    }

    /// <param name="value">A fraction: 1 is 100%.</param>
    public static TaffyDimension Percent(float value)
    {
        return new TaffyDimension(value, TaffyUnit.Percent);
    }

    public static TaffyDimension Auto()
    {
        return new TaffyDimension(0f, TaffyUnit.Auto);
    }

    public static TaffyDimension MinContent()
    {
        return new TaffyDimension(0f, TaffyUnit.MinContent);
    }

    public static TaffyDimension MaxContent()
    {
        return new TaffyDimension(0f, TaffyUnit.MaxContent);
    }

    public static TaffyDimension Fr(float value)
    {
        return new TaffyDimension(value, TaffyUnit.Fr);
    }
}

public static class TrackSizingFunction
{
    public static TaffyTrackSizingFunction Px(float value)
    {
        return new TaffyTrackSizingFunction(Dimension.Px(value), Dimension.Px(value));
    }

    public static TaffyTrackSizingFunction Percent(float value)
    {
        return new TaffyTrackSizingFunction(Dimension.Percent(value), Dimension.Percent(value));
    }

    /// <summary>A flexible track, <c>minmax(auto, Nfr)</c> as in CSS.</summary>
    public static TaffyTrackSizingFunction Fr(float value)
    {
        return new TaffyTrackSizingFunction(Dimension.Auto(), Dimension.Fr(value));
    }

    public static TaffyTrackSizingFunction AutoTrack()
    {
        return new TaffyTrackSizingFunction(Dimension.Auto(), Dimension.Auto());
    }

    public static TaffyTrackSizingFunction MinContent()
    {
        return new TaffyTrackSizingFunction(Dimension.MinContent(), Dimension.MinContent());
    }

    public static TaffyTrackSizingFunction MaxContent()
    {
        return new TaffyTrackSizingFunction(Dimension.MaxContent(), Dimension.MaxContent());
    }

    public static TaffyTrackSizingFunction MinMax(TaffyDimension min, TaffyDimension max)
    {
        return new TaffyTrackSizingFunction(min, max);
    }
}
