using Verse;
using Void.XML;

namespace Void;

/// <summary>
/// A standalone XML layout. The <c>layout</c> element is parsed once all defs are loaded, since class
/// attributes resolve against <see cref="StyleMapDef"/>.
/// </summary>
public class LayoutDef : Def
{
    public ParsedLayout? layout;

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        layout?.ParseAndResolveReferences();
    }
}
