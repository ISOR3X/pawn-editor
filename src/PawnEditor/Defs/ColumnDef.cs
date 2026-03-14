using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class ColumnDef : Def
{
    public static readonly Vector2 IconSize = new(26f, 26f);
    public int gap;
    public bool groupable;
    public bool headerAlwaysInteractable;
    [NoTranslate] public string? headerIcon;
    public Vector2 headerIconSize;
    [Unsaved] private Texture2D? headerIconTex;
    [MustTranslate] public string? headerTip;
    public bool iconBackground;
    public bool ignoreWhenCalculatingOptimalTableSize;
    public bool showIcon;
    public bool sortable;
    public bool useLabelShort;
    public int width = 26;
    public int widthPriority = 100;

    public Texture2D? HeaderIcon
    {
        get
        {
            if (!headerIcon.NullOrEmpty())
                headerIconTex = ContentFinder<Texture2D>.Get(headerIcon);
            return headerIconTex;
        }
    }

    public Vector2 HeaderIconSize
    {
        get
        {
            if (headerIconSize != new Vector2())
                return headerIconSize;
            return HeaderIcon != null ? IconSize : Vector2.zero;
        }
    }

    public bool HeaderInteractable => sortable || !headerTip.NullOrEmpty() || headerAlwaysInteractable;
}

public class DefColumnDef : ColumnDef
{
    public Type workerClass = typeof(DefColumnWorker);
    [Unsaved] private ColumnWorker<Def>? workerInt;

    public ColumnWorker<Def> Worker
    {
        get
        {
            if (workerInt != null) return workerInt;
            workerInt = (ColumnWorker<Def>)Activator.CreateInstance(workerClass);
            workerInt.Def = this;
            return workerInt;
        }
    }
}

public class ThingColumnDef : ColumnDef
{
    public Type workerClass = typeof(ThingColumnWorker);
    [Unsaved] private ColumnWorker<Thing>? workerInt;

    public ColumnWorker<Thing> Worker
    {
        get
        {
            if (workerInt != null) return workerInt;
            workerInt = (ColumnWorker<Thing>)Activator.CreateInstance(workerClass);
            workerInt.Def = this;
            return workerInt;
        }
    }
}