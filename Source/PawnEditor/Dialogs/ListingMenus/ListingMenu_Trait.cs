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
    private static readonly Action<TraitInfo, Pawn> action = TryAdd;
    private static readonly List<Filter<TraitInfo>> filters;

    static ListingMenu_Trait()
    {
        items = DefDatabase<TraitDef>.AllDefs
           .SelectMany(traitDef =>
                traitDef.degreeDatas.Select(degree => new TraitInfo(traitDef, degree)))
           .ToList();
        filters = GetFilters();
    }

    public ListingMenu_Trait(Pawn pawn) : base(items, labelGetter, b => action(b, pawn),
        "ChooseStuffForRelic".Translate() + " " + "Trait".Translate().ToLower(),
        b => descGetter(b, pawn), null, filters, pawn) { }

    private static void TryAdd(TraitInfo traitInfo, Pawn pawn)
    {
        if (pawn.kindDef.disallowedTraits.NotNullAndContains(traitInfo.Trait.def)
         || pawn.kindDef.disallowedTraitsWithDegree.NotNullAndAny(t => t.def == traitInfo.Trait.def && t.degree == traitInfo.TraitDegreeData.degree)
         || (pawn.kindDef.requiredWorkTags != WorkTags.None
          && (traitInfo.Trait.def.disabledWorkTags & pawn.kindDef.requiredWorkTags) != WorkTags.None))
        {
            Messages.Message("PawnEditor.TraitDisallowedByKind".Translate(traitInfo.Trait.Label, pawn.kindDef.labelPlural), MessageTypeDefOf.RejectInput,
                false);
            return;
        }

        if (pawn.story.traits.allTraits.FirstOrDefault(tr => traitInfo.Trait.def.ConflictsWith(tr)) is { } trait)
        {
            Messages.Message("PawnEditor.TraitConflicts".Translate(traitInfo.Trait.Label, trait.Label), MessageTypeDefOf.RejectInput, false);
            return;
        }

        if (pawn.WorkTagIsDisabled(traitInfo.Trait.def.requiredWorkTags))
        {
            Messages.Message(
                "PawnEditor.TraitWorkDisabled".Translate(pawn.Name.ToStringShort, traitInfo.Trait.def.requiredWorkTags.LabelTranslated(),
                    traitInfo.Trait.Label), MessageTypeDefOf.RejectInput, false);
            return;
        }

        if (traitInfo.Trait.def.requiredWorkTypes?.FirstOrDefault(pawn.WorkTypeIsDisabled) is { } workType)
        {
            Messages.Message("PawnEditor.TraitWorkDisabled".Translate(pawn.Name.ToStringShort, workType.label, traitInfo.Trait.Label),
                MessageTypeDefOf.RejectInput, false);
            return;
        }

        pawn.story.traits.GainTrait(new(traitInfo.Trait.def, traitInfo.TraitDegreeData.degree));
        PawnEditor.Notify_PointsUsed();
    }

    private static List<Filter<TraitInfo>> GetFilters()
    {
        var list = new List<Filter<TraitInfo>>();

        var modSourceDict =
            LoadedModManager.runningMods
               .Where(m => m.AllDefs.OfType<TraitDef>().Any())
               .ToDictionary<ModContentPack, string, Func<TraitInfo, bool>>(m => m.Name, m => traitInfo =>
                    traitInfo.Trait.def.modContentPack.Name == m.Name);
        list.Add(new Filter_Dropdown<TraitInfo>("Source".Translate(), modSourceDict, true, "PawnEditor.SourceDesc".Translate()));

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
