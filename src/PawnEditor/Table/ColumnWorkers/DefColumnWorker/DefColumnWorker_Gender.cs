using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class DefColumnWorker_Gender : DefColumnWorker_Icon
{
    private static readonly Texture2D MaleUsually = ContentFinder<Texture2D>.Get("UI/Icons/Gender/MaleUsually");
    private static readonly Texture2D FemaleUsually = ContentFinder<Texture2D>.Get("UI/Icons/Gender/FemaleUsually");

    protected override Texture2D GetIconFor(Def thing)
    {
        if (thing is not HairDef hair) return Gender.None.GetIcon();
        return hair.styleGender switch
        {
            StyleGender.MaleUsually => MaleUsually,
            StyleGender.Male => Gender.Male.GetIcon(),
            StyleGender.FemaleUsually => FemaleUsually,
            StyleGender.Female => Gender.Female.GetIcon(),
            _ => Gender.None.GetIcon()
        };
    }

    protected override string? GetIconTip(Def thing)
    {
        return null;
    }
}