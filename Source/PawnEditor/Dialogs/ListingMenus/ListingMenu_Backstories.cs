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

    static ListingMenu_Backstories()
    {
        items = DefDatabase<BackstoryDef>.AllDefsListForReading;
    }

    public ListingMenu_Backstories(Pawn pawn) : base(items, b => b.titleShort.CapitalizeFirst(), b => TryAdd(b, pawn),
        "ChooseStuffForRelic".Translate() + " " + "Backstory".Translate().ToLower(),
        b => b.FullDescriptionFor(pawn).Resolve(), null, GetFilters(), pawn)
    {
    }

    private static void TryAdd(BackstoryDef backstoryDef, Pawn pawn)
    {
        if (backstoryDef.slot == BackstorySlot.Childhood)
            pawn.story.Childhood = backstoryDef;
        else if (!pawn.ageTracker.Adult)
            Messages.Message("PawnEditor.NoAdultOnChild".Translate(backstoryDef.LabelCap), MessageTypeDefOf.RejectInput, false);
        else
            pawn.story.Adulthood = backstoryDef;
    }

    private static List<TFilter<BackstoryDef>> GetFilters()
    {
        var list = new List<TFilter<BackstoryDef>>();

        list.Add(new("PawnEditor.ShuffableOnly".Translate(), true, item => item.shuffleable, "PawnEditor.ShuffableOnlyDesc".Translate()));

        var backstorySlotDict = new Dictionary<FloatMenuOption, Func<BackstoryDef, bool>>();
        Enum.GetValues(typeof(BackstorySlot))
            .Cast<BackstorySlot>()
            .ToList()
            .ForEach(bs =>
            {
                var label = bs.ToString();
                var option = new FloatMenuOption(label, () => { });
                backstorySlotDict.Add(option, bd => bd.slot == bs);
            });
        list.Add(new("PawnEditor.BackstorySlot".Translate(), false, backstorySlotDict, "PawnEditor.BackstorySlotDesc".Translate()));


        for (var i = 0; i < 5; i++)
        {
            var skillGainDict = new Dictionary<FloatMenuOption, Func<BackstoryDef, bool>>();
            DefDatabase<SkillDef>.AllDefs.Where(sd => items.Any(bd => bd.skillGains.ContainsKey(sd)))
                .ToList()
                .ForEach(sd =>
                {
                    var label = sd.skillLabel.CapitalizeFirst();
                    var option = new FloatMenuOption(label, () => { });
                    skillGainDict.Add(option, bd => bd.skillGains.ContainsKey(sd) && bd.skillGains[sd] > 0);
                });
            list.Add(new("PawnEditor.SkillGain".Translate(), false, skillGainDict, "PawnEditor.SkillGainDesc".Translate()));
        }


        list.Add(new("PawnEditor.WorkDisables".Translate(), false, item => item.workDisables == WorkTags.None, "PawnEditor.WorkDisablesDesc".Translate()));

        list.Add(new("PawnEditor.SkillLose".Translate(), false, item => item.skillGains.Values.All(i => i > 0), "PawnEditor.SkillLoseDesc".Translate()));

        return list;
    }
}