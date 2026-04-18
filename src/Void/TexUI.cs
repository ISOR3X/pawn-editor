using System.Reflection;
using UnityEngine;
using Verse;

namespace Void;

[StaticConstructorOnStartup]
public static class TexUI
{
    public static readonly Texture2D ArrowUp = LoadResource("Void.Textures.ArrowUp.png");
    public static readonly Texture2D ArrowDown = LoadResource("Void.Textures.ArrowDown.png");
    public static readonly Texture2D ArrowLeft = LoadResource("Void.Textures.ArrowLeft.png");
    public static readonly Texture2D ArrowRight = LoadResource("Void.Textures.ArrowRight.png");
    public static readonly Texture2D ArrowLeftDouble = LoadResource("Void.Textures.ArrowLeftDouble.png");
    public static readonly Texture2D ArrowRightDouble = LoadResource("Void.Textures.ArrowRightDouble.png");

    private static Texture2D LoadResource(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName) ??
                           throw new FileNotFoundException("Could not find embedded resource: " + resourceName);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        tex.LoadImage(bytes);
        tex.Apply();
        return tex;
    }
}