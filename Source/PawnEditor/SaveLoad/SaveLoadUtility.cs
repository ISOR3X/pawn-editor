using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static partial class SaveLoadUtility
{
    private static bool currentlyWorking;
    private static ILoadReferenceable currentItem;
    private static Pawn currentPawn;
    private static readonly HashSet<ILoadReferenceable> savedItems = new();

    public static MethodInfo ReferenceLook = AccessTools.FirstMethod(typeof(Scribe_References),
        mi => mi.Name == "Look" && mi.GetParameters().All(p => !p.ParameterType.Name.Contains("WeakReference")));

    public static string BaseSaveFolder => GenFilePaths.FolderUnderSaveData("PawnEditor");

    public static string SaveFolderForItemType(string type) => Path.Combine(BaseSaveFolder, type.CapitalizeFirst());

    public static string FilePathFor(string type, string name) => Path.Combine(SaveFolderForItemType(type), name + ".xml");

    public static int CountWithName(string type, string name) =>
        new DirectoryInfo(SaveFolderForItemType(type)).GetFiles().Count(f => f.Extension == ".xml" && f.Name.StartsWith(name));

    public static void SaveItem<T>(T item, Action<T> callback = null, Pawn parentPawn = null, Action<T> prepare = null) where T : IExposable
    {
        var type = typeof(T).Name;
        Find.WindowStack.Add(new Dialog_PawnEditorFiles_Save(type, path =>
        {
            currentlyWorking = true;
            currentItem = item as ILoadReferenceable;
            currentPawn = parentPawn;
            savedItems.Clear();
            prepare?.Invoke(item);
            ApplyPatches();

            var tempFile = Path.GetTempFileName();
            Scribe.saver.InitSaving(tempFile, type);
            item.ExposeData();
            Scribe.saver.FinalizeSaving();
            File.Delete(tempFile);

            Scribe.saver.InitSaving(path, type);
            ScribeMetaHeaderUtility.WriteMetaHeader();
            item.ExposeData();
            Scribe.saver.FinalizeSaving();

            savedItems.Clear();
            currentItem = null;
            currentlyWorking = false;
            currentPawn = null;
            UnApplyPatches();
            callback?.Invoke(item);
        }, item switch
        {
            Pawn pawn => pawn.LabelShort,
            Map => "Colony",
            StartingThingsManager.StartingPreset => "Colony",
            _ => type
        }));
    }

    public static void LoadItem<T>(T item, Action<T> callback = null, Pawn parentPawn = null, Action<T> prepare = null) where T : IExposable
    {
        var type = typeof(T).Name;
        Find.WindowStack.Add(new Dialog_PawnEditorFiles_Load(type, path =>
        {
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
        }));
    }

    private static void ApplyPatches()
    {
        var myType = typeof(SaveLoadUtility);
        PawnEditorMod.Harm.Patch(ReferenceLook.MakeGenericMethod(typeof(ILoadReferenceable)),
            new HarmonyMethod(myType, nameof(InterceptReferences)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.ExposeData)),
            transpiler: new HarmonyMethod(myType, nameof(FixFactionWeirdness)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(DebugLoadIDsSavingErrorsChecker), nameof(DebugLoadIDsSavingErrorsChecker.RegisterDeepSaved)),
            postfix: new HarmonyMethod(myType, nameof(Notify_DeepSaved)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(PostLoadIniter), nameof(PostLoadIniter.RegisterForPostLoadInit)),
            postfix: new HarmonyMethod(myType, nameof(Notify_DeepSaved)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new[] { typeof(int) }),
            new HarmonyMethod(myType, nameof(ReassignLoadID)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.ExposeData)),
            new HarmonyMethod(myType, nameof(AssignCurrentPawn)), new HarmonyMethod(myType, nameof(ClearCurrentPawn)));
        PawnEditorMod.Harm.Patch(AccessTools.Method(typeof(LoadIDsWantedBank), nameof(LoadIDsWantedBank.RegisterLoadIDListReadFromXml),
            new[] { typeof(List<string>), typeof(string), typeof(IExposable) }), new HarmonyMethod(myType, nameof(InterceptIDList)));
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
