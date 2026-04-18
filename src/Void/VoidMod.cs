using Verse;

namespace Void;

public class VoidMod : Mod
{
    public static string ModName = "Void";
    public static VoidSettings Settings = new();
    
    public VoidMod(ModContentPack content) : base(content)
    {
    }
}