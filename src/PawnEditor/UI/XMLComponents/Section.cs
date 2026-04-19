using System.Xml;
using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor.XMLComponents;

/// <summary>
///     Represents a <c><section></c> element within a layout tree. Delegates rendering to the
///     referenced <see cref="SectionDef" />'s worker, applying tab-level style composition.
///     The <see cref="Pawn" /> property is set via <see cref="SetContext" /> before
///     <see cref="Render" /> is called.
/// </summary>
public class SectionElement : XMLComponent
{
    /// <summary>Set via SetContext before each call to Render.</summary>
    internal Pawn? Pawn;

    public SectionDef? ResolvedDef;

    public override bool IsLeaf => true;

    public override void SetContext(IContext? context)
    {
        Pawn = (context as IContext<Pawn>)?.Value;
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
        if (worker == null || Pawn == null) return;
        // Delegate entirely to BuildSection — it handles both UILayout (merge) and legacy (wrap).
        worker.BuildSection(builder, Pawn, Style);
    }
}