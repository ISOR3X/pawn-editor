namespace PawnEditor.Layout;

/// <summary>
/// Defines the sizing of a single grid column track.
/// Either a fixed pixel width or a fractional share of remaining space.
/// </summary>
public readonly struct GridTrack
{
    public readonly float value;
    public readonly bool isFractional;

    private GridTrack(float value, bool isFractional)
    {
        this.value = value;
        this.isFractional = isFractional;
    }
    
    public static GridTrack Px(float pixels) => new(pixels, false);
    public static GridTrack Fr(float fraction = 1f) => new(fraction, true);
    public static implicit operator GridTrack(float pixels) => Px(pixels);
}