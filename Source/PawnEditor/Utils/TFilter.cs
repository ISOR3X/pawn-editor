using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

// ReSharper disable once InconsistentNaming
public class TFilter<T>
{
    public readonly bool EnabledByDefault;
    public readonly string Label;

    private string _buttonLabel;
    private readonly string _description;

    public Func<T, bool> FilterAction;

    private int _valueCur;
    private readonly int _valueMax;
    private readonly int _valueMin;
    private string _valueBuffer;

    private readonly FilterType _filterType;
    private readonly List<FloatMenuOption> _floatMenuOptions;

    public bool DoInvert;

    private enum FilterType
    {
        IntLimit,
        Dropdown,
        Toggle,
    }

    public TFilter(string label, bool enabledByDefault, Dictionary<FloatMenuOption, Func<T, bool>> options, string description = "") : this(label, enabledByDefault, filterAction: null,
        description)
    {
        _filterType = FilterType.Dropdown;

        foreach (var keyValuePair in options)
        {
            keyValuePair.Key.action += () =>
            {
                _buttonLabel = keyValuePair.Key.Label;
                FilterAction = t =>
                {
                    if (!DoInvert)
                        return keyValuePair.Value(t);
                    return !keyValuePair.Value(t);
                };
            };
        }

        _floatMenuOptions = options.Keys.ToList();
        options.FirstOrDefault().Key.action.Invoke();
    }

    public TFilter(string label, bool enabledByDefault, Func<T, bool> filterAction, string description = "")
    {
        _filterType = FilterType.Toggle;

        Label = label;
        _description = description;
        EnabledByDefault = enabledByDefault;
        
        if (filterAction != null)
            FilterAction = t =>
            {
                if (!DoInvert)
                {
                    return filterAction(t);
                }
                return !filterAction(t);
            };
    }

    public TFilter(string label, bool enabledByDefault, Func<T, int> filterValue, int valueMin, int valueMaxValue, string description = "") : this(label, enabledByDefault, filterAction: null, description)
    {
        _filterType = FilterType.IntLimit;

        _valueMin = valueMin;
        _valueMax = valueMaxValue;
        _valueCur = valueMin;
        
        FilterAction = t =>
        {
            if (!DoInvert)
            {
                return filterValue(t) >= _valueCur;
            }
            return filterValue(t) < _valueCur;
        };
    }

    private void DrawWidget(Rect inRect)
    {
        Rect widgetRect = inRect;
        switch (_filterType)
        {
            case FilterType.Dropdown:
                if (Widgets.ButtonText(widgetRect, _buttonLabel.Truncate(widgetRect.width - 12f)))
                {
                    Find.WindowStack.Add(new FloatMenu(_floatMenuOptions));
                }

                break;
            case FilterType.IntLimit:
                if (Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowLeftHalf))
                {
                    if (_valueCur >= _valueMin + 1)
                    {
                        _valueCur--;
                        _valueBuffer = null;
                    }
                    else
                    {
                        Messages.Message(new Message("Reached limit of input", MessageTypeDefOf.RejectInput));
                    }
                }

                if (Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRightHalf))
                {
                    if (_valueCur <= _valueMax - 1)
                    {
                        _valueCur++;
                        _valueBuffer = null;
                    }
                    else
                    {
                        Messages.Message(new Message("Reached limit of input", MessageTypeDefOf.RejectInput));
                    }
                }

                Widgets.TextFieldNumeric(inRect.ContractedBy(0f, 4f), ref _valueCur, ref _valueBuffer, _valueMin, _valueMax);

                break;
        }
    }

    public void DrawFilter(ref Rect inRect, ref List<TFilter<T>> filtersToRemove)
    {
        float rowHeight = UIUtility.RegularButtonHeight * 2 + 8;
        if (_filterType == FilterType.Toggle) 
        {
            rowHeight -= (UIUtility.RegularButtonHeight + 4);
        }

        var filterRect = inRect.TakeTopPart(rowHeight);
        
        // Grey background
        GUI.color = CharacterCardUtility.StackElementBackground;
        GUI.DrawTexture(filterRect, BaseContent.WhiteTex);
        GUI.color = Color.white;
        filterRect = filterRect.ContractedBy(6f);

        // Filter widget
        DrawWidget(filterRect.TakeBottomPart(UIUtility.RegularButtonHeight));
        filterRect.yMax -= 4f;

        // Filter info
        Rect topRowRect = filterRect.TakeTopPart(Text.LineHeightOf(GameFont.Small));
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Rect buttonRect = topRowRect.TakeRightPart(topRowRect.height);
            if (Widgets.ButtonImage(buttonRect, TexButton.DeleteX))
            {
                DoInvert = false;
                filtersToRemove.Add(this);
            }

            buttonRect = topRowRect.TakeRightPart(topRowRect.height).ExpandedBy(4f);
            buttonRect.x -= 4f;

            TooltipHandler.TipRegion(buttonRect, "PawnEditor.InvertFilter".Translate());
            
            Texture2D filter = DoInvert ? TexPawnEditor.InvertFilterActive : TexPawnEditor.InvertFilter;
            if (Widgets.ButtonImage(buttonRect, filter))
            {
                DoInvert = !DoInvert;
            }

            Widgets.Label(topRowRect, Label);
            if (Mouse.IsOver(topRowRect) && _description != "")
            {
                TooltipHandler.TipRegion(topRowRect, $"{Label.Colorize(ColoredText.TipSectionTitleColor)}\n\n{_description}");
            }
        }
    }
}