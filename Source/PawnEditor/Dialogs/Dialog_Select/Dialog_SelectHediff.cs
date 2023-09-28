using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
[HotSwappable]
public class Dialog_SelectHediff : Dialog_SelectThing<HediffDef>
{
    private static readonly Dictionary<HediffDef, (List<BodyPartDef>, List<BodyPartGroupDef>)> defaultBodyParts;
    private static readonly HashSet<TechLevel> possibleTechLevels;
    private Vector2 scrollPosition;
    private Hediff selected;

    static Dialog_SelectHediff()
    {
        defaultBodyParts = DefDatabase<RecipeDef>.AllDefs.Where(recipe => recipe.addsHediff != null)
           .ToDictionary(recipe => recipe.addsHediff, recipe =>
                (recipe.appliedOnFixedBodyParts, recipe.appliedOnFixedBodyPartGroups));

        possibleTechLevels = new();
        foreach (var hediff in DefDatabase<HediffDef>.AllDefsListForReading)
            if (hediff.spawnThingOnRemoved is { techLevel: var level })
                possibleTechLevels.Add(level);
    }

    public Dialog_SelectHediff(List<HediffDef> thingList, Pawn curPawn, Hediff select = null) : base(thingList, curPawn)
    {
        OnSelected = def =>
        {
            if (defaultBodyParts.TryGetValue(def, out var result))
            {
                if (result.Item1?.Select(part => curPawn.RaceProps.body.GetPartsWithDef(part)?.FirstOrDefault()).FirstOrDefault() is { } part1)
                    curPawn.health.AddHediff(def, part1);
                else if (result.Item2?.Select(group => curPawn.RaceProps.body.AllParts.FirstOrDefault(part => part.IsInGroup(group))).FirstOrDefault() is
                         { } part2)
                    curPawn.health.AddHediff(def, part2);
                else
                    curPawn.health.AddHediff(def);
            }
            else
                curPawn.health.AddHediff(def);
        };
        selected = select ?? curPawn.health.hediffSet.GetFirstHediff<Hediff>();
    }

    public override Vector2 InitialSize => base.InitialSize + new Vector2(300, 0);

    protected override string PageTitle => "ChooseStuffForRelic".Translate() + " " + "PawnEditor.Hediff".Translate();

    protected override List<TFilter<HediffDef>> Filters()
    {
        var filters = base.Filters();
        filters.Add(new("PawnEditor.Prosthetic".Translate(), false, def => typeof(Hediff_AddedPart).IsAssignableFrom(def.hediffClass)));
        filters.Add(new("PawnEditor.IsImplant".Translate(), false, def => typeof(Hediff_Implant).IsAssignableFrom(def.hediffClass) && !typeof(Hediff_AddedPart)
           .IsAssignableFrom(def.hediffClass)));
        filters.Add(new("PawnEditor.IsInjury".Translate(), false, def => typeof(Hediff_Injury).IsAssignableFrom(def.hediffClass)));
        filters.Add(new("PawnEditor.IsDisease".Translate(), false, def => def.makesSickThought));
        var techLevel = possibleTechLevels.ToDictionary<TechLevel, FloatMenuOption, Func<HediffDef, bool>>(
            level => new(level.ToStringHuman().CapitalizeFirst(), () => { }),
            level => hediff => hediff.spawnThingOnRemoved?.techLevel == level);
        filters.Add(new("PawnEditor.TechLevel".Translate(), false, techLevel));

        return filters;
    }

    protected override void DrawInfoCard(ref Rect inRect)
    {
        base.DrawInfoCard(ref inRect);

        var hediffs = CurPawn.health.hediffSet.hediffs;
        if (hediffs.Count == 0)
        {
            var rect = inRect.TakeTopPart(30);
            Widgets.Label(rect, "None".Translate().Colorize(ColoredText.SubtleGrayColor));
            TooltipHandler.TipRegionByKey(rect, "None");
            return;
        }

        var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 15, 32 * hediffs.Count);
        var outRect = inRect.TakeTopPart(Mathf.Min(32f * 5, hediffs.Count * 32f)).ExpandedBy(4f);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            var itemsToRemove = new List<Hediff>();

            var partWidth = UIUtility.ColumnWidth(6, hediffs.Select(h => h.Part?.LabelCap ?? "WholeBody".Translate()).ToArray());
            for (var i = 0; i < hediffs.Count; i++)
            {
                var hediff = hediffs[i];
                var rowRect = viewRect.TakeTopPart(32f);
                GUI.DrawTexture(rowRect.TakeLeftPart(32f).ContractedBy(2.5f), Widgets.PlaceholderIconTex);

                if (Widgets.ButtonImage(rowRect.TakeRightPart(32f).ContractedBy(2.5f), TexButton.DeleteX)) itemsToRemove.Add(hediff);

                if (Widgets.ButtonImage(rowRect.TakeRightPart(32f).ContractedBy(2.5f), TexButton.Info)) Find.WindowStack.Add(new Dialog_InfoCard(hediff.def));

                if (Widgets.RadioButton(rowRect.xMax - 32f, rowRect.y + (32f - Widgets.RadioButtonSize) / 2, selected == hediff)) selected = hediff;

                rowRect.xMax -= 32f + 16f; // 32f from radiobutton, and 16f for spacing

                Widgets.Label(rowRect.TakeLeftPart(partWidth), (hediff.Part?.LabelCap ?? "WholeBody".Translate()).Colorize(hediff.Part == null
                    ? HealthUtility
                       .RedColor
                    : HealthUtility
                       .GetPartConditionLabel(CurPawn, hediff.Part)
                       .Second));

                Widgets.Label(rowRect, hediff.LabelCap);

                if (Mouse.IsOver(rowRect)) Widgets.DrawHighlight(rowRect);

                // ToDo: Combine with radiobutton
                if (Widgets.ButtonInvisible(rowRect)) selected = hediff;
            }

            foreach (var hediff in itemsToRemove) CurPawn.health.RemoveHediff(hediff);
            itemsToRemove.Clear();
        }

        inRect.TakeTopPart(16f);
        // UpdateInventory();
        Widgets.EndScrollView();
    }

    private static void NoOption(Rect inRect)
    {
        using (new TextBlock(TextAnchor.MiddleCenter))
            Widgets.Label(inRect, "PawnEditor.Unavailable".Translate().Colorize(ColoredText.SubtleGrayColor));
        if (Mouse.IsOver(inRect))
        {
            Widgets.DrawHighlight(inRect);
            TooltipHandler.TipRegion(inRect, "PawnEditor.UnavailableDesc".Translate());
        }
    }

    protected override void DrawOptions(ref Rect inRect)
    {
        base.DrawOptions(ref inRect);
        const float labelWidthPct = 0.3f;
        if (selected == null) return;

        var cellCount = 0;
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            if (selected is Hediff_Level hediffLevel)
            {
                Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "Level".Translate());
                float level = hediffLevel.level;
                Widgets.HorizontalSlider(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), ref level, new(0, selected.def.maxSeverity),
                    hediffLevel.level.ToString());
                hediffLevel.SetLevelTo(Mathf.RoundToInt(level));
            }
            else if (selected.def.stages?.Count > 1)
            {
                Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "PawnEditor.Severity".Translate());
                var level = selected.Severity;
                Widgets.HorizontalSlider(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), ref level, new(0, selected.def.maxSeverity),
                    selected.SeverityLabel);
                selected.Severity = level;
            }
            else
                NoOption(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct));

            cellCount++;


            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "PawnEditor.BodyPart".Translate());
            if (Widgets.ButtonText(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), selected.Part?.LabelCap ?? "WholeBody".Translate()))
                Find.WindowStack.Add(new FloatMenu(CurPawn.RaceProps.body.AllParts.Select(part =>
                        new FloatMenuOption(part.LabelCap, () => selected.Part = part))
                   .Prepend(new("WholeBody".Translate(), () => selected.Part = null))
                   .ToList()));

            cellCount++;

            if (selected is HediffWithComps hediff)
                foreach (var comp in hediff.comps)
                    switch (comp)
                    {
                        case HediffComp_Immunizable:
                            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "Immunity".Translate());
                            CurPawn.health.immunity.TryAddImmunityRecord(selected.def, selected.def);
                            var immunity = CurPawn.health.immunity.GetImmunityRecord(selected.def);
                            Widgets.HorizontalSlider(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), ref immunity.immunity,
                                FloatRange.ZeroToOne, immunity.immunity.ToStringPercent());
                            cellCount++;
                            break;
                        case HediffComp_Disappears disappears:
                            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "TimeLeft".Translate().CapitalizeFirst());
                            var progress = disappears.Progress;
                            progress = Widgets.HorizontalSlider_NewTemp(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), progress,
                                0, 1, true, disappears.ticksToDisappear.ToStringTicksToPeriodVerbose(), "0" + "SecondsLower".Translate(),
                                disappears.disappearsAfterTicks.ToStringTicksToPeriodVerbose());
                            disappears.ticksToDisappear = Mathf.RoundToInt(progress * Math.Max(1, disappears.disappearsAfterTicks));
                            cellCount++;
                            break;
                        case HediffComp_GetsPermanent permanent:
                            var rect = UIUtility.CellRect(cellCount, inRect);
                            Widgets.Label(rect.LeftPart(labelWidthPct), "PawnEditor.IsPermanent".Translate());
                            var isPermanent = permanent.IsPermanent;
                            Widgets.Checkbox(rect.xMax - 30, rect.y + 2, ref isPermanent);
                            permanent.IsPermanent = isPermanent;
                            cellCount++;
                            break;
                    }
        }

        inRect.TakeTopPart(Mathf.CeilToInt(cellCount / 2f) * 38f + 16f);
    }
}
