using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class TexPawnEditor
{
    public static Texture2D OpenPawnEditor = ContentFinder<Texture2D>.Get("UI/Buttons/DevRoot/OpenPawnEditor");
    public static Texture2D ArrowLeftHalf = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowLeft");
    public static Texture2D ArrowRightHalf = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowRight");
    public static Texture2D ArrowLeftHalfDouble = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowLeftDouble");
    public static Texture2D ArrowRightHalfDouble = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowRightDouble");
    public static Texture2D PassionEmptyTex = ContentFinder<Texture2D>.Get("UI/Buttons/Icons/PassionEmpty");
    public static Texture2D GoToPawn = ContentFinder<Texture2D>.Get("UI/Buttons/GoToPawn");
    public static Texture2D GendersTex = ContentFinder<Texture2D>.Get("UI/Icons/Gender/Genders");
    public static Dictionary<BodyTypeDef, Texture2D> BodyTypeIcons;
    public static Texture2D SkillBarBGTex = SolidColorMaterials.NewSolidColorTexture(0.137255f, 0.145098f, 0.156863f, 1);

    static TexPawnEditor()
    {
        BodyTypeIcons = DefDatabase<BodyTypeDef>.AllDefs.ToDictionary(def => def,
            def => (Texture2D)GraphicDatabase.Get<Graphic_Multi>(def.bodyNakedGraphicPath, ShaderUtility.GetSkinShader(false), Vector2.one, Color.white)
               .MatSouth.mainTexture);
    }
}
