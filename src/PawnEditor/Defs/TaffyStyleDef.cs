using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

/// <summary>
/// A RimWorld <see cref="Def"/> that maps CSS class names to inline style strings.
/// Any mod can define one; classes are available to all <c>&lt;layout&gt;</c> elements
/// via the <c>class="..."</c> attribute.
/// </summary>
/// <example>
/// <code>
/// &lt;PawnEditor.TaffyStyleDef&gt;
///     &lt;defName&gt;PawnEditorStyles&lt;/defName&gt;
///     &lt;styles&gt;
///         &lt;li name="row" value="flex-direction: row" /&gt;
///         &lt;li name="wrap" value="flex-wrap: wrap" /&gt;
///         &lt;li name="w-full" value="width: 100%" /&gt;
///         &lt;li name="grow" value="flex-grow: 1" /&gt;
///         &lt;li name="gap-sm" value="gap: 4px" /&gt;
///     &lt;/styles&gt;
/// &lt;/PawnEditor.TaffyStyleDef&gt;
/// </code>
/// </example>
[UsedImplicitly]
public class TaffyStyleDef : Def
{
    public List<StyleEntry> styles = [];

    [field: Unsaved]
    public Dictionary<string, StyleOverride> Styles { get; private set; } = [];

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        Styles = styles.ToDictionary(
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
