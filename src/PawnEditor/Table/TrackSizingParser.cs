using System.Globalization;
using Taffy;

namespace PawnEditor.Table;

public static class TrackSizingParser
{
    /// <summary>
    /// Parses a CSS Grid track-sizing string into a <see cref="TrackSizingFunction"/>.
    /// Supported forms:
    /// <list type="bullet">
    ///   <item><c>Nfr</c> — fractional unit, e.g. <c>1fr</c>, <c>0.5fr</c></item>
    ///   <item><c>Npx</c> — fixed pixels, e.g. <c>24px</c></item>
    ///   <item><c>auto</c> — automatic sizing</item>
    /// </list>
    /// </summary>
    public static TrackSizingFunction Parse(string value)
    {
        if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return TrackSizingFunction.Auto();

        if (value.EndsWith("fr", StringComparison.OrdinalIgnoreCase))
        {
            var numStr = value[..^2];
            if (float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var fr))
                return TrackSizingFunction.Fr(fr);
        }
        else if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            var numStr = value[..^2];
            if (float.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var px))
                return TrackSizingFunction.Px(px);
        }

        throw new FormatException($"Cannot parse trackSize '{value}'. Expected 'auto', 'Nfr', or 'Npx'.");
    }
}
