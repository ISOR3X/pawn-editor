using System.Xml;
using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor.XMLComponents;

/// <summary>
///     Represents a
///     <c>
///         <section>
///     </c>
///     element within a layout tree. Delegates rendering to the
///     referenced <see cref="SectionDef" />'s worker, applying tab-level style composition.
///     The <see cref="_pawn" /> property is set via <see cref="SetContext" /> before
///     <see cref="Render" /> is called.
/// </summary>
public class SectionElement : XMLComponent
{
    /// <summary>Set via SetContext before each call to Render.</summary>
    private Pawn? _pawn;

    public SectionDef? ResolvedDef;

    public override bool IsLeaf => true;

    public override void SetContext(IContext? context)
    {
        _pawn = (context as IContext<Pawn>)?.Value;
    }

    public override void ParseXmlAttrs(XmlNode node)
    {
        var defName = node.InnerText.Trim();
        if (!defName.NullOrEmpty())
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(ResolvedDef), defName);
    }

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        var worker = ResolvedDef?.Worker;
        if (worker == null || _pawn == null) return;
        if (!worker.ShowSection(_pawn)) return;
        // Delegate entirely to BuildSection - it handles both UILayout (merge) and legacy (wrap).
        worker.BuildSection(builder, _pawn, Style, Attrs);
    }
}