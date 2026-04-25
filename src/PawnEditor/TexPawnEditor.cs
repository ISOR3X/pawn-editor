using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class TexPawnEditor
{
    public static readonly Texture2D Randomize = ContentFinder<Texture2D>.Get("UI/Buttons/Randomize");

    static TexPawnEditor()
    {
    }
}