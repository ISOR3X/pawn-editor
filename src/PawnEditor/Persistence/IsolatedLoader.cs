using Verse;

namespace PawnEditor;

public static class IsolatedLoader
{
    // Swaps in a fresh LoadedObjectDirectory so cross-references to objects outside the
    // preset file resolve to null instead of live-game objects, then restores the original.
    public static void FinalizeLoadingIsolated()
    {
        var crossRefs = Scribe.loader.crossRefs;
        var origDir = crossRefs.loadedObjectDirectory;
        crossRefs.loadedObjectDirectory = new LoadedObjectDirectory();
        try
        {
            Scribe.loader.FinalizeLoading();
        }
        finally
        {
            crossRefs.loadedObjectDirectory = origDir;
        }
    }
}