using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ListingMenu_Backstories : ListingMenu<BackstoryDef>
{
    private static readonly List<BackstoryDef> items;

    static ListingMenu_Backstories() => items = DefDatabase<BackstoryDef>.AllDefsListForReading;

    public ListingMenu_Backstories(Pawn pawn) : base(items, b => b.titleShort.CapitalizeFirst(), b => TryAdd(b, pawn),
        "ChooseStuffForRelic".Translate() + " " + "Backstory".Translate().ToLower(),
        b => b.FullDescriptionFor(pawn).Resolve(), null, GetFilters(), pawn) { }

    private static AddResult TryAdd(BackstoryDef backstoryDef, Pawn pawn)
    {
        if (backstoryDef.slot == BackstorySlot.Childhood)
            pawn.story.Childhood = backstoryDef;
        else if (!pawn.ageTracker.Adult)
            return "PawnEditor.NoAdultOnChild".Translate(backstoryDef.LabelCap);
        else
            pawn.story.Adulthood = backstoryDef;

        return true;
    }

    private static List<Filter<BackstoryDef>> GetFilters()
    {
        var list = new List<Filter<BackstoryDef>>();

        list.Add(new Filter_Toggle<BackstoryDef>("PawnEditor.ShuffableOnly".Translate(), item => item.shuffleable, true,
            "PawnEditor.ShuffableOnlyDesc".Translate()));

        var backstorySlotDict = Enum.GetValues(typeof(BackstorySlot))
           .Cast<BackstorySlot>()
           .ToDictionary<BackstorySlot, string, Func<BackstoryDef, bool>>(bs => bs.ToString(), bs => bd => bd.slot == bs);
        list.Add(
            new Filter_Dropdown<BackstoryDef>("PawnEditor.BackstorySlot".Translate(), backstorySlotDict, false, "PawnEditor.BackstorySlotDesc".Translate()));


        for (var i = 0; i < 5; i++)
        {
            var skillGainDict = DefDatabase<SkillDef>.AllDefs.Where(sd => items.Any(bd => bd.skillGains.ContainsKey(sd)))
               .ToDictionary<SkillDef, string, Func<BackstoryDef, bool>>(sd => sd.skillLabel.CapitalizeFirst(),
                    sd => bd => bd.skillGains.ContainsKey(sd) && bd.skillGains[sd] > 0);
            list.Add(new Filter_Dropdown<BackstoryDef>("PawnEditor.SkillGain".Translate(), skillGainDict, false, "PawnEditor.SkillGainDesc".Translate()));
        }


        list.Add(new Filter_Toggle<BackstoryDef>("PawnEditor.WorkDisables".Translate(), item => item.workDisables == WorkTags.None, false,
            "PawnEditor.WorkDisablesDesc".Translate()));

        list.Add(new Filter_Toggle<BackstoryDef>("PawnEditor.SkillLose".Translate(), item => item.skillGains.Values.All(i => i > 0), false,
            "PawnEditor.SkillLoseDesc".Translate()));

        return list;
    }
}
