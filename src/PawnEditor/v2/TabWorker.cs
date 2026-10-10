using System.Xml;
using Verse;
using Void.Native;
using Void.XML.Elements;

namespace PawnEditor.v2;

class TabWorker(TabDef def)
{
    public TabDef Def = def;

    public virtual void DoTabContents(UIBranch b)
    {
        b.Div(Def.layout._builder, style: Def.layout._style);
    }
}
