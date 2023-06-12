using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class TexPawnEditor
{
    public static Texture2D OpenPawnEditor = ContentFinder<Texture2D>.Get("UI/Buttons/DevRoot/OpenPawnEditor");
    public static Texture2D OpenColorPicker = ContentFinder<Texture2D>.Get("UI/Widgets/OpenColorPicker");
}
