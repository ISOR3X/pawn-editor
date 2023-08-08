using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
// ReSharper disable once InconsistentNaming
public class Dialog_ChoosePawnKindDef : Window
{
    private List<PawnKindDef> _pawnKindDefs = new();
    private PawnKindDef _selectedPawnKindDef;
    private readonly Action<PawnKindDef> _onSelect;
    private string _filter = "";
    private bool _listDirty;

    private Vector2 _scrollPosition;
    private float _scrollViewHeight;
    private const float SearchBarSize = 30f;
    private static readonly Vector2 ButSize = new(150f, 38f);
    private readonly Dictionary<string, string> _truncateCache = new();

    private readonly QuickSearchWidget _quickSearchWidget = new();

    public override Vector2 InitialSize => new(500f, 600f);

    public Dialog_ChoosePawnKindDef(Action<PawnKindDef> onSelect)
    {
        _onSelect = onSelect;
        closeOnAccept = false;
        absorbInputAroundWindow = true;
    }

    private List<PawnKindDef> PawnKindDefsInOrder()
    {
        if (_listDirty)
        {
            _pawnKindDefs.Clear();
            _pawnKindDefs = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(pk => pk.RaceProps.Humanlike && pk.LabelCap.rawText.ToLower().Contains(_filter.ToLower()))
                .GroupBy(item => item.LabelCap.rawText)
                .Select(group => group.First())
                .OrderBy(item => item.LabelCap.rawText)
                .ToList();
            _listDirty = false;
        }

        return _pawnKindDefs;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        _quickSearchWidget.Reset();
        _listDirty = true;
    }

    public override void DoWindowContents(Rect rect)
    {
        Rect rect1 = rect;
        rect1.yMax -= ButSize.y + 4f;
        float headerHeight = DrawHeader(rect);

        rect1.yMin += headerHeight;
        DisplayPawnKindDefs(rect1);

        Rect rect2 = rect;
        rect2.yMin = rect2.yMax - ButSize.y;
        if (_selectedPawnKindDef != null)
        {
            if (Widgets.ButtonText(
                    new Rect(rect2.xMax - ButSize.x, rect2.y, ButSize.x, ButSize.y), 
                    "Accept".Translate()))
                Accept();
            if (!Widgets.ButtonText(
                    new Rect(rect2.x, rect2.y, ButSize.x, ButSize.y), 
                    "Close".Translate()))
                return;
            Close();
        }
        else
        {
            if (!Widgets.ButtonText(
                    new Rect((float)((rect2.width - (double)ButSize.x) / 2.0), rect2.y, ButSize.x, ButSize.y),
                    "Close".Translate()))
                return;
            Close();
        }
    }

    private float DrawHeader(Rect rect)
    {
        Text.Font = GameFont.Medium;
        string str = "PawnEditor.SelectPawnKindDef".Translate();
        float height = Text.CalcHeight(str, rect.width);
        Widgets.Label(rect, str);
        Text.Font = GameFont.Small;
        return height + 10f;
    }

    private void DisplayPawnKindDefs(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        rect = rect.ContractedBy(4f);

        Rect viewRect = new Rect(0.0f, 0.0f, rect.width - 16f, _scrollViewHeight);
        Rect outRect = rect.AtZero();
        outRect.yMax -= (SearchBarSize + 4f);

        GUI.BeginGroup(rect);
        float y = 0.0f;
        Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
        for (int index = 0; index < PawnKindDefsInOrder().Count; ++index)
        {
            float width = outRect.width;
            if (_scrollViewHeight > (double)outRect.height)
                width -= 16f;
            DrawRow(new Rect(0.0f, y, width, 32f), index);
            y += 32f;
        }

        if (Event.current.type == EventType.Layout)
            _scrollViewHeight = y;
        Widgets.EndScrollView();

        GUI.EndGroup();
        _quickSearchWidget.OnGUI(new Rect(rect.x, rect.yMax - SearchBarSize, rect.width, SearchBarSize));
        if (_quickSearchWidget.filter.Text != _filter)
            _listDirty = true;
        _filter = _quickSearchWidget.filter.Text;
        _listDirty = true;
    }

    private void DrawRow(Rect rect, int index)
    {
        PawnKindDef pawnKindDef = PawnKindDefsInOrder()[index];
        if (index % 2 == 1)
            Widgets.DrawLightHighlight(rect);
        if (Mouse.IsOver(rect))
            Widgets.DrawHighlight(rect);
        if (_selectedPawnKindDef == pawnKindDef)
            Widgets.DrawHighlightSelected(rect);

        rect.xMin += 4f;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rect, pawnKindDef.LabelCap.rawText.Truncate(rect.width, _truncateCache));
        Text.Anchor = TextAnchor.UpperLeft;

        if (!Widgets.ButtonInvisible(rect))
            return;
        _selectedPawnKindDef = pawnKindDef;
    }

    private void Accept()
    {
        if (_onSelect != null)
        {
            _onSelect(_selectedPawnKindDef);
        }
    }
}