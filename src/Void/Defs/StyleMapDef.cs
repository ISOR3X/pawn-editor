using JetBrains.Annotations;
using Verse;
using Void.Taffy;
using Void.XML;

namespace Void;

/// <summary>
///     A RimWorld <see cref="Def" /> that maps CSS class names to inline style strings.
/// </summary>
[UsedImplicitly]
public class StyleMapDef : Def
{
    public List<StyleEntry> styles = [];

    [field: Unsaved] public Dictionary<string, Style> Styles { get; private set; } = [];

    /// <summary>
    ///     The same class map for the pre-UITree API. Parsed from the same strings by the legacy
    ///     parser rather than converted, so the legacy path keeps its original behaviour exactly.
    ///     Delete along with Legacy/**.
    /// </summary>
    [field: Unsaved] public Dictionary<string, StyleOverride> LegacyStyles { get; private set; } = [];

    /// <summary>
    ///     PostLoad instead of ResolveReferences so the Styles dictionary is available immediately for other defs.
    /// </summary>
    public override void PostLoad()
    {
        base.PostLoad();
        Styles = styles.ToDictionary(
            e => e.name,
            e => StyleParser.ParseInlineStyle(e.value));
        LegacyStyles = styles.ToDictionary(
            e => e.name,
            e => TaffyStyleParser.ParseInlineStyle(e.value));
    }

    [UsedImplicitly]
    public class StyleEntry
    {
        public string name = string.Empty;
        public string value = string.Empty;
    }
}