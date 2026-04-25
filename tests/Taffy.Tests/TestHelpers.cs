using Taffy;
using Xunit;

namespace TaffyTests;

internal static class T
{
    public static Size<AvailableSpace> Viewport(float w, float h)
    {
        return new Size<AvailableSpace>(AvailableSpace.Definite(w), AvailableSpace.Definite(h));
    }

    public static Size<AvailableSpace> MaxContent()
    {
        return new Size<AvailableSpace>(AvailableSpace.MaxContent, AvailableSpace.MaxContent);
    }

    public static Dimension Px(float v)
    {
        return Dimension.Length(v);
    }

    public static LengthPercentage LPx(float v)
    {
        return LengthPercentage.Length(v);
    }

    public static Rect<LengthPercentage> Padding(float all)
    {
        return new Rect<LengthPercentage>(LPx(all), LPx(all), LPx(all), LPx(all));
    }

    public static Rect<LengthPercentage> Padding(float lr, float tb)
    {
        return new Rect<LengthPercentage>(LPx(lr), LPx(lr), LPx(tb), LPx(tb));
    }

    public static void AssertLayout(Layout layout, float x, float y, float w, float h, string? label = null)
    {
        var ctx = label is null ? string.Empty : $" [{label}]";
        Assert.True(MathF.Abs(layout.Location.X - x) <= 0.5f, $"Expected X={x} got {layout.Location.X}{ctx}");
        Assert.True(MathF.Abs(layout.Location.Y - y) <= 0.5f, $"Expected Y={y} got {layout.Location.Y}{ctx}");
        Assert.True(MathF.Abs(layout.Size.Width - w) <= 0.5f, $"Expected W={w} got {layout.Size.Width}{ctx}");
        Assert.True(MathF.Abs(layout.Size.Height - h) <= 0.5f, $"Expected H={h} got {layout.Size.Height}{ctx}");
    }
}
