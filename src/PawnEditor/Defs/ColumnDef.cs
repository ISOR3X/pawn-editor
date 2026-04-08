using PawnEditor.Table;
using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class ColumnDef : Def
{
    public static readonly Vector2 IconSize = new(26f, 26f);
    public bool groupable;
    public bool headerAlwaysInteractable;
    [NoTranslate] public string? headerIcon;
    public Vector2 headerIconSize;
    [Unsaved] private Texture2D? headerIconTex;
    [MustTranslate] public string? headerTip;
    public bool showIcon;
    public bool sortable;

    [NoTranslate] public string trackSize = "auto";
    [Unsaved] public TrackSizingFunction ResolvedTrackSize;

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        ResolvedTrackSize = TrackSizingParser.Parse(trackSize);
    }

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
    [Unsaved] private Table.ColumnWorker<Def>? workerInt;

    public Table.ColumnWorker<Def> Worker
    {
        get
        {
            if (workerInt != null) return workerInt;
            var w = (DefColumnWorker)Activator.CreateInstance(workerClass);
            w.Def = this;
            workerInt = w;
            return workerInt;
        }
    }
}

public class ThingColumnDef : ColumnDef
{
    public Type workerClass = typeof(ThingColumnWorker);
    [Unsaved] private Table.ColumnWorker<Thing>? workerInt;

    public Table.ColumnWorker<Thing> Worker
    {
        get
        {
            if (workerInt != null) return workerInt;
            var w = (ThingColumnWorker)Activator.CreateInstance(workerClass);
            w.Def = this;
            workerInt = w;
            return workerInt;
        }
    }
}