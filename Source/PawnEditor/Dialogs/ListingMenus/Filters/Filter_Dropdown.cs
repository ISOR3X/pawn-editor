using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class Filter_Dropdown<T> : Filter<T>
{
    private readonly Dictionary<string, Func<T, bool>> dropdownOptions;
    private string selectedOption;

    public Filter_Dropdown(string label, Dictionary<string, Func<T, bool>> options, bool enabledByDefault = false, string description = null) : base(
        label, enabledByDefault, description)
    {
        dropdownOptions = options;
        selectedOption = options.Keys.FirstOrDefault();
    }

    protected override void DrawWidget(Rect rect)
    {
        if (Widgets.ButtonText(rect, selectedOption.Truncate(rect.width - 12f)))
            Find.WindowStack.Add(new FloatMenu(dropdownOptions.Keys.Select(op => new FloatMenuOption(op, () => selectedOption = op)).ToList()));
    }

    protected override bool MatchesInt(T item) => dropdownOptions[selectedOption](item);
}
