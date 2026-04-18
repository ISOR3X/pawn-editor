using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class TexPawnEditor
{
    public static readonly Texture2D Reroll = ContentFinder<Texture2D>.Get("UI/Buttons/Reroll");

    static TexPawnEditor()
    {
    }
}