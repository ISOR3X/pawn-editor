namespace Void.Native;

// Hand-written halves of the value types generated into NativeMethods.g.cs: constructors, equality and the
// Dimension/TrackSizingFunction helpers, matching the API of the Taffy.dll types they replace.

public partial struct TaffyDimension(float value, TaffyUnit unit) : IEquatable<TaffyDimension>
{

    public TaffyUnit Unit => (TaffyUnit)unit;

    public bool Equals(TaffyDimension other) => value.Equals(other.value) && unit == other.unit;
    public override bool Equals(object? obj) => obj is TaffyDimension other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(value, unit);
    public static bool operator ==(TaffyDimension a, TaffyDimension b) => a.Equals(b);
    public static bool operator !=(TaffyDimension a, TaffyDimension b) => !a.Equals(b);
}

public partial struct TaffyTrackSizingFunction(TaffyDimension min, TaffyDimension max) : IEquatable<TaffyTrackSizingFunction>
{

    public bool Equals(TaffyTrackSizingFunction other) => min == other.min && max == other.max;
    public override bool Equals(object? obj) => obj is TaffyTrackSizingFunction other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(min, max);
}

public partial struct TaffyGridPlacement
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
}

/// <summary>
///     A pair of values per axis, used for gaps: <see cref="Width" /> is the gap between columns, <see cref="Height" />
///     the gap between rows. A component with unit <see cref="TaffyUnit.None" /> keeps the default.
/// </summary>
public partial struct TaffyAxes(TaffyDimension width, TaffyDimension height)
{

    public TaffyAxes(TaffyDimension all) : this(all, all)
    {
    }
}

/// <summary>
///     A value per edge, used for padding and margin. An edge with unit <see cref="TaffyUnit.None" /> keeps the default.
/// </summary>
public partial struct TaffyEdges(TaffyDimension top, TaffyDimension right, TaffyDimension bottom, TaffyDimension left)
{

    public TaffyEdges(TaffyDimension all) : this(all, all, all, all)
    {
    }

    /// <summary>Like the two-value CSS shorthand: top and bottom, then left and right.</summary>
    public TaffyEdges(TaffyDimension vertical, TaffyDimension horizontal)
        : this(vertical, horizontal, vertical, horizontal)
    {
    }
}

public static class Dimension
{
    public static TaffyDimension Px(float value) => new(value, TaffyUnit.Length);

    /// <param name="value">A fraction: 1 is 100%.</param>
    public static TaffyDimension Percent(float value) => new(value, TaffyUnit.Percent);

    public static TaffyDimension Auto() => new(0f, TaffyUnit.Auto);
    public static TaffyDimension MinContent() => new(0f, TaffyUnit.MinContent);
    public static TaffyDimension MaxContent() => new(0f, TaffyUnit.MaxContent);
    public static TaffyDimension Fr(float value) => new(value, TaffyUnit.Fr);
}

public static class TrackSizingFunction
{
    public static TaffyTrackSizingFunction Px(float value) => new(Dimension.Px(value), Dimension.Px(value));
    public static TaffyTrackSizingFunction Percent(float value) => new(Dimension.Percent(value), Dimension.Percent(value));

    /// <summary>A flexible track, <c>minmax(auto, Nfr)</c> as in CSS.</summary>
    public static TaffyTrackSizingFunction Fr(float value) => new(Dimension.Auto(), Dimension.Fr(value));

    public static TaffyTrackSizingFunction AutoTrack() => new(Dimension.Auto(), Dimension.Auto());
    public static TaffyTrackSizingFunction MinContent() => new(Dimension.MinContent(), Dimension.MinContent());
    public static TaffyTrackSizingFunction MaxContent() => new(Dimension.MaxContent(), Dimension.MaxContent());
    public static TaffyTrackSizingFunction MinMax(TaffyDimension min, TaffyDimension max) => new(min, max);
}
