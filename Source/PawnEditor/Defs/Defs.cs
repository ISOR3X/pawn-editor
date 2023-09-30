using System;
using System.Collections.Generic;
using HarmonyLib;
using MonoMod.Utils;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable InconsistentNaming

namespace PawnEditor;

public class TabGroupDef : Def
{
    public List<TabDef> tabs;

    public TabGroupDef()
    {
        label ??= defName;
        description ??= label;
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        tabs ??= new();
        foreach (var def in DefDatabase<TabDef>.AllDefs)
            if (def.tabGroup == this)
                tabs.Add(def);

        foreach (var def in tabs) def.tabGroup = this;
    }
}

[DefOf]
public static class TabGroupDefOf
{
    public static TabGroupDef Humanlike;
    public static TabGroupDef AnimalMech;
    public static TabGroupDef PlayerFaction;
    public static TabGroupDef NPCFaction;

    static TabGroupDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(TabGroupDefOf));
    }
}

public abstract class TabWorker<T>
{
    private static readonly List<TabWorker<T>> tabWorkers = new();
    public abstract void DrawTabContents(Rect rect, T pawn);

    public virtual IEnumerable<SaveLoadItem> GetSaveLoadItems(T pawn)
    {
        yield break;
    }

    public virtual IEnumerable<FloatMenuOption> GetRandomizationOptions(T pawn)
    {
        yield break;
    }

    public virtual void Initialize()
    {
        tabWorkers.Add(this);
    }

    protected virtual void Notify_Open() { }

    public static void Notify_OpenedDialog()
    {
        foreach (var tabWorker in tabWorkers) tabWorker.Notify_Open();
    }
}

public class TabDef : Def
{
    public enum TabType
    {
        Faction, Pawn
    }

    public TabGroupDef tabGroup;

    public TabType type;
    public Type workerClass;


    private Action<Rect, object> drawer;
    private Func<object, IEnumerable<FloatMenuOption>> getRandomizationOptions;
    private Func<object, IEnumerable<SaveLoadItem>> getSaveLoadItems;
    private object worker;

    public TabDef() => description ??= label;

    public void DrawTabContents(Rect rect, Pawn pawn) => drawer?.Invoke(rect, pawn);

    public IEnumerable<SaveLoadItem> GetSaveLoadItems(Pawn pawn) => getSaveLoadItems?.Invoke(pawn);

    public IEnumerable<FloatMenuOption> GetRandomizationOptions(Pawn pawn) => getRandomizationOptions?.Invoke(pawn);
    public void DrawTabContents(Rect rect, Faction faction) => drawer?.Invoke(rect, faction);

    public IEnumerable<SaveLoadItem> GetSaveLoadItems(Faction faction) => getSaveLoadItems?.Invoke(faction);

    public IEnumerable<FloatMenuOption> GetRandomizationOptions(Faction faction) => getRandomizationOptions?.Invoke(faction);

    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(Initialize);
    }

    private void Initialize()
    {
        if (workerClass != null)
        {
            worker = Activator.CreateInstance(workerClass);
            try { AccessTools.Method(workerClass, "Initialize")?.Invoke(worker, Array.Empty<object>()); }
            catch { Log.Error("Failed to initialize tab worker."); }

            try { drawer = AccessTools.Method(workerClass, "DrawTabContents").CreateDelegate<Action<Rect, object>>(worker); }
            catch { Log.Error("Failed to instantiate tab drawer."); }

            try { getSaveLoadItems = AccessTools.Method(workerClass, "GetSaveLoadItems")?.CreateDelegate<Func<object, IEnumerable<SaveLoadItem>>>(worker); }
            catch { Log.Error("Failed to instantiate tab save/loading."); }

            try
            {
                getRandomizationOptions = AccessTools.Method(workerClass, "GetRandomizationOptions")
                   .CreateDelegate<Func<object, IEnumerable<FloatMenuOption>>>(worker);
            }
            catch { Log.Error("Failed to instantiate tab randomization."); }
        }
    }
}

public class WidgetDef : Def
{
    public float defaultHeight;
    public float defaultWidth;
    public Type workerClass;

    private Action<Rect> drawer;

    public WidgetDef() => description ??= label;

    public override void PostLoad()
    {
        base.PostLoad();
        if (workerClass != null)
            try { drawer = AccessTools.Method(workerClass, "DrawTabContents", new[] { typeof(Rect) }).CreateDelegate<Action<Rect>>(); }
            catch { Log.Error("Failed to instantiate tab drawer."); }
    }
}
