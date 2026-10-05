using System.Runtime.InteropServices;
using Verse;
using Void.Native;

namespace Void;

public class VoidMod : Mod
{
    public static string ModName = "Void";
    public static VoidSettings Settings = new();

    public VoidMod(ModContentPack content) : base(content)
    {
        ModName = content.Name;
        // ctaffy is still used by the Legacy layout code; void_native backs UITree.
        LoadNativeLibrary(content.RootDir, "ctaffy");
        if (LoadNativeLibrary(content.RootDir, "void_native")) InitializeVoidNative();

        Settings = GetSettings<VoidSettings>();
    }

    private static bool LoadNativeLibrary(string modRoot, string name)
    {
        string path;
        IntPtr handle;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = Path.Combine(modRoot, "Native", "win-x64", $"{name}.dll");
            handle = LoadLibraryW(path);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = Path.Combine(modRoot, "Native", "linux-x64", $"lib{name}.so");
            handle = Dlopen(path, 2 /* RTLD_NOW */);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            path = Path.Combine(modRoot, "Native", "osx-arm64", $"lib{name}.dylib");
            handle = Dlopen(path, 2 /* RTLD_NOW */);
        }
        else
        {
            Log.Error($"[{ModName}] Unknown OS: {name} native library not loaded.");
            return false;
        }

        if (handle == IntPtr.Zero)
        {
            Log.Error($"[{ModName}] Could not load {name} from {path}.");
            return false;
        }

        Log.Message($"[{ModName}] Loaded {name}: handle={handle}");
        return true;
    }

    private static void InitializeVoidNative()
    {
        try
        {
            var mismatch = VoidNative.VerifyAbi();
            if (mismatch != null)
            {
                Log.Error($"[{ModName}] void_native does not match this build of Void ({mismatch}). UI will not work.");
                return;
            }

            MeasureRegistry.Install();
        }
        catch (Exception e)
        {
            Log.Error($"[{ModName}] Initializing void_native failed: {e}");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "LoadLibraryW")]
    private static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

    [DllImport("libdl.so", EntryPoint = "dlopen")]
    private static extern IntPtr Dlopen(string filename, int flags);
}