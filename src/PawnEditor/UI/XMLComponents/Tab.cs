// XML-driven layout node backed by Taffy.
//
// Defines a tree of layout nodes parsed from XML, using CSS-style attribute names
// that mirror Taffy's own XML test format. Container nodes use <div>, leaf nodes
// that reference SectionDefs use <section>. The root <layout> element is always
// an implicit flex-column container.
//
// Example XML:
//   <layout>
//       <div display="flex" flex-direction="row">
//           <section flex-basis="60%">PawnEditor_Info</section>
//           <section flex-grow="1">PawnEditor_Portrait</section>
//       </div>
//       <section>PawnEditor_Skills</section>
//   </layout>

using System.Xml;
using HotSwap;
using Taffy;
using Verse;
using StyleOverride = Void.StyleOverride;
using TaffyBuilder = Void.TaffyBuilder;
using TaffyStyleParser = Void.TaffyStyleParser;
using VoidMod = Void.VoidMod;

namespace PawnEditor;

[HotSwappable]
public class Tab
{
    private readonly List<Tab> _children = [];
    private Func<bool> _isActive = static () => true;

    public SectionDef? section; // Public so DirectXmlCrossRefLoader can resolve it by field name after all defs load.

    /// <summary>The style of this node, used as the Taffy root style when this is the root layout node.</summary>
    public StyleOverride Style { get; private set; } = new();


    #region XML LOADING

    public void LoadDataFromXmlCustom(XmlNode xmlNode)
    {
        if (xmlNode.Attributes?["mayRequire"]?.Value is { } req
            && !ModLister.AllModsActiveNoSuffix(req.Split(',')))
        {
            _isActive = static () => false;
            return;
        }

        // Root <layout> defaults to a wrapping flex row with small gap.
        if (xmlNode.Name == "layout")
        {
            Style.gap = Void.Taffy.Gap(GenUI.GapSmall, GenUI.GapSmall);
            Style.flexWrap = FlexWrap.Wrap;
        }

        ParseStyleAttributes(xmlNode);

        if (xmlNode.Name == "section")
        {
            var defName = xmlNode.InnerText.Trim();
            if (!defName.NullOrEmpty())
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "section", defName);
        }
        else
        {
            foreach (XmlNode child in xmlNode.ChildNodes)
            {
                if (child is XmlComment) continue;
                if (child is XmlText txt && txt.Value?.Trim().Length == 0) continue;
                if (child.Name != "div" && child.Name != "section")
                {
                    Log.Error(
                        $"[{VoidMod.ModName}] Unknown layout element <{child.Name}>. Expected <div> or <section>.");
                    continue;
                }

                var childNode = new Tab();
                childNode.LoadDataFromXmlCustom(child);
                _children.Add(childNode);
            }
        }
    }

    private void ParseStyleAttributes(XmlNode xmlNode)
    {
        if (xmlNode.Attributes == null) return;
        foreach (XmlAttribute attr in xmlNode.Attributes)
        {
            // mayRequire is handled before this method is called.
            if (attr.Name is "mayRequire") continue;
            // style="..." is an inline CSS block — parse as a whole rather than as a single property.
            if (attr.Name is "style")
            {
                var parsed = TaffyStyleParser.ParseInlineStyle(attr.Value);
                Style = parsed.Merge(Style); // inline wins over any previously set values
                continue;
            }

            TaffyStyleParser.ApplyProperty(attr.Name, attr.Value, Style);
        }
    }

    #endregion

    #region DRAW / BUILDINTO

    /// <summary>
    ///     Adds this layout tree's children directly into <paramref name="col" />, without wrapping in a
    ///     root container. The root node's style should be passed as the root style of the enclosing
    ///     <see cref="Taffy.DivMeasured" /> call (via <see cref="Style" />).
    ///     Section XML nodes become flex-column containers whose items are populated by
    ///     <see cref="SectionWorker.DoSectionContents" />.
    /// </summary>
    public void BuildChildrenInto(TaffyBuilder col, Pawn pawn, Func<SectionDef, bool>? isVisible = null)
    {
        foreach (var child in _children)
        {
            if (!child._isActive()) continue;
            if (!child.HasVisibleContent(isVisible)) continue;
            BuildNode(col, child, pawn, isVisible);
        }
    }

    private static void BuildNode(TaffyBuilder col, Tab node, Pawn pawn,
        Func<SectionDef, bool>? isVisible)
    {
        if (node.section != null)
        {
            // Thread the tab-provided style into BuildSection so it can be merged onto the
            // section's root node (UILayout sections) or used as a wrapper (legacy sections).
            node.section.Worker.BuildSection(col, pawn, node.Style);
            return;
        }

        col.Div(inner =>
        {
            foreach (var child in node._children)
            {
                if (!child._isActive()) continue;
                if (!child.HasVisibleContent(isVisible)) continue;
                BuildNode(inner, child, pawn, isVisible);
            }
        }, node.Style);
    }

    private bool HasVisibleContent(Func<SectionDef, bool>? isVisible)
    {
        if (section != null)
            return isVisible == null || isVisible(section);
        foreach (var child in _children)
            if (child._isActive() && child.HasVisibleContent(isVisible))
                return true;
        return false;
    }

    #endregion
}