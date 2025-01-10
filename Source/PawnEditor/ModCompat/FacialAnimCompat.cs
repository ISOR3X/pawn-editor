using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[ModCompat("Nals.FacialAnimation")]
public static class FacialAnimCompat
{
    public static bool Active;
    public static string Name = "Facial Animations";
    private static Type faceTypeDef;
    public static List<Def> FaceTypeDefs;


    [UsedImplicitly]
    public static void Activate()
    {
        faceTypeDef = AccessTools.TypeByName("FacialAnimation.FaceTypeDef");
        FaceTypeDefs = GenDefDatabase.GetAllDefsInDatabaseForDef(faceTypeDef).ToList();
        
        /*foreach (var typeDef in FaceTypeDefs)
        {
            // Log.Message(typeDef.defName);
        }*/
    }


}