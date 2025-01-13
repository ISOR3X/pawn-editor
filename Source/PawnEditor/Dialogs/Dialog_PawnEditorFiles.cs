using System;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class Dialog_PawnEditorFiles : Dialog_FileList
{
    protected readonly string type;

    protected readonly Action<string> usePath;

    protected Dialog_PawnEditorFiles(string type, Action<string> onPath)
    {
        this.type = type;
        usePath = onPath;
    }

    public override void PostOpen()
    {
        base.PostOpen();
        ReloadFiles();
    }

    //Create the Save/Load Pawn window
    public override void DoWindowContents(Rect inRect)
    {
        var vector = new Vector2(inRect.width - 16f, 40f);
        var y = vector.y;
        var height = files.Count * y;
        var viewRect = new Rect(0f, 0f, inRect.width - 16f, height);
        var num = inRect.height - CloseButSize.y - bottomAreaHeight - 18f;
        if (ShouldDoTypeInField) num -= 53f;
        var outRect = inRect.TopPartPixels(num);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        var num2 = 0f;
        var num3 = 0;
        foreach (var saveFileInfo in files)
        {
            if (num2 + vector.y >= scrollPosition.y && num2 <= scrollPosition.y + outRect.height)
            {
                var rect = new Rect(0f, num2, vector.x, vector.y);
                if (num3 % 2 == 0) Widgets.DrawAltRect(rect);
                Widgets.BeginGroup(rect);
                var rect2 = new Rect(rect.width - 36f, (rect.height - 36f) / 2f, 36f, 36f);
                if (Widgets.ButtonImage(rect2, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
                {
                    var localFile = saveFileInfo.FileInfo;
                    Find.WindowStack.Add(new Dialog_Confirm("ConfirmDelete".Translate(localFile.Name), "ConfirmDelete" + type, delegate
                    {
                        localFile.Delete();
                        ReloadFiles();
                    }, true));
                }

                TooltipHandler.TipRegionByKey(rect2, deleteTipKey);
                Text.Font = GameFont.Small;
                var rect3 = new Rect(rect2.x - 100f, (rect.height - 36f) / 2f, 100f, 36f);
                if (Widgets.ButtonText(rect3, interactButLabel)) DoFileInteraction(Path.GetFileNameWithoutExtension(saveFileInfo.FileName));
                var rect4 = new Rect(rect3.x - 94f, 0f, 94f, rect.height);
                DrawDateAndVersion(saveFileInfo, rect4);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = FileNameColor(saveFileInfo);
                var leftMargin = saveFileInfo is ExtendedSaveFileInfo { Icon: not null } ? 50f : 8f;
                var rect5 = new Rect(leftMargin, 0f, rect4.x - leftMargin - 4f, rect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(saveFileInfo.FileName);
                Widgets.Label(rect5, fileNameWithoutExtension.Truncate(rect5.width * 1.8f));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                if (saveFileInfo is ExtendedSaveFileInfo { Icon: { } icon })
                {
                    var rect6 = new Rect(8f, 0f, 40f, 40f);
                    GUI.DrawTexture(rect6, icon);
                }

                Widgets.EndGroup();
            }

            num2 += vector.y;
            num3++;
        }
        Widgets.EndScrollView();
        if (ShouldDoTypeInField) DoTypeInField(inRect.TopPartPixels(inRect.height - CloseButSize.y - 18f));

        //Show tickbox for loading/saving with random faction only if you're saving a human.
        if (type.Contains("Humans"))
        {
            Rect useRandomCheckRect = new Rect(420, 625, 160, 30);
            Widgets.CheckboxLabeled(useRandomCheckRect, "PawnEditor.WithRandomFaction".Translate(), ref SaveLoadUtility.UseRandomFactionOnSave);
        }
    }

    public override void ReloadFiles()
    {
        if (type.NullOrEmpty()) return;
        files.Clear();
        foreach (var file in SaveLoadUtility.SaveFolderForItemType(type)
                    .GetFiles()
                    .Where(file => file.Extension == ".xml")
                    .OrderByDescending(file => file.LastWriteTime))
        {
            var info = new ExtendedSaveFileInfo(file);
            info.LoadAllData();
            files.Add(info);
        }
    }

    public override void DoFileInteraction(string fileName)
    {
        usePath(SaveLoadUtility.FilePathFor(type, fileName));
        Close();
    }
}

public class ExtendedSaveFileInfo : SaveFileInfo
{
    public Texture2D Icon;
    public ExtendedSaveFileInfo(FileInfo fileInfo) : base(fileInfo) { }

    public void LoadAllData()
    {
        LoadData();
        var iconFile = new FileInfo(Path.ChangeExtension(fileInfo.FullName, ".png"));
        if (iconFile.Exists)
        {
            var data = File.ReadAllBytes(iconFile.FullName);
            var texture2D = new Texture2D(128, 128, TextureFormat.Alpha8, true);
            texture2D.LoadImage(data);
            if (Prefs.TextureCompression) texture2D.Compress(true);

            texture2D.name = Path.GetFileNameWithoutExtension(iconFile.Name);
            texture2D.filterMode = FilterMode.Trilinear;
            texture2D.anisoLevel = 2;
            texture2D.Apply(true, true);
            Icon = texture2D;
        }
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
