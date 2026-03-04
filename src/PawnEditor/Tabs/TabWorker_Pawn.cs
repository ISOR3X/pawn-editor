using System.Collections.Generic;
using System.Linq;
using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public abstract class TabWorker_Pawn(TabDef def) : TabWorker(def)
{
    private float _viewRectHeight = 5000;
    private List<List<SectionRef>>? _layout;
    // private List<List<SectionRef>>? _stickyLayout;

    private static Pawn? SelectedPawn => Window_Editor.GetSelectedPawn();
    
    private static List<List<T>> GetLayout<T>(ref List<List<T>>? cache, List<T> sections) where T : IFlexItem
        => cache ??= FlexLayout.ComputeLayout(sections);

    protected override void DoInnerTabContents(ref Rect inRect)
    {
        DoInnerTabContents(ref inRect, SelectedPawn);
    }

    protected virtual void DoInnerTabContents(ref Rect inRect, Pawn? pawn)
    {
        DoTabHeader(ref inRect, pawn);
        inRect.yMin += 8f;
        inRect.yMax -= 8f;

        if (Def.sections is not { Count: > 0 } || pawn == null) return;

        Widgets.BeginGroup(inRect);
        var contentRect = inRect.AtZero();
        var additionalWidth = inRect.height < _viewRectHeight ? UIUtility.ScrollBarWidth_WithMargin : 0;
        var viewRect = new Rect(contentRect.x, contentRect.y, contentRect.width - additionalWidth, _viewRectHeight);
        Widgets.BeginScrollView(contentRect, ref TabScrollPosition, viewRect);

        var curY = viewRect.y;
        foreach (var row in GetLayout(ref _layout, Def.sections))
        {
            var resolvedWidths = FlexLayout.ResolveWidths(row, viewRect.width, FlexLayout.ColumnGap);
            var rowHeight = row.Select((s, i) => s.section.Worker.MeasureHeight(pawn, viewRect.width * resolvedWidths[i])).Max();
            if (rowHeight <= 0f) continue;

            var curX = viewRect.x;
            for (var i = 0; i < row.Count; i++)
            {
                var sectionRect = new Rect(curX, curY, resolvedWidths[i], rowHeight);
                Widgets.DrawRectFast(sectionRect, new Color(1, 1, 1, 0.1f));
                row[i].section.Worker.DoSection(ref sectionRect, pawn);
                curX += resolvedWidths[i] + FlexLayout.ColumnGap; // add a gap between sections
            }

            curY += rowHeight + FlexLayout.RowGap;
        }

        _viewRectHeight = curY;

        Widgets.EndScrollView();
        Widgets.EndGroup();
    }

    protected virtual void DoTabHeader(ref Rect inRect, Pawn? pawn)
    {
        // Widgets.BeginGroup(inRect);
        // if (Def.stickySections is { Count: > 0 } && pawn != null)
        // {
        //     var contentRect = inRect.AtZero();
        //     var curY = contentRect.y;
        //
        //     foreach (var row in GetLayout(ref _stickyLayout, Def.stickySections))
        //     {
        //         var resolvedWidths = FlexLayout.ResolveWidths(row);
        //         var rowHeight = row.Select((s, i) => s.section.Worker.MeasureHeight(pawn, contentRect.width * resolvedWidths[i])).Max();
        //         if (rowHeight <= 0f) continue;
        //
        //         var curX = contentRect.x;
        //         for (var i = 0; i < row.Count; i++)
        //         {
        //             var sectionWidth = contentRect.width * resolvedWidths[i];
        //             var sectionRect = new Rect(curX, curY, sectionWidth, rowHeight);
        //             row[i].section.Worker.DoSection(ref sectionRect, pawn);
        //             curX += sectionWidth;
        //         }
        //
        //         curY += rowHeight + FlexLayout.RowGap;
        //     }
        //
        //     inRect.yMin += curY;
        // }
        //
        // Widgets.EndGroup();
    }

    #region EVENTS

    public override void Notify_ContentChanged()
    {
        base.Notify_ContentChanged();

        if (SelectedPawn == null) return;

        Def.sections.ForEach(s => s.section.Worker.OnThingChanged(SelectedPawn));
        

        QuickActionUtility.actions.TryGetValue(Def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        QuickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
            QuickActions.Add(attribute.ToFloatMenuOption(method));
        
        Def.sections?.ForEach(s => s.section.Worker.InvalidateHeight());
        Log.Message("NOTIFY");
    }

    #endregion
}