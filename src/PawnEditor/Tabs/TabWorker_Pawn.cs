using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public abstract class TabWorker_Pawn : TabWorker
{
    public List<SectionDef> sections { get; set; }
    public List<SectionDef> stickySections { get; set; }


    private float viewRectHeight = 5000;


    protected Pawn selectedPawn => Window_Editor.GetSelectedPawn();

    protected TabWorker_Pawn(TabDef def) : base(def)
    {
        if (!def.sections.NullOrEmpty())
        {
            sections = def.sections.Where(d => !d.sticky).ToList();
            stickySections = def.sections.Where(d => d.sticky).ToList();
            InitSections(sections);
            InitSections(stickySections);
        }
    }

    private void InitSections(List<SectionDef> sectionList)
    {
        if (sectionList.NullOrEmpty()) return;
        sectionList.SortBy(d => d.priority);
        foreach (var s in sectionList) s.Worker.tab = this;
    }

    protected override void DoInnerTabContents(ref Rect inRect)
    {
        DoInnerTabContents(ref inRect, selectedPawn);
    }

    protected virtual void DoInnerTabContents(ref Rect inRect, Pawn pawn)
    {
        DoTabHeader(ref inRect, pawn);
        inRect.yMin += 8f;
        inRect.yMax -= 8f;

        if (!sections.NullOrEmpty())
        {
            Widgets.BeginGroup(inRect);
            var contentRect = inRect.AtZero();
            var additionalWidth = inRect.height < viewRectHeight ? UIUtility.scrollBarWidth_WithMargin : 0;
            var viewRect = new Rect(contentRect.x, contentRect.y, contentRect.width - additionalWidth, viewRectHeight);
            Widgets.BeginScrollView(contentRect, ref TabScrollPosition, viewRect);
            foreach (SectionDef section in sections)
            {
                section.Worker.DoSection(ref viewRect, pawn);
                viewRect.yMin += 8f;
            }

            viewRectHeight = viewRect.yMin;

            Widgets.EndScrollView();
            Widgets.EndGroup();
        }
    }

    /// <summary>
    /// The tab header by default holds the sticky sections (the ones that do not scroll with the rest of the sections). 
    /// </summary>
    protected virtual void DoTabHeader(ref Rect inRect, Pawn pawn)
    {
        Widgets.BeginGroup(inRect);
        if (!stickySections.NullOrEmpty())
        {
            var contentRect = inRect.AtZero();
            foreach (SectionDef section in stickySections)
            {
                section.Worker.DoSection(ref contentRect, pawn);
            }

            inRect.TakeTopPart(inRect.height - contentRect.height);
        }

        Widgets.EndGroup();
    }

    #region EVENTS

    public override void Notify_ContentChanged()
    {
        base.Notify_ContentChanged();

        if (selectedPawn == null) return;

        // Update sections
        def.sections?.ForEach(s => s.Worker.OnPawnChanged(selectedPawn));

        // Update quick actions
        QuickActionUtility.actions.TryGetValue(def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        quickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
        {
            quickActions.Add(attribute.ToFloatMenuOption(method));
        }
    }

    #endregion
}