using JetBrains.Annotations;
using Verse;

namespace Void;

/// <summary>
///     A RimWorld <see cref="Def" /> that maps CSS class names to inline style strings.
///     Any mod can define one; classes are available to all
///     <c>
///         <layout />
///     </c>
///     elements
///     via the <c>class="..."</c> attribute.
/// </summary>
/// <example>
///     <code>
/// <PawnEditor.TaffyStyleDef>
///             <defName>PawnEditorStyles</defName>
///             <styles>
///                 <li name="row" value="flex-direction: row" />
///                 <li name="wrap" value="flex-wrap: wrap" />
///                 <li name="w-full" value="width: 100%" />
///                 <li name="grow" value="flex-grow: 1" />
///                 <li name="gap-sm" value="gap: 4px" />
///             </styles>
///         </PawnEditor.TaffyStyleDef>
/// </code>
/// </example>
[UsedImplicitly]
public class StyleMapDef : Def
{
    public List<StyleEntry> styles = [];

    [field: Unsaved] public Dictionary<string, StyleOverride> Styles { get; private set; } = [];

    /// <summary>
    ///     PostLoad instead of ResolveReferences so the Styles dictionary is available immediately for other defs.
    /// </summary>
    public override void PostLoad()
    {
        base.PostLoad();
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