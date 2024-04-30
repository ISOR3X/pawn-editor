using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
// ToDo: Separate list of mechs/ animals
public class ListingMenu_Hediffs : ListingMenu<HediffDef>
{
    private static readonly List<HediffDef> items;
    private static readonly Func<HediffDef, string> labelGetter = d => d.LabelCap;
    private static readonly Func<HediffDef, string> descGetter = d => d.Description;
    private static readonly List<Filter<HediffDef>> filters;

    private static readonly HashSet<TechLevel> possibleTechLevels;
    public static readonly Dictionary<HediffDef, (List<BodyPartDef>, List<BodyPartGroupDef>)> defaultBodyParts;

    public static BodyPartRecord currentBodyPart;

    static ListingMenu_Hediffs()
    {
        defaultBodyParts = new();
        currentBodyPart = null;
        foreach (var recipe in DefDatabase<RecipeDef>.AllDefs.Where(recipe => recipe.addsHediff != null))
            AddDefaultBodyParts(recipe.addsHediff, recipe.appliedOnFixedBodyParts, recipe.appliedOnFixedBodyPartGroups);

        possibleTechLevels = new();
        foreach (var hediff in DefDatabase<HediffDef>.AllDefsListForReading)
        {
            if (hediff.spawnThingOnRemoved is { techLevel: var level })
                possibleTechLevels.Add(level);
            if (hediff.defaultInstallPart != null)
                AddDefaultBodyParts(hediff, new List<BodyPartDef> { hediff.defaultInstallPart }, null);
        }


        items = DefDatabase<HediffDef>.AllDefsListForReading;
        filters = GetFilters();
    }

    private static void AddDefaultBodyParts(HediffDef hediff, List<BodyPartDef> appliedOnFixedBodyParts, List<BodyPartGroupDef> appliedOnFixedBodyPartGroups)
    {
        if (!defaultBodyParts.TryGetValue(hediff, out var item))
            defaultBodyParts.Add(hediff, (appliedOnFixedBodyParts ?? new(), appliedOnFixedBodyPartGroups ?? new()));
        else
        {
            List<BodyPartDef> item1 = new();
            List<BodyPartGroupDef> item2 = new();
            item1.AddRange(item.Item1);
            item2.AddRange(item.Item2);
            if (appliedOnFixedBodyParts != null)
                item1.AddRange(appliedOnFixedBodyParts);
            if (appliedOnFixedBodyPartGroups != null)
                item2.AddRange(appliedOnFixedBodyPartGroups);
            defaultBodyParts[hediff] = (item1, item2);
        }
    }

    public ListingMenu_Hediffs(Pawn pawn, UITable<Pawn> table) : base(items, labelGetter, b => TryAdd(b, pawn, table),
        "PawnEditor.Choose".Translate() + " " + "PawnEditor.Hediff".Translate().ToLower(),
        b => descGetter(b), null, filters, pawn)
    {

    }

    private static AddResult TryAdd(HediffDef hediffDef, Pawn pawn, UITable<Pawn> uiTable)
    {
        BodyPartRecord part = currentBodyPart;
        if (part is null && defaultBodyParts.TryGetValue(hediffDef, out var defaultPart))
        {
            if (defaultPart.Item1?.Select(part => pawn.RaceProps.body.GetPartsWithDef(part)?.FirstOrDefault()).FirstOrDefault() is { } part1)
                part = part1;
            if (defaultPart.Item2?.Select(group => pawn.RaceProps.body.AllParts.FirstOrDefault(part => part.IsInGroup(group))).FirstOrDefault() is { } part2)
                part = part2;
        }
        if (part == null && (typeof(Hediff_Injury).IsAssignableFrom(hediffDef.hediffClass) || typeof(
                Hediff_MissingPart).IsAssignableFrom(hediffDef.hediffClass)))
            part = pawn.RaceProps.body.corePart;

        AddResult result = new SuccessInfo(() =>
        {
            pawn.health.AddHediff(hediffDef, part);
            pawn.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
            TabWorker_Table<Pawn>.ClearCacheFor<TabWorker_Needs>();
            PawnEditor.Notify_PointsUsed();
            uiTable.ClearCache();
        });

        var price = hediffDef.priceOffset;
        if (price == 0 && hediffDef.priceImpact && hediffDef.spawnThingOnRemoved != null) price = hediffDef.spawnThingOnRemoved.BaseMarketValue;
        if (price is >= 1 or <= 1 && hediffDef.priceImpact)
            result = new ConditionalInfo(PawnEditor.CanUsePoints(price), result);

        if (typeof(Hediff_AddedPart).IsAssignableFrom(hediffDef.hediffClass)
            && pawn.health.hediffSet.GetFirstHediffMatchingPart<Hediff_AddedPart>(part) is { } hediff)
            result = new ConfirmInfo("PawnEditor.HediffConflict".Translate(hediffDef.LabelCap, hediff.LabelCap), "HediffConflict", result);

        if (typeof(Hediff_AddedPart).IsAssignableFrom(hediffDef.hediffClass) && part == null)
            result = new ConfirmInfo("PawnEditor.MissingPart".Translate(hediffDef.LabelCap), "MissingPart", result);


        if (!typeof(Hediff_Injury).IsAssignableFrom(hediffDef.hediffClass))
        {
            var existing = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs(ref existing, h => h.def == hediffDef && h.Part == part);
            result = new ConfirmInfo("PawnEditor.HediffDuplicate".Translate(hediffDef.LabelCap), "HediffDuplicate", result, existing.Count > 0);
        }

        result = new ConfirmInfo("PawnEditor.WouldDie".Translate(hediffDef.LabelCap, pawn.NameShortColored), "HediffDeath", result,
            pawn.health.WouldDieAfterAddingHediff(hediffDef, part, hediffDef.initialSeverity));
        result = new ConfirmInfo("PawnEditor.WouldBeDowned".Translate(hediffDef.LabelCap, pawn.NameShortColored), "HediffDowned", result,
            pawn.health.WouldBeDownedAfterAddingHediff(hediffDef, part, hediffDef.initialSeverity));

        return result;
    }

    private List<BodyPartRecord> AllowedBodyParts()
    {
        var hediffDef = Listing.Selected;
        var pawn = Pawn;
        var records = new List<BodyPartRecord>();
        if (defaultBodyParts.TryGetValue(hediffDef, out var defaultPart))
        {
            var allBodyParts = defaultPart.Item1;
            foreach (var bodyPart in allBodyParts)
            {
                var parts = pawn.RaceProps.body.GetPartsWithDef(bodyPart);
                records.AddRange(parts);
            }
        }
        return records;
    }

    private static void RecheckCurrentBodyPart(List<BodyPartRecord> records)
    {
        if (currentBodyPart != null && records.Contains(currentBodyPart) is false || currentBodyPart is null)
        {
            currentBodyPart = records.Any() ? records[0] : null;
        }
    }

    protected override void DrawFooter(ref Rect inRect)
    {
        if (Listing.Selected != null)
        {
            var allBodyParts = AllowedBodyParts();
            RecheckCurrentBodyPart(allBodyParts);
            if (allBodyParts.Count > 1)
            {
                const float padding = 4f;
                var rowRect = inRect.TakeBottomPart(30f + padding * 2f);
                rowRect = rowRect.ContractedBy(0f, padding);
                Widgets.Label(rowRect.LeftHalf(), "PawnEditor.SelectedLocation".Translate());
                if (Widgets.ButtonText(rowRect.TakeRightPart(UIUtility.BottomButtonSize.x),
                    currentBodyPart is null ? "None".Translate() : currentBodyPart.LabelCap))
                {
                    var options = new List<FloatMenuOption>();
                    foreach (var part in allBodyParts)
                    {
                        options.Add(new FloatMenuOption(part.LabelCap, delegate
                        {
                            currentBodyPart = part;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
        }
    }

    private static List<Filter<HediffDef>> GetFilters()
    {
        var list = new List<Filter<HediffDef>>
        {
            new Filter_Toggle<HediffDef>("PawnEditor.Prosthetic".Translate(), def => typeof(Hediff_AddedPart).IsAssignableFrom(def.hediffClass)),
            new Filter_Toggle<HediffDef>("PawnEditor.IsImplant".Translate(),
                def => typeof(Hediff_Implant).IsAssignableFrom(def.hediffClass) && !typeof(Hediff_AddedPart).IsAssignableFrom(def.hediffClass)),
            new Filter_Toggle<HediffDef>("PawnEditor.IsInjury".Translate(), def => typeof(Hediff_Injury).IsAssignableFrom(def.hediffClass)),
            new Filter_Toggle<HediffDef>("PawnEditor.IsDisease".Translate(), def => def.makesSickThought)
        };

        var techLevel = possibleTechLevels.ToDictionary<TechLevel, string, Func<HediffDef, bool>>(
            level => level.ToStringHuman().CapitalizeFirst(),
            level => hediff => hediff.spawnThingOnRemoved?.techLevel == level);
        list.Add(new Filter_Dropdown<HediffDef>("PawnEditor.TechLevel".Translate(), techLevel));
        list.Add(new Filter_ModSource<HediffDef>());
        return list;
    }
}