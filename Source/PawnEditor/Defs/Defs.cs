using System;
using System.Collections.Generic;
using HarmonyLib;
using MonoMod.Utils;
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
        tabs ??= new List<TabDef>();
        foreach (var def in DefDatabase<TabDef>.AllDefs)
            if (def.tabGroup == this)
                tabs.Add(def);

        foreach (var def in tabs) def.tabGroup = this;
    }
}

public abstract class TabWorker
{
    public abstract void DrawTabContents(Rect rect, Pawn pawn);

    public virtual IEnumerable<SaveLoadItem> GetSaveLoadItems(Pawn pawn)
    {
        yield break;
    }

    public virtual IEnumerable<FloatMenuOption> GetRandomizationOptions(Pawn pawn)
    {
        yield break;
    }
}

public class TabDef : Def
{
    public TabGroupDef tabGroup;
    public Type workerClass;


    private Action<Rect, Pawn> drawer;
    private Func<Pawn, IEnumerable<FloatMenuOption>> getRandomizationOptions;
    private Func<Pawn, IEnumerable<SaveLoadItem>> getSaveLoadItems;

    public TabDef() => description ??= label;

    public void DrawTabContents(Rect rect, Pawn pawn) => drawer?.Invoke(rect, pawn);

    public IEnumerable<SaveLoadItem> GetSaveLoadItems(Pawn pawn) => getSaveLoadItems?.Invoke(pawn);

    public IEnumerable<FloatMenuOption> GetRandomizationOptions(Pawn pawn) => getRandomizationOptions?.Invoke(pawn);

    public override void PostLoad()
    {
        base.PostLoad();
        if (workerClass != null)
        {
            try { drawer = AccessTools.Method(workerClass, "DrawTabContents").CreateDelegate<Action<Rect, Pawn>>(); }
            catch { Log.Error("Failed to instantiate tab drawer."); }

            try { getSaveLoadItems = AccessTools.Method(workerClass, "GetSaveLoadItems")?.CreateDelegate<Func<Pawn, IEnumerable<SaveLoadItem>>>(); }
            catch { Log.Error("Failed to instantiate tab save/loading."); }

            try
            {
                getRandomizationOptions = AccessTools.Method(workerClass, "GetRandomizationOptions").CreateDelegate<Func<Pawn, IEnumerable<FloatMenuOption>>>();
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
