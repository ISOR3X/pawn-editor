using System.IO;
using Verse;

namespace PawnEditor;

public static class SaveLoadUtility
{
    public static string BaseSaveFolder => Path.Combine(GenFilePaths.SaveDataFolderPath, "PawnEditor");
}