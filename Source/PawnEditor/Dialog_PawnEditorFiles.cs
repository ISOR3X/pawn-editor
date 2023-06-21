using System;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor;

public abstract class Dialog_PawnEditorFiles : Dialog_FileList
{
    protected readonly string type;

    protected readonly Action<string> usePath;

    protected Dialog_PawnEditorFiles(string type, Action<string> onPath)
    {
        this.type = type;
        usePath = onPath;
        ReloadFiles();
    }

    public override void ReloadFiles()
    {
        if (type.NullOrEmpty()) return;
        files.Clear();
        var dir = new DirectoryInfo(SaveLoadUtility.SaveFolderForItemType(type));
        if (!dir.Exists) dir.Create();
        foreach (var file in dir.GetFiles().Where(file => file.Extension == ".xml").OrderByDescending(file => file.LastWriteTime))
        {
            var info = new SaveFileInfo(file);
            info.LoadData();
            files.Add(info);
        }
    }

    public override void DoFileInteraction(string fileName)
    {
        usePath(SaveLoadUtility.FilePathFor(type, fileName));
        Close();
    }
}

public class Dialog_PawnEditorFiles_Save : Dialog_PawnEditorFiles
{
    public Dialog_PawnEditorFiles_Save(string type, Action<string> onPath, string name = "") : base(type, onPath)
    {
        interactButLabel = "OverwriteButton".Translate();
        if (name.NullOrEmpty()) return;
        var count = SaveLoadUtility.CountWithName(type, name);
        if (count > 0) name += " " + count;
        typingName = name;
    }

    public override bool ShouldDoTypeInField => true;
}

public class Dialog_PawnEditorFiles_Load : Dialog_PawnEditorFiles
{
    public Dialog_PawnEditorFiles_Load(string type, Action<string> onPath) : base(type, onPath) => interactButLabel = "Load".Translate();

    public override bool ShouldDoTypeInField => false;
}
