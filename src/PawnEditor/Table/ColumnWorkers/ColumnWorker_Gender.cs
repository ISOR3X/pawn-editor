using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ColumnWorker_Gender : ColumnWorker_Icon
{
    public static readonly Texture2D MaleUsually = ContentFinder<Texture2D>.Get("UI/Icons/Gender/MaleUsually");
    public static readonly Texture2D FemaleUsually = ContentFinder<Texture2D>.Get("UI/Icons/Gender/FemaleUsually");

    protected override Texture2D GetIconFor(Def thing)
    {
        if (thing is not HairDef hair) return null;
        switch (hair.styleGender)
        {
            case StyleGender.MaleUsually:
                return MaleUsually;
            case StyleGender.Male:
                return Gender.Male.GetIcon();
            case StyleGender.FemaleUsually:
                return FemaleUsually;
            case StyleGender.Female:
                return Gender.Female.GetIcon();
            default:
                return Gender.None.GetIcon();
        }
    }

    protected override string GetIconTip(Def thing) => null;
}