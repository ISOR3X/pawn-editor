using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static partial class SaveLoadUtility
{
    private static bool currentlyWorking;
    private static ILoadReferenceable currentItem;
    private static Pawn currentPawn;
    private static readonly HashSet<ILoadReferenceable> savedItems = new();
    public static bool UseRandomFactionOnSave = false;

    public static MethodInfo ReferenceLook = AccessTools.FirstMethod(typeof(Scribe_References),
        mi => mi.Name == "Look" && mi.GetParameters().All(p => !p.ParameterType.Name.Contains("WeakReference")));

    public static string BaseSaveFolder => GenFilePaths.FolderUnderSaveData("PawnEditor");

    public static DirectoryInfo SaveFolderForItemType(string type)
    {
        var dir = new DirectoryInfo(Path.Combine(BaseSaveFolder, type.CapitalizeFirst()));
        var parent = dir.Parent;
        while (parent is { Exists: false })
        {
            parent.Create();
            parent = parent.Parent;
        }

        if (!dir.Exists) dir.Create();
        return dir;
    }

    public static string FilePathFor(string type, string name) => Path.Combine(BaseSaveFolder, type.CapitalizeFirst(), name + ".xml");

    public static int CountWithName(string type, string name) =>
        SaveFolderForItemType(type).GetFiles().Count(f => f.Extension == ".xml" && f.Name.StartsWith(name));

    public static void SaveItem<T>(T item, Action<T> callback = null, Pawn parentPawn = null, Action<T> prepare = null, string typePostfix = null)
        where T : IExposable
    {
        var type = typeof(T).Name;
        Find.WindowStack.Add(new Dialog_PawnEditorFiles_Save(typePostfix.NullOrEmpty() ? type : Path.Combine(type, typePostfix!), path =>
        {
            currentlyWorking = true;
            currentItem = item as ILoadReferenceable;
            currentPawn = parentPawn;
            savedItems.Clear();
            prepare?.Invoke(item);
            ApplyPatches();

            var tempFile = Path.GetTempFileName();
            Scribe.saver.InitSaving(tempFile, typePostfix.NullOrEmpty() ? type : type + "." + typePostfix);
            item.ExposeData();
            Scribe.saver.FinalizeSaving();
            File.Delete(tempFile);

            Scribe.saver.InitSaving(path, typePostfix.NullOrEmpty() ? type : type + "." + typePostfix);
            ScribeMetaHeaderUtility.WriteMetaHeader();
            item.ExposeData();
            Scribe.saver.FinalizeSaving();

            savedItems.Clear();
            currentItem = null;
            currentlyWorking = false;
            currentPawn = null;
            UnApplyPatches();

            if (item is Pawn pawn) PawnEditor.SavePawnTex(pawn, Path.ChangeExtension(path, ".png"), Rot4.South);

            //Overwrite saved faction with "Random" if setting is active
            if (item is Pawn)
            {
                if (UseRandomFactionOnSave)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);
                    XmlNode factionNode = doc.DocumentElement["faction"];
                    factionNode.InnerText = "Random";
                    doc.Save(path);
                }
               
            }

            callback?.Invoke(item);
        },
        item switch
        {
            Pawn pawn => pawn.LabelShort,
            Map => "Colony",
            StartingThingsManager.StartingPreset => "Colony",
            Faction faction => faction.Name,
            Pawn_AbilityTracker abilities => abilities.pawn.LabelShort,
            Pawn_EquipmentTracker equipment => equipment.pawn.LabelShort,
            Pawn_ApparelTracker apparel => apparel.pawn.LabelShort,
            Pawn_InventoryTracker inventory => inventory.pawn.LabelShort,
            HediffSet hediffs => hediffs.pawn.LabelShort,
            ISaveable saveable => saveable.DefaultFileName(),
            _ => type
        }));
    }

    public static void LoadItem<T>(T item, Action<T> callback = null, Pawn parentPawn = null, Action<T> prepare = null, string typePostfix = null)
        where T : IExposable
    {
        var type = typeof(T).Name;
        Find.WindowStack.Add(new Dialog_PawnEditorFiles_Load(typePostfix.NullOrEmpty() ? type : Path.Combine(type, typePostfix!), path =>
        {
            //Setup loading with random faction
            string beforeSave = "";
            if (item is Pawn)
            {
                if (UseRandomFactionOnSave)
                {

                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);
                    XmlNode factionNode = doc.DocumentElement["faction"];
                    beforeSave = factionNode.InnerText;
                    factionNode.InnerText = "Random";
                    doc.Save(path);
                }
               
            }


            currentlyWorking = true;
            currentItem = item as ILoadReferenceable;
            currentPawn = parentPawn;
            savedItems.Clear();
            loadInfo.Clear();
            var playing = false;
            if (Current.ProgramState == ProgramState.Playing)
            {
                Current.ProgramState = ProgramState.MapInitializing;
                playing = true;
            }

            prepare?.Invoke(item);
            ApplyPatches();
            Scribe.loader.InitLoading(path);
            ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.None, true);
            Scribe.loader.curParent = item;
            item.ExposeData();
            if (item is IExposable exposable)
            {
                Scribe.loader.crossRefs.crossReferencingExposables.Add(exposable);
                Scribe.loader.initer.saveablesToPostLoad.Add(exposable);
            }

            Scribe.loader.FinalizeLoading();
            savedItems.Clear();
            loadInfo.Clear();
            currentlyWorking = false;
            currentItem = null;
            currentPawn = null;
            UnApplyPatches();
            callback?.Invoke(item);
            if (playing)
                Current.ProgramState = ProgramState.Playing;

            PawnEditor.Notify_PointsUsed();

            //cleanup loading with random faction
            if (item is Pawn)
            {

                if (UseRandomFactionOnSave)
                {

                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);
                    XmlNode factionNode = doc.DocumentElement["faction"];
                    factionNode.InnerText = beforeSave;
                    doc.Save(path);
                }

            }
        }));
    }

    private static void ApplyPatches()
    {
        var myType = typeof(SaveLoadUtility);
        PawnEditorMod.Harm.Patch(ReferenceLook.MakeGenericMethod(typeof(ILoadReferenceable)),
            new(myType, nameof(InterceptReferences)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.ExposeData)),
            transpiler: new(myType, nameof(FixFactionWeirdness)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(DebugLoadIDsSavingErrorsChecker), nameof(DebugLoadIDsSavingErrorsChecker.RegisterDeepSaved)),
            postfix: new(myType, nameof(Notify_DeepSaved)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(PostLoadIniter), nameof(PostLoadIniter.RegisterForPostLoadInit)),
            postfix: new(myType, nameof(Notify_DeepSaved)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new[] { typeof(int) }),
            new(myType, nameof(ReassignLoadID)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.ExposeData)),
            new(myType, nameof(AssignCurrentPawn)), new(myType, nameof(ClearCurrentPawn)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(LoadIDsWantedBank), nameof(LoadIDsWantedBank.RegisterLoadIDListReadFromXml),
            new[] { typeof(List<string>), typeof(string), typeof(IExposable) }), new(myType, nameof(InterceptIDList)));
    }

    private static void UnApplyPatches()
    {
        var myType = typeof(SaveLoadUtility);
        PawnEditorMod.Harm.Unpatch(ReferenceLook.MakeGenericMethod(typeof(ILoadReferenceable)),
            AccessTools.Method(myType, nameof(InterceptReferences)));
        PawnEditorMod.Harm.Unpatch(AccessTools.Method(typeof(Thing), nameof(Thing.ExposeData)),
            AccessTools.Method(myType, nameof(FixFactionWeirdness)));
        PawnEditorMod.Harm.Unpatch(AccessTools.Method(typeof(DebugLoadIDsSavingErrorsChecker), nameof(DebugLoadIDsSavingErrorsChecker.RegisterDeepSaved)),
            AccessTools.Method(myType, nameof(Notify_DeepSaved)));
        PawnEditorMod.Harm.Unpatch(AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new[] { typeof(int) }),
            AccessTools.Method(myType, nameof(ReassignLoadID)));
        PawnEditorMod.Harm.Unpatch(AccessTools.Method(typeof(Pawn), nameof(Pawn.ExposeData)), HarmonyPatchType.All, PawnEditorMod.Harm.Id);
        PawnEditorMod.Harm.Unpatch(AccessTools.Method(typeof(LoadIDsWantedBank), nameof(LoadIDsWantedBank.RegisterLoadIDListReadFromXml),
            new[] { typeof(List<string>), typeof(string), typeof(IExposable) }), AccessTools.Method(myType, nameof(InterceptIDList)));
    }
}

public interface ISaveable
{
    string DefaultFileName();
}
