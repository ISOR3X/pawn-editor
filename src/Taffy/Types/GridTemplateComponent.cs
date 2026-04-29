// Port of taffy/src/style/grid.rs - RepetitionCount, GridTemplateRepeat, GridTemplateComponent.
//
// GridTemplateComponent is the element type for gridTemplateColumns/gridTemplateRows.
// It is either a plain TrackSizingFunction (Single) or a repeat() group (Repeat).

namespace Taffy;

/// <summary>The repetition strategy for a repeat() template component.</summary>
public enum GridTrackRepetition
{
    AutoFill,
    AutoFit
}

/// <summary>A repeat() group: a count and the list of tracks to repeat.</summary>
public readonly struct GridTemplateRepeat(GridTrackRepetition count, List<TrackSizingFunction> tracks)
{
    public readonly GridTrackRepetition Count = count;
    public readonly List<TrackSizingFunction> Tracks = tracks;
}

/// <summary>
///     An element of <c>grid-template-columns</c> or <c>grid-template-rows</c>.
///     Either a plain <see cref="TrackSizingFunction" /> or a <c>repeat(auto-fill/auto-fit, …)</c> group.
/// </summary>
public readonly struct GridTemplateComponent
{
    private readonly TrackSizingFunction _single;
    private readonly GridTemplateRepeat _repeat;

    private GridTemplateComponent(TrackSizingFunction single)
    {
        IsRepeat = false;
        _single = single;
    }

    private GridTemplateComponent(GridTemplateRepeat repeat)
    {
        IsRepeat = true;
        _repeat = repeat;
    }

    public bool IsRepeat { get; }

    public bool IsSingle => !IsRepeat;

    public TrackSizingFunction AsSingle()
    {
        return _single;
    }

    public GridTemplateRepeat AsRepeat()
    {
        return _repeat;
    }

    public static GridTemplateComponent Single(TrackSizingFunction track)
    {
        return new GridTemplateComponent(track);
    }

    public static GridTemplateComponent Repeat(GridTrackRepetition count, List<TrackSizingFunction> tracks)
    {
        return new GridTemplateComponent(new GridTemplateRepeat(count, tracks));
    }

    public static implicit operator GridTemplateComponent(TrackSizingFunction t)
    {
        return Single(t);
    }
}