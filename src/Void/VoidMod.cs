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
        LoadNativeLibrary(content.RootDir);

        Settings = GetSettings<VoidSettings>();
    }

    private static void LoadNativeLibrary(string modRoot)
    {
        string path;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = Path.Combine(modRoot, "Native", "win-x64", "ctaffy.dll");
            var handle = LoadLibraryW(path);
            Log.Message($"[{ModName}] Loaded ctaffy (Windows): handle={handle}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = Path.Combine(modRoot, "Native", "linux-x64", "libctaffy.so");
            var handle = Dlopen(path, 2 /* RTLD_NOW */);
            Log.Message($"[{ModName}] Loaded ctaffy (Linux): handle={handle}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            path = Path.Combine(modRoot, "Native", "osx-arm64", "libctaffy.dylib");
            var handle = Dlopen(path, 2 /* RTLD_NOW */);
            Log.Message($"[{ModName}] Loaded ctaffy (macOS): handle={handle}");
        }
        else
        {
            Log.Error($"[{ModName}] Unknown OS — ctaffy native library not loaded.");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "LoadLibraryW")]
    private static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

    [DllImport("libdl.so", EntryPoint = "dlopen")]
    private static extern IntPtr Dlopen(string filename, int flags);
}