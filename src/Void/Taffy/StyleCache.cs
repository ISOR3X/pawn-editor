using Void.Taffy;

/// <summary>
/// Style cache based on <paramref name="TKey" />.
/// </summary>
public sealed class StyleCache<TKey>(Func<TKey, Style> create) where TKey : struct
{
    private readonly Dictionary<TKey, Style> _styles = [];

    public Style Get(TKey key)
    {
        if (!_styles.TryGetValue(key, out var style))
            _styles[key] = style = create(key);
        return style;
    }
}
