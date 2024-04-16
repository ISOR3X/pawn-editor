using System;
using System.Linq;
using Verse;

namespace PawnEditor;

public class Filter_ModSource<T> : Filter_Dropdown<T> where T : Def
{
    public Filter_ModSource(bool enabledByDefault = false) : base(
        "Source".Translate(),
        LoadedModManager.runningMods
           .Where(m => m.AllDefs.OfType<T>().Any())
           .ToDictionary<ModContentPack, string, Func<T, bool>>(m => m.Name, m => d =>
                d.modContentPack?.Name == m.Name), enabledByDefault, "PawnEditor.SourceDesc".Translate()) { }
}
