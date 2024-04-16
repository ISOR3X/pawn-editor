using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ListingMenu_Trait : ListingMenu<ListingMenu_Trait.TraitInfo>
{
    private static readonly List<TraitInfo> items;
    private static readonly Func<TraitInfo, string> labelGetter = t => t?.TraitDegreeData.LabelCap;
    private static readonly Func<TraitInfo, Pawn, string> descGetter = (t, p) => t?.TraitDegreeData.description.Formatted(p.Named("PAWN")).AdjustedFor(p);
    private static readonly List<Filter<TraitInfo>> filters;

    static ListingMenu_Trait()
    {
        items = DefDatabase<TraitDef>.AllDefs
           .SelectMany(traitDef =>
                traitDef.degreeDatas.Select(degree => new TraitInfo(traitDef, degree)))
           .ToList();
        filters = GetFilters();
    }

    public ListingMenu_Trait(Pawn pawn) : base(items, labelGetter, b => TryAdd(b, pawn),
        "PawnEditor.Choose".Translate() + " " + "Trait".Translate().ToLower(),
        b => descGetter(b, pawn), null, filters, pawn) { }

    private static AddResult TryAdd(TraitInfo traitInfo, Pawn pawn)
    {
        if (pawn.kindDef.disallowedTraits.NotNullAndContains(traitInfo.Trait.def)
         || pawn.kindDef.disallowedTraitsWithDegree.NotNullAndAny(t => t.def == traitInfo.Trait.def && t.degree == traitInfo.TraitDegreeData.degree)
         || (pawn.kindDef.requiredWorkTags != WorkTags.None
          && (traitInfo.Trait.def.disabledWorkTags & pawn.kindDef.requiredWorkTags) != WorkTags.None))
            return "PawnEditor.TraitDisallowedByKind".Translate(traitInfo.Trait.Label, pawn.kindDef.labelPlural);

        if (pawn.story.traits.allTraits.FirstOrDefault(tr => traitInfo.Trait.def.ConflictsWith(tr)) is { } trait)
            return "PawnEditor.TraitConflicts".Translate(traitInfo.Trait.Label, trait.Label);

        if (pawn.WorkTagIsDisabled(traitInfo.Trait.def.requiredWorkTags))
            return "PawnEditor.TraitWorkDisabled".Translate(pawn.Name.ToStringShort, traitInfo.Trait.def.requiredWorkTags.LabelTranslated(),
                traitInfo.Trait.Label);

        if (traitInfo.Trait.def.requiredWorkTypes?.FirstOrDefault(pawn.WorkTypeIsDisabled) is { } workType)
            return "PawnEditor.TraitWorkDisabled".Translate(pawn.Name.ToStringShort, workType.label, traitInfo.Trait.Label);

        if (HARCompat.Active && HARCompat.EnforceRestrictions && !HARCompat.CanGetTrait(traitInfo, pawn))
            return "PawnEditor.HARRestrictionViolated".Translate(pawn.Named("PAWN"), pawn.def.label.Named("RACE"), "PawnEditor.Wear".Named("VERB"),
                traitInfo.Trait.Label.Named("ITEM"));

        pawn.story.traits.GainTrait(new(traitInfo.Trait.def, traitInfo.TraitDegreeData.degree));
        PawnEditor.Notify_PointsUsed();
        return true;
    }

    private static List<Filter<TraitInfo>> GetFilters()
    {
        var list = new List<Filter<TraitInfo>>();

        var modSourceDict =
            LoadedModManager.runningMods
               .Where(m => m.AllDefs.OfType<TraitDef>().Any())
               .ToDictionary<ModContentPack, string, Func<TraitInfo, bool>>(m => m.Name, m => traitInfo =>
                    traitInfo.Trait.def.modContentPack?.Name == m.Name);
        list.Add(new Filter_Dropdown<TraitInfo>("Source".Translate(), modSourceDict, false, "PawnEditor.SourceDesc".Translate()));

        return list;
    }

    public class TraitInfo
    {
        public readonly Trait Trait;
        public readonly TraitDegreeData TraitDegreeData;

        public TraitInfo(TraitDef traitDef, TraitDegreeData degree)
        {
            TraitDegreeData = degree;
            Trait = new(traitDef, degree.degree);
        }
    }
}
