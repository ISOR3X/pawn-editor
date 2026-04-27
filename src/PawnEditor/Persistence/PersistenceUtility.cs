using Verse;

namespace PawnEditor;

public static class PersistenceUtility
{
    private static string PresetFolder =>
        Path.Combine(GenFilePaths.SaveDataFolderPath, "PawnEditor", "Presets");

    public static string[] GetPresetFiles() =>
        Directory.Exists(PresetFolder) ? Directory.GetFiles(PresetFolder, "*.xml") : [];

    public static void SavePawn(Pawn pawn, string name)
    {
        Directory.CreateDirectory(PresetFolder);
        var path = Path.Combine(PresetFolder, $"{name}.xml");
        Scribe.saver.InitSaving(path, "pawnPreset");
        Scribe_Deep.Look(ref pawn, "pawn");
        Scribe.saver.FinalizeSaving();
    }

    public static Pawn? LoadPawn(string filePath)
    {
        // ProgramState must not be Playing during load: sub-component constructors (e.g.
        // Pawn_NeedsTracker) and PostLoadInits trigger LifeStageWorker_HumanlikeAdult.
        // Notify_LifeStageStarted, which accesses pawn.story before it is populated and
        // crashes. MapInitializing matches the state used by the game's own save loader.
        var prevState = Current.ProgramState;
        Current.ProgramState = ProgramState.MapInitializing;
        try
        {
            Scribe.loader.InitLoading(filePath);
            Pawn? pawn = null;
            Scribe_Deep.Look(ref pawn, "pawn");
            IsolatedLoader.FinalizeLoadingIsolated();

            if (pawn == null) return null;
            PawnFixup.FixupLoadedPawn(pawn);
            PawnFixup.ReassignIDs(pawn);
            return pawn;
        }
        finally
        {
            Current.ProgramState = prevState;
        }
    }
}
