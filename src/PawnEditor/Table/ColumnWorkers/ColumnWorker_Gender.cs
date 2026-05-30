using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table.ColumnWorkers;

[StaticConstructorOnStartup]
public class ColumnWorker_Gender<T> : ColumnWorker<T> where T : StyleItemDef
{
    private static readonly Texture2D FemaleUsually = ContentFinder<Texture2D>.Get("UI/Icons/Gender/FemaleUsually");
    private static readonly Texture2D MaleUsually = ContentFinder<Texture2D>.Get("UI/Icons/Gender/MaleUsually");

    protected override string HeaderLabel => "Gender";

    public override TaffyTrackSizingFunction TrackSize => Void.Taffy.Px(CalcHeaderWidth(HeaderLabel));
    public override bool Sortable => true;

    public override void DrawCell(TaffyBuilder grid, T row)
    {
        grid.Icon(GetIcon(row));
    }

    public override int Compare(T a, T b)
    {
        return GetOrder(a).CompareTo(GetOrder(b));
    }

    private static int GetOrder(T def)
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

    private static Texture2D GetIcon(T def)
    {
        return def.styleGender switch
        {
            StyleGender.MaleUsually => MaleUsually,
            StyleGender.Male => Gender.Male.GetIcon(),
            StyleGender.FemaleUsually => FemaleUsually,
            StyleGender.Female => Gender.Female.GetIcon(),
            _ => Gender.None.GetIcon()
        };
    }

    private static float CalcHeaderWidth(string header)
    {
        var w = 0f;
        using (new TextBlock(GameFont.Small))
        {
            w += Text.CalcSize(header).x;
        }

        return w + GenUI.Gap;
    }
}