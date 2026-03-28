using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class TexPawnEditor
{
    public static readonly Texture2D ArrowLeft = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowLeft");
    public static readonly Texture2D ArrowLeftDouble = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowLeftDouble");
    public static readonly Texture2D ArrowRight = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowRight");
    public static readonly Texture2D ArrowRightDouble = ContentFinder<Texture2D>.Get("UI/Buttons/ArrowRightDouble");
    public static readonly Texture2D Reroll = ContentFinder<Texture2D>.Get("UI/Buttons/Reroll");
    public static readonly Texture2D Up = ContentFinder<Texture2D>.Get("UI/Buttons/Up");
    public static readonly Texture2D Down = ContentFinder<Texture2D>.Get("UI/Buttons/Down");

    static TexPawnEditor()
    {
    }
}