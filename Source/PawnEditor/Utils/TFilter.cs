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
    private readonly Dictionary<string, string> _truncatedStringCache = new();

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
        Rect widgetRect = inRect.ContractedBy(0f, 4f);
        switch (_filterType)
        {
            case FilterType.Dropdown:
                if (Widgets.ButtonText(widgetRect, _buttonLabel.Truncate(inRect.width - 12f, _truncatedStringCache)))
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

    public void DrawFilter(Rect inRect, ref List<TFilter<T>> filtersToRemove)
    {
        // Grey background
        Rect s = inRect;
        GUI.color = CharacterCardUtility.StackElementBackground;
        GUI.DrawTexture(inRect, BaseContent.WhiteTex);
        GUI.color = Color.white;

        // Filter label
        inRect.xMin += 8f;
        // Label can use whole width if toggle since that has no interaction widget.
        Rect labelRect = this._filterType == FilterType.Toggle ? inRect : inRect.LeftHalf();
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Color c = DoInvert ? Color.gray : Color.white;
            Widgets.Label(labelRect, Label.Colorize(c));

            labelRect.xMin += s.height;
            if (Widgets.ButtonInvisible(labelRect))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> { new("PawnEditor.InvertFilter".Translate(), () => { DoInvert = !DoInvert; }) }));
            }
        }

        if (Mouse.IsOver(labelRect) && _description != "")
        {
            TooltipHandler.TipRegion(labelRect, $"{Label.Colorize(ColoredText.TipSectionTitleColor)}\n\n{_description}");
        }

        // Filter widget
        inRect.xMax -= 4f;
        DrawWidget(inRect.RightHalf());

        // Deletion icon
        if (Mouse.IsOver(inRect))
        {
            if (Widgets.ButtonImage(inRect.LeftPartPixels(s.height).ContractedBy(4), TexButton.DeleteX))
            {
                DoInvert = false;
                filtersToRemove.Add(this);
            }
        }
    }
}