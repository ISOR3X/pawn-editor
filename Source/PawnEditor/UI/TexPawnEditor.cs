using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class TexPawnEditor
{
    public static readonly Texture2D OpenPawnEditor = ContentFinder<Texture2D>.Get("UI/Buttons/DevRoot/OpenPawnEditor");
    public static readonly Texture2D ArrowLeftHalf = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowLeft");
    public static readonly Texture2D ArrowRightHalf = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowRight");
    public static readonly Texture2D ArrowLeftHalfDouble = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowLeftDouble");
    public static readonly Texture2D ArrowRightHalfDouble = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowRightDouble");
    public static readonly Texture2D PassionEmptyTex = ContentFinder<Texture2D>.Get("UI/Buttons/Icons/PassionEmpty");
    public static readonly Texture2D GoToPawn = ContentFinder<Texture2D>.Get("UI/Buttons/GoToPawn");
    public static readonly Texture2D Randomize = ContentFinder<Texture2D>.Get("UI/Buttons/Randomize");
    public static readonly Texture2D Save = ContentFinder<Texture2D>.Get("UI/Buttons/Save");
    public static readonly Texture2D GendersTex = ContentFinder<Texture2D>.Get("UI/Icons/Gender/Genders");
    public static readonly Texture2D InvertFilter = ContentFinder<Texture2D>.Get("UI/Buttons/InvertFilter_a");
    public static readonly Texture2D InvertFilterActive = ContentFinder<Texture2D>.Get("UI/Buttons/InvertFilter_b");
    public static readonly Dictionary<BodyTypeDef, Texture2D> BodyTypeIcons;
    public static readonly Texture2D SkillBarBGTex = SolidColorMaterials.NewSolidColorTexture(0.137255f, 0.145098f, 0.156863f, 1);

    static TexPawnEditor()
    {
        Shader s = ShaderDatabase.CutoutSkinColorOverride;
        try { s = ShaderUtility.GetSkinShaderAbstract(true, false); } catch { }
        BodyTypeIcons = DefDatabase<BodyTypeDef>.AllDefs.ToDictionary(def => def,
            def => (Texture2D)GraphicDatabase
               .Get<Graphic_Multi>(def.bodyNakedGraphicPath, s, Vector2.one, Color.white)
               .MatSouth.mainTexture);
    }
}
