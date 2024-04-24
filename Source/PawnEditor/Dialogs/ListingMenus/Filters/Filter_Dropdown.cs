using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnEditor;
public class Filter_Dropdown<T> : Filter<T>
{
    private readonly Dictionary<string, List<Func<T, bool>>> dropdownOptions;
    private string selectedOption;

    public Filter_Dropdown(string label, Dictionary<string, Func<T, bool>> options, bool enabledByDefault = false, string description = null) : base(
    label, enabledByDefault, description)
    {
        dropdownOptions = new Dictionary<string, List<Func<T, bool>>>();
        foreach (var option in options)
        {
            dropdownOptions[option.Key] = new List<Func<T, bool>> { option.Value };
        }
        selectedOption = options.Keys.FirstOrDefault();
    }

    public Filter_Dropdown(string label, Dictionary<string, List<Func<T, bool>>> options, 
        bool enabledByDefault = false, string description = null) : base(label, enabledByDefault, description)
    {
        dropdownOptions = options;
        selectedOption = options.Keys.FirstOrDefault();
    }

    public static Dictionary<string, List<Func<ThingDef, bool>>> GetDefFilter<D>(Func<ThingDef, D, bool> f, 
        IEnumerable<D> defs = null) where D : Def
    {
        var dict = new Dictionary<string, List<Func<ThingDef, bool>>>();
        defs ??= DefDatabase<D>.AllDefs;
        foreach (var def in defs)
        {
            if (dict.TryGetValue(def.LabelCap, out var func) is false)
            {
                dict[def.LabelCap] = func = new List<Func<ThingDef, bool>> { td => f(td, def) };
            }
            else
            {
                func.Add(td => f(td, def));
            }
        }
        return dict;
    }

    protected override void DrawWidget(Rect rect)
    {
        if (Widgets.ButtonText(rect, selectedOption.Truncate(rect.width - 12f)))
            Find.WindowStack.Add(new FloatMenu(dropdownOptions.Keys.Select(op => new FloatMenuOption(op, () => selectedOption = op)).ToList()));
    }

    protected override bool MatchesInt(T item) => dropdownOptions[selectedOption].Any(x => x(item));
}

