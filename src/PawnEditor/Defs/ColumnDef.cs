using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ColumnDef : Def
{
    public Type workerClass = typeof(ColumnWorker);
    public bool sortable;
    public bool ignoreWhenCalculatingOptimalTableSize;
    [NoTranslate] public string headerIcon;
    public Vector2 headerIconSize;
    [MustTranslate] public string headerTip;
    public bool headerAlwaysInteractable;
    public bool groupable;
    public int gap;
    public bool showIcon;
    public bool iconBackground;
    public bool useLabelShort;
    public int widthPriority = 100;
    public int width = 26;
    [Unsaved] private ColumnWorker workerInt;
    [Unsaved] private Texture2D headerIconTex;
    public static readonly Vector2 IconSize = new(26f, 26f);

    public ColumnWorker Worker
    {
        get
        {
            if (workerInt == null)
            {
                workerInt = (ColumnWorker)Activator.CreateInstance(workerClass);
                workerInt.def = this;
            }

            return workerInt;
        }
    }

    public Texture2D HeaderIcon
    {
        get
        {
            if (headerIconTex == null && !headerIcon.NullOrEmpty())
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