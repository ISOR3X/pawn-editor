using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ListingMenu_Backstories : ListingMenu<BackstoryDef>
{
    private static readonly Dictionary<BackstorySlot, List<BackstoryDef>> backstoriesBySlot = new();

    static ListingMenu_Backstories()
    {
        var items = DefDatabase<BackstoryDef>.AllDefsListForReading;
        backstoriesBySlot.Add(BackstorySlot.Adulthood, items.Where(b => b.slot == BackstorySlot.Adulthood).ToList());
        backstoriesBySlot.Add(BackstorySlot.Childhood, items.Where(b => b.slot == BackstorySlot.Childhood).ToList());
    }

    public ListingMenu_Backstories(Pawn pawn, BackstorySlot backstorySlot) : base(backstoriesBySlot[backstorySlot], b => b.TitleFor(pawn.gender).CapitalizeFirst(), b => TryAdd(b, pawn),
        "PawnEditor.Choose".Translate() + " " + "Backstory".Translate().ToLower(),
        b => DoToolTipFor(b, pawn), null, GetFilters(), pawn)
    {
    }

    private static AddResult TryAdd(BackstoryDef backstoryDef, Pawn pawn)
    {
        if (backstoryDef.slot == BackstorySlot.Childhood)
            pawn.story.Childhood = backstoryDef;
        else if (!pawn.ageTracker.Adult)
            return "PawnEditor.NoAdultOnChild".Translate(backstoryDef.LabelCap);
        else
            pawn.story.Adulthood = backstoryDef;

        pawn.skills = new Pawn_SkillTracker(pawn);
        PawnGenerator.GenerateSkills(pawn, default);
        pawn.Notify_DisabledWorkTypesChanged();
        return true;
    }

    private static List<Filter<BackstoryDef>> GetFilters()
    {
        var list = new List<Filter<BackstoryDef>>();

        list.Add(new Filter_Toggle<BackstoryDef>("PawnEditor.ShuffableOnly".Translate(), item => item.shuffleable, true,
            "PawnEditor.ShuffableOnlyDesc".Translate()));

        // var backstorySlotDict = Enum.GetValues(typeof(BackstorySlot))
        //     .Cast<BackstorySlot>()
        //     .ToDictionary<BackstorySlot, string, Func<BackstoryDef, bool>>(bs => bs.ToString(), bs => bd => bd.slot == bs);
        // list.Add(
        //     new Filter_Dropdown<BackstoryDef>("PawnEditor.BackstorySlot".Translate(), backstorySlotDict, false, "PawnEditor.BackstorySlotDesc".Translate()));

        for (var i = 0; i < 5; i++)
        {
            var spawnCategoriesDict = DefDatabase<BackstoryDef>.AllDefs.SelectMany(bd => bd.spawnCategories).Distinct()
                .ToDictionary<string, string, Func<BackstoryDef, bool>>(sc => sc.ConvertCamelCase(), sc => bd => bd.spawnCategories.Contains(sc));
            list.Add(
                new Filter_Dropdown<BackstoryDef>("PawnEditor.BackstoryType".Translate(), spawnCategoriesDict, false, "PawnEditor.BackstoryTypeDesc".Translate()));
        }

        for (var i = 0; i < 5; i++)
        {
            var skillGainDict = DefDatabase<SkillDef>.AllDefs.Where(sd => backstoriesBySlot
                    .SelectMany(p => p.Value).Any(bd => bd.skillGains.Any(sg => sg.skill == sd)))
                .ToDictionary<SkillDef, string, Func<BackstoryDef, bool>>(sd => sd.skillLabel.CapitalizeFirst(),
                    sd => bd => bd.skillGains.Any(sg => sg.skill == sd && sg.amount > 0));
            list.Add(new Filter_Dropdown<BackstoryDef>("PawnEditor.SkillGain".Translate(), skillGainDict, false, "PawnEditor.SkillGainDesc".Translate()));
        }

        list.Add(new Filter_Toggle<BackstoryDef>("PawnEditor.WorkDisables".Translate(), item => item.workDisables == WorkTags.None, false,
            "PawnEditor.WorkDisables".Translate()));

        for (var i = 0; i < 5; i++)
        {
            var workDict = DefDatabase<BackstoryDef>.AllDefs.SelectMany(bd => bd.DisabledWorkTypes).Distinct()
                .ToDictionary<WorkTypeDef, string, Func<BackstoryDef, bool>>(dwt => dwt.ToStringSafe().ConvertCamelCase(), dwt => bd => bd.DisabledWorkTypes.Contains(dwt));
            list.Add(new Filter_Dropdown<BackstoryDef>("PawnEditor.DisabledWorkTypes".Translate(), workDict, false, "PawnEditor.DisabledWorkTypesDesc".Translate()));
        }

        list.Add(new Filter_Toggle<BackstoryDef>("PawnEditor.SkillLose".Translate(), item => item.skillGains.All(sg => sg.amount > 0), false,
            "PawnEditor.SkillLose".Translate()));

        for (var i = 0; i < 5; i++)
        {
            var skillGainDict = DefDatabase<SkillDef>.AllDefs.Where(sd => backstoriesBySlot
                    .SelectMany(p => p.Value).Any(bd => bd.skillGains.Any(sg => sg.skill == sd)))
                .ToDictionary<SkillDef, string, Func<BackstoryDef, bool>>(sd => sd.skillLabel.CapitalizeFirst(),
                    sd => bd => bd.skillGains.Any(sg => sg.skill == sd && sg.amount < 0));
            list.Add(new Filter_Dropdown<BackstoryDef>("PawnEditor.SkillLoses".Translate(), skillGainDict, false, "PawnEditor.SkillLosesDesc".Translate()));
        }

        list.Add(new Filter_ModSource<BackstoryDef>());

        return list;
    }

    private static string DoToolTipFor(BackstoryDef backstoryDef, Pawn pawn)
    {
        string output = backstoryDef.FullDescriptionFor(pawn).Resolve();
        string cats = string.Join(", ", backstoryDef.spawnCategories.Select(sc => sc.ConvertCamelCase()));
        output += "\n\n" + "PawnEditor.Categories".Translate().CapitalizeFirst() + ": \n" + cats;
        return output;
    }
}