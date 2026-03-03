using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public abstract class TabWorker_Pawn : TabWorker
{
    private float _viewRectHeight = 5000;

    protected TabWorker_Pawn(TabDef def) : base(def)
    {
        if (def.sections.NullOrEmpty()) return;
        Sections = def.sections.Where(d => !d.sticky).ToList();
        StickySections = def.sections.Where(d => d.sticky).ToList();
        InitSections(Sections);
        InitSections(StickySections);
    }

    private List<SectionDef>? Sections { get; }
    private List<SectionDef>? StickySections { get; }


    private static Pawn? SelectedPawn => Window_Editor.GetSelectedPawn();

    private void InitSections(List<SectionDef> sectionList)
    {
        if (sectionList.NullOrEmpty()) return;
        sectionList.SortBy(d => d.priority);
        foreach (var s in sectionList) s.Worker.Tab = this;
    }

    protected override void DoInnerTabContents(ref Rect inRect)
    {
        DoInnerTabContents(ref inRect, SelectedPawn);
    }

    protected virtual void DoInnerTabContents(ref Rect inRect, Pawn? pawn)
    {
        DoTabHeader(ref inRect, pawn);
        inRect.yMin += 8f;
        inRect.yMax -= 8f;

        if (Sections is not { Count: > 0 } || pawn == null) return;

        Widgets.BeginGroup(inRect);
        var contentRect = inRect.AtZero();
        var additionalWidth = inRect.height < _viewRectHeight ? UIUtility.ScrollBarWidth_WithMargin : 0;
        var viewRect = new Rect(contentRect.x, contentRect.y, contentRect.width - additionalWidth, _viewRectHeight);
        Widgets.BeginScrollView(contentRect, ref TabScrollPosition, viewRect);
        foreach (var section in Sections)
        {
            section.Worker.DoSection(ref viewRect, pawn);
            viewRect.yMin += 8f;
        }

        _viewRectHeight = viewRect.yMin;

        Widgets.EndScrollView();
        Widgets.EndGroup();
    }

    /// <summary>
    ///     The tab header by default holds the sticky sections (the ones that do not scroll with the rest of the sections).
    /// </summary>
    protected virtual void DoTabHeader(ref Rect inRect, Pawn? pawn)
    {
        Widgets.BeginGroup(inRect);
        if (StickySections is { Count: > 0 } && pawn != null)
        {
            var contentRect = inRect.AtZero();
            foreach (var section in StickySections) section.Worker.DoSection(ref contentRect, pawn);

            inRect.TakeTopPart(inRect.height - contentRect.height);
        }

        Widgets.EndGroup();
    }

    #region EVENTS

    public override void Notify_ContentChanged()
    {
        base.Notify_ContentChanged();

        if (SelectedPawn == null) return;

        // Update sections
        Def.sections.ForEach(s => s.Worker.OnPawnChanged(SelectedPawn));

        // Update quick actions
        QuickActionUtility.actions.TryGetValue(Def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        QuickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
            QuickActions.Add(attribute.ToFloatMenuOption(method));
    }

    #endregion
}