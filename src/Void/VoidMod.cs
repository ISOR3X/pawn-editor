using System.Runtime.InteropServices;
using Verse;

namespace Void;

public class VoidMod : Mod
{
    public static string ModName = "Void";
    public static VoidSettings Settings = new();

    public VoidMod(ModContentPack content) : base(content)
    {
        ModName = content.Name;

        string dllPath = Path.Combine(
            content.RootDir,
            "Native",
            "Windows",
            "x64",
            "ctaffy.dll");

        IntPtr handle = LoadLibrary(dllPath);

        Log.Message($"Loaded ctaffy.dll: {handle}");

        Settings = GetSettings<VoidSettings>();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);
}