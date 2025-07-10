using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static PawnEditor.TabDef;

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
    private Action<Rect, Faction> drawerFaction;


    private Action<Rect, Pawn> drawerPawn;
    private Func<Faction, IEnumerable<FloatMenuOption>> getRandomizationOptionsFaction;
    private Func<Pawn, IEnumerable<FloatMenuOption>> getRandomizationOptionsPawn;
    private Func<Faction, IEnumerable<SaveLoadItem>> getSaveLoadItemsFaction;
    private Func<Pawn, IEnumerable<SaveLoadItem>> getSaveLoadItemsPawn;
    private object worker;

    public TabDef() => description ??= label;

    public void DrawTabContents(Rect rect, Pawn pawn) => drawerPawn?.Invoke(rect, pawn);

    public IEnumerable<SaveLoadItem> GetSaveLoadItems(Pawn pawn) => getSaveLoadItemsPawn?.Invoke(pawn);

    public IEnumerable<FloatMenuOption> GetRandomizationOptions(Pawn pawn) => getRandomizationOptionsPawn?.Invoke(pawn);
    public void DrawTabContents(Rect rect, Faction faction) => drawerFaction?.Invoke(rect, faction);

    public IEnumerable<SaveLoadItem> GetSaveLoadItems(Faction faction) => getSaveLoadItemsFaction?.Invoke(faction);

    public IEnumerable<FloatMenuOption> GetRandomizationOptions(Faction faction) => getRandomizationOptionsFaction?.Invoke(faction);

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
            var argType = type switch
            {
                TabType.Faction => typeof(Faction),
                TabType.Pawn => typeof(Pawn),
                _ => throw new ArgumentOutOfRangeException()
            };
            try { AccessTools.Method(workerClass, "Initialize", Type.EmptyTypes)?.Invoke(worker, Array.Empty<object>()); }
            catch (Exception e) { Log.Error($"Failed to initialize tab worker: {e}"); }

            try
            {
                if (type == TabType.Faction)
                    drawerFaction = AccessTools.Method(workerClass, "DrawTabContents", new[] { typeof(Rect), argType })
                       .CreateDelegate<Action<Rect, Faction>>(worker);
                else if (type == TabType.Pawn)
                    drawerPawn = AccessTools.Method(workerClass, "DrawTabContents", new[] { typeof(Rect), argType })
                       .CreateDelegate<Action<Rect, Pawn>>(worker);
            }
            catch (Exception e) { Log.Error($"Failed to instantiate tab drawer: {e}"); }

            try
            {
                if (type == TabType.Faction)
                    getSaveLoadItemsFaction = AccessTools.Method(workerClass, "GetSaveLoadItems", new[] { argType })
                      ?.CreateDelegate<Func<Faction, IEnumerable<SaveLoadItem>>>(worker);
                else if (type == TabType.Pawn)
                    getSaveLoadItemsPawn = AccessTools.Method(workerClass, "GetSaveLoadItems", new[] { argType })
                      ?.CreateDelegate<Func<Pawn, IEnumerable<SaveLoadItem>>>(worker);
            }
            catch (Exception e) { Log.Error($"Failed to instantiate tab save/loading: {e}"); }

            try
            {
                if (type == TabType.Faction)
                    getRandomizationOptionsFaction = AccessTools.Method(workerClass, "GetRandomizationOptions", new[] { argType })
                      ?.CreateDelegate<Func<Faction, IEnumerable<FloatMenuOption>>>(worker);
                else if (type == TabType.Pawn)
                    getRandomizationOptionsPawn = AccessTools.Method(workerClass, "GetRandomizationOptions", new[] { argType })
                      ?.CreateDelegate<Func<Pawn, IEnumerable<FloatMenuOption>>>(worker);
            }
            catch (Exception e) { Log.Error($"Failed to instantiate tab randomization: {e}"); }
        }
    }
}

public class WidgetDef : Def
{
    public float defaultHeight;
    public float defaultWidth;
    public TabType type;
    public Type workerClass;
    private Action<Rect, object> drawer;


    private Func<object, float> getHeight;
    private Func<object, float> getWidth;
    private Func<object, bool> showOn;
    private object worker;

    public WidgetDef()
    {
        label ??= defName;
        description ??= label;
    }

    public float GetWidth(Pawn pawn) => getWidth?.Invoke(pawn) ?? defaultWidth;
    public float GetWidth(Faction faction) => getWidth?.Invoke(faction) ?? defaultWidth;
    public float GetHeight(Pawn pawn) => getHeight?.Invoke(pawn) ?? defaultWidth;
    public float GetHeight(Faction faction) => getHeight?.Invoke(faction) ?? defaultWidth;
    public bool ShowOn(Pawn pawn) => showOn?.Invoke(pawn) ?? true;
    public bool ShowOn(Faction faction) => showOn?.Invoke(faction) ?? true;

    public void Draw(Rect inRect, Pawn pawn)
    {
        drawer(inRect, pawn);
    }

    public void Draw(Rect inRect, Faction faction)
    {
        drawer(inRect, faction);
    }

    public override void PostLoad()
    {
        base.PostLoad();
        if (workerClass != null)
        {
            worker = AccessTools.CreateInstance(workerClass);
            var argType = type switch
            {
                TabType.Faction => typeof(Faction),
                TabType.Pawn => typeof(Pawn),
                _ => throw new ArgumentOutOfRangeException()
            };
            try { AccessTools.Method(workerClass, "Initialize", new[] { typeof(WidgetDef) })?.Invoke(worker, new object[] { this }); }
            catch (Exception e) { Log.Error($"Failed to initialize widget worker: {e}"); }

            try { drawer = AccessTools.Method(workerClass, "Draw", new[] { typeof(Rect), argType }).CreateDelegate<Action<Rect, object>>(worker); }
            catch (Exception e) { Log.Error($"Failed to instantiate widget drawer: {e}"); }

            try { getHeight = AccessTools.Method(workerClass, "GetHeight", new[] { argType })?.CreateDelegate<Func<object, float>>(worker); }
            catch (Exception e) { Log.Error($"Failed to instantiate height getter: {e}"); }

            try { getWidth = AccessTools.Method(workerClass, "GetWidth", new[] { argType })?.CreateDelegate<Func<object, float>>(worker); }
            catch (Exception e) { Log.Error($"Failed to instantiate width getter: {e}"); }

            try { showOn = AccessTools.Method(workerClass, "ShowOn", new[] { argType })?.CreateDelegate<Func<object, bool>>(worker); }
            catch (Exception e) { Log.Error($"Failed to instantiate predicate: {e}"); }
        }
    }
}

public abstract class WidgetWorker<T>
{
    protected WidgetDef def;

    public virtual void Initialize(WidgetDef def)
    {
        this.def = def;
    }

    public abstract void Draw(Rect inRect, T pawn);

    public virtual float GetWidth(T pawn) => def.defaultWidth;
    public virtual float GetHeight(T pawn) => def.defaultHeight;

    public virtual bool ShowOn(T pawn) => true;
}

public class WidgetWorker_Blank : WidgetWorker<Pawn>
{
    public override void Draw(Rect inRect, Pawn pawn)
    {
        Widgets.DrawBoxSolid(inRect, pawn.story?.favoriteColor.color ?? Color.cyan);
    }
}
