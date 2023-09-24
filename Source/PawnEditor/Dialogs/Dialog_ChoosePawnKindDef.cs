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
    private List<PawnKindDef> _filteredPawnKindDefs = new();
    private PawnCategory _pawnCategory;
    private PawnKindDef _selectedPawnKindDef;
    private readonly Action<PawnKindDef> _onSelect;
    private string _filter = "";
    private bool _listDirty;

    private Vector2 _scrollPosition;
    private float _scrollViewHeight;
    private readonly Dictionary<string, string> _truncateCache = new();

    private readonly QuickSearchWidget _quickSearchWidget = new();

    public override Vector2 InitialSize => new(500f, 600f);

    private List<PawnKindDef> PawnKindDefsInOrder()
    {
        if (_listDirty)
        {
            _filteredPawnKindDefs.Clear();
            _filteredPawnKindDefs = _pawnKindDefs
                .Where(pk => pk.LabelCap.rawText.ToLower().Contains(_filter.ToLower()))
                .GroupBy(item => item.LabelCap.rawText)
                .Select(group => group.First())
                .OrderBy(item => item.LabelCap.rawText)
                .ToList();
            _listDirty = false;
        }

        return _filteredPawnKindDefs;
    }

    public Dialog_ChoosePawnKindDef(Action<PawnKindDef> onSelect, PawnCategory pawnCategory)
    {
        _onSelect = onSelect;
        closeOnAccept = false;
        absorbInputAroundWindow = true;
        _pawnCategory = pawnCategory;
    }

    public override void PreOpen()
    {
        base.PreOpen();

        switch (_pawnCategory)
        {
            case PawnCategory.Animals:
            {
                _pawnKindDefs = DefDatabase<PawnKindDef>.AllDefs
                    .Where(pkd => pkd.race.race.Animal && !pkd.race.race.Dryad)
                    .ToList();
                break;
            }
            case PawnCategory.Mechs
                : // Right now mechanoids are found based on their maskPath but this seems a bit weird.
            {
                _pawnKindDefs = DefDatabase<PawnKindDef>.AllDefs
                    .Where(pkd =>
                        pkd.race.race.IsMechanoid &&
                        pkd.lifeStages.LastOrDefault()!.bodyGraphicData.maskPath != null)
                    .ToList();
                break;
            }
            default: // Humans
            {
                _pawnKindDefs = DefDatabase<PawnKindDef>.AllDefs
                    .Where(pk => pk.RaceProps.Humanlike)
                    .ToList();
                break;
            }
        }

        _quickSearchWidget.Reset();
        _listDirty = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect headerRect = inRect.TakeTopPart(Text.LineHeightOf(GameFont.Medium) + 12);
        using (new TextBlock(GameFont.Medium))
        {
            string str = "ChooseStuffForRelic".Translate() + " " + "PawnEditor.PawnKindDef".Translate();
            Widgets.Label(headerRect, str);
        }

        DoBottomButtons(inRect.TakeBottomPart(UIUtility.BottomButtonSize.y));
        inRect.yMax -= 4f;
        DoSelectedLabel(inRect.TakeBottomPart(Text.LineHeightOf(GameFont.Small)));
        inRect.yMax -= 4f;
        DisplayPawnKindDefs(inRect);
    }

    private void DoSelectedLabel(Rect inRect)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            if (_selectedPawnKindDef != null)
            {
                String tooltipTitle = _selectedPawnKindDef.LabelCap;
                String tooltipDesc = _selectedPawnKindDef.race.description;
                TooltipHandler.TipRegion(inRect,
                    $"{tooltipTitle.Colorize(ColoredText.TipSectionTitleColor)}\n\n{tooltipDesc}");
            }

            string labelStr = $"{"StartingPawnsSelected".Translate()}: ";
            string selectedStr = _selectedPawnKindDef?.LabelCap ?? "None";
            float labelWidth = Text.CalcSize(labelStr).x;
            inRect.xMin += 4f;
            Widgets.Label(inRect, labelStr.Colorize(ColoredText.SubtleGrayColor));
            inRect.xMin += labelWidth;
            float height = Text.LineHeightOf(GameFont.Small);
            Widgets.DrawTextureFitted(new Rect(inRect.x, inRect.y, height, height),
                GetTexture(_selectedPawnKindDef), 1f);
            inRect.xMin += height;
            Widgets.Label(inRect, selectedStr);
        }
    }

    private void DoBottomButtons(Rect inRect)
    {
        if (_selectedPawnKindDef != null)
        {
            if (Widgets.ButtonText(
                    new Rect(inRect.xMax - UIUtility.BottomButtonSize.x,
                        inRect.y, UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y),
                    "Accept".Translate()))
                Accept();
            if (!Widgets.ButtonText(
                    new Rect(inRect.x, inRect.y, UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y),
                    "Close".Translate()))
                return;
            Close();
        }
        else
        {
            if (!Widgets.ButtonText(
                    new Rect((float)((inRect.width - (double)UIUtility.BottomButtonSize.x) / 2.0), inRect.y,
                        UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y),
                    "Close".Translate()))
                return;
            Close();
        }
    }

    private void DisplayPawnKindDefs(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        rect = rect.ContractedBy(4f);

        Rect viewRect = new Rect(0.0f, 0.0f, rect.width - 16f, _scrollViewHeight);
        Rect outRect = rect.AtZero();
        outRect.yMax -= (UIUtility.SearchBarHeight + 4f);

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
        _quickSearchWidget.OnGUI(new Rect(rect.x, rect.yMax - UIUtility.SearchBarHeight, rect.width,
            UIUtility.SearchBarHeight));
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
        String tooltipTitle = pawnKindDef.LabelCap;
        String tooltipDesc = pawnKindDef.race.description;
        TooltipHandler.TipRegion(rect, $"{tooltipTitle.Colorize(ColoredText.TipSectionTitleColor)}\n\n{tooltipDesc}");
        Widgets.DrawTextureFitted(new Rect(rect.x, rect.y, 32f, 32f), GetTexture(pawnKindDef), .8f);
        rect.xMin += (32f + 8f);
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(rect, pawnKindDef.LabelCap.rawText.Truncate(rect.width, _truncateCache));

        if (!Widgets.ButtonInvisible(rect))
            return;
        _selectedPawnKindDef = pawnKindDef;
    }

    private Texture GetTexture(PawnKindDef pawnKindDef)
    {
        return _pawnCategory == PawnCategory.Humans || pawnKindDef == null
            ? Widgets.PlaceholderIconTex
            : ContentFinder<Texture2D>.Get(pawnKindDef.lifeStages.LastOrDefault()?.bodyGraphicData.texPath + "_east");
    }

    private void Accept()
    {
        if (_onSelect != null)
        {
            _onSelect(_selectedPawnKindDef);
        }
    }
}