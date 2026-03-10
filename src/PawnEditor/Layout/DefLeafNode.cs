using System.Xml;
using Verse;

namespace PawnEditor.Layout;

public class DefLeafNode<T> : LayoutNode<T> where T : Def
{
    public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        base.LoadDataFromXmlCustom(xmlRoot);
        if (!IsActive) return;
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "leaf", xmlRoot.InnerText.Trim());
    }
}