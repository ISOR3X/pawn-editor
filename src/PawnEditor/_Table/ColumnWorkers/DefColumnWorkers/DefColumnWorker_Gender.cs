using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class DefColumnWorker_Gender : DefColumnWorker
{
    private static readonly Texture2D MaleUsually = ContentFinder<Texture2D>.Get("UI/Icons/Gender/MaleUsually");
    private static readonly Texture2D FemaleUsually = ContentFinder<Texture2D>.Get("UI/Icons/Gender/FemaleUsually");

    public override bool Sortable => true;

    public override int Compare(Def a, Def b)
        => GetOrder(a).CompareTo(GetOrder(b));

    private static int GetOrder(Def def)
    {
        if (def is not HairDef hair) return -1;
        return hair.styleGender switch
        {
            StyleGender.Male => 0,
            StyleGender.MaleUsually => 1,
            StyleGender.Any => 2,
            StyleGender.FemaleUsually => 3,
            StyleGender.Female => 4,
            _ => 2
        };
    }

    private static Texture2D? GetIcon(Def def)
    {
        if (def is not HairDef hair) return Gender.None.GetIcon();
        return hair.styleGender switch
        {
            StyleGender.MaleUsually => MaleUsually,
            StyleGender.Male => Gender.Male.GetIcon(),
            StyleGender.FemaleUsually => FemaleUsually,
            StyleGender.Female => Gender.Female.GetIcon(),
            _ => Gender.None.GetIcon()
        };
    }

    protected override void DrawCellContent(Rect r, Def row)
    {
        var icon = GetIcon(row);
        if (icon == null) return;
        var cell = r.ContractedBy(2f);
        var size = Mathf.Min(cell.width, cell.height);
        var centeredRect = new Rect(cell.x + (cell.width - size) / 2f, cell.y + (cell.height - size) / 2f, size, size);
        GUI.DrawTexture(centeredRect, icon);
    }
}
