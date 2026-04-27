// Port of taffy/src/style/grid.rs - RepetitionCount, GridTemplateRepeat, GridTemplateComponent.
//
// GridTemplateComponent is the element type for gridTemplateColumns/gridTemplateRows.
// It is either a plain TrackSizingFunction (Single) or a repeat() group (Repeat).

namespace Taffy;

/// <summary>The repetition strategy for a repeat() template component.</summary>
public enum GridTrackRepetition { AutoFill, AutoFit }

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
    private readonly bool _isRepeat;
    private readonly TrackSizingFunction _single;
    private readonly GridTemplateRepeat _repeat;

    private GridTemplateComponent(TrackSizingFunction single)
    {
        _isRepeat = false;
        _single = single;
    }

    private GridTemplateComponent(GridTemplateRepeat repeat)
    {
        _isRepeat = true;
        _repeat = repeat;
    }

    public bool IsRepeat => _isRepeat;
    public bool IsSingle => !_isRepeat;

    public TrackSizingFunction AsSingle() => _single;
    public GridTemplateRepeat AsRepeat() => _repeat;

    public static GridTemplateComponent Single(TrackSizingFunction track) => new(track);

    public static GridTemplateComponent Repeat(GridTrackRepetition count, List<TrackSizingFunction> tracks)
        => new(new GridTemplateRepeat(count, tracks));

    public static implicit operator GridTemplateComponent(TrackSizingFunction t) => Single(t);
}
