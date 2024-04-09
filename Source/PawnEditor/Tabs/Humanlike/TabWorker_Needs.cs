using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_Needs : TabWorker_Table<Pawn>
{
    private Vector2 needsScrollPos;

    private Vector2 thoughtsScrollPos;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var oldDevMode = Prefs.DevMode;
        var oldGodMode = DebugSettings.godMode;
        Prefs.DevMode = false;
        DebugSettings.godMode = false;

        DoBottomOptions(rect.TakeBottomPart(UIUtility.RegularButtonHeight), pawn);

        var needsRect = rect.TakeLeftPart(330f);
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(needsRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "TabNeeds".Translate().Colorize(ColoredText.TipSectionTitleColor));
        needsRect.xMin += 4f;
        needsRect.yMin += 4f;
        DrawNeeds(needsRect, pawn);

        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(rect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "Mood".Translate().Colorize(ColoredText.TipSectionTitleColor));
        rect.xMin += 4f;
        rect.yMin += 4f;

        DrawNeedWidget(rect.TakeTopPart(30f).LeftPart(0.8f), pawn.needs.mood, drawLabel: false);
        rect.yMin += 16f;
        rect.xMin -= 4f;

        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(rect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "PawnEditor.Thoughts".Translate().Colorize(ColoredText.TipSectionTitleColor));
        DrawThoughts(rect, pawn);


        Prefs.DevMode = oldDevMode;
        DebugSettings.godMode = oldGodMode;
    }

    private void DoBottomOptions(Rect inRect, Pawn pawn)
    {
        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.QuickActions".Translate(), 80f))
            Find.WindowStack.Add(new FloatMenu(new()
            {
                new("PawnEditor.RefillNeeds".Translate(), () =>
                {
                    foreach (var need in pawn.needs.AllNeeds) need.CurLevelPercentage = 1f;
                }),
                new("PawnEditor.CancelBreakdown".Translate(),
                    () => pawn.mindState.mentalStateHandler.CurState?.RecoverFromState())
            }));

        inRect.xMin += 4f;

        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.AddThought".Translate())) Find.WindowStack.Add(new ListingMenu_Thoughts(pawn, table));

        inRect.xMin += 4f;
    }

    private void DrawNeeds(Rect inRect, Pawn pawn)
    {
        NeedsCardUtility.UpdateDisplayNeeds(pawn);
        var needs = NeedsCardUtility.displayNeeds;
        var resourceGenes = pawn.genes.cachedGenes.OfType<Gene_Resource>().ToList();

        var viewRect = new Rect(0, 0, inRect.width - 20f,
            needs.Sum(_ => 38f) + resourceGenes.Sum(_ => 38f));
        Widgets.BeginScrollView(inRect, ref needsScrollPos, viewRect);
        foreach (var n in needs)
        {
            DrawNeedWidget(viewRect.TakeTopPart(30f), n);
            viewRect.yMin += 8f;
        }

        viewRect.yMin += 16f;

        if (ModsConfig.IdeologyActive)
            // ToDo: Gene_Resource SetMax()
            foreach (var resourceGene in resourceGenes)
            {
                UIUtility.GeneResourceBarWidget(viewRect.TakeTopPart(30f), resourceGene);
                viewRect.yMin += 8f;
            }

        if (ModsConfig.royaltyActive && pawn.psychicEntropy.NeedsPsyfocus)
            UIUtility.ResourceBarWidget(viewRect.TakeTopPart(30f), ref pawn.psychicEntropy.currentPsyfocus, "Psyfocus".Translate(),
                new(0.34f, 0.42f, 0.43f), new Color(0.03f, 0.035f, 0.05f), new() { 0.25f, 0.5f }, 0.05f);

        Widgets.EndScrollView();
    }

    private void DrawNeedWidget(Rect inRect, Need n, float margin = 16, bool drawLabel = true)
    {
        var width = n.def.major ? 1 : 0.8f;
        if (drawLabel)
            Widgets.Label(inRect.TakeLeftPart(100f), n.LabelCap);
        var barRect = inRect.LeftPart(width);
        n.DrawOnGUI(barRect, customMargin: margin, drawLabel: false);
        var pct = n.CurInstantLevelPercentage;
        var vector2 = new Vector2(barRect.x + (barRect.width - margin * 2f) * pct, barRect.y + barRect.height);
        GUI.DrawTexture(new(vector2.x - 12 / 2f + 16f, vector2.y - margin / 2f, 12, 12), Need.BarInstantMarkerTex);

        var barRect1 = barRect;
        barRect1.yMax -= margin / 2;
        barRect.xMin -= margin;
        barRect1.xMax -= margin;
        var plusRect = barRect1.TakeRightPart(barRect1.height).ContractedBy(4f);
        var minRect = barRect1.TakeRightPart(barRect1.height).ContractedBy(4f);
        if (Widgets.ButtonImage(plusRect, TexButton.Plus))
            n.CurLevelPercentage += 0.1f;
        if (Mouse.IsOver(plusRect))
            TooltipHandler.TipRegion(plusRect, (TipSignal)"+ 10%");
        if (Widgets.ButtonImage(minRect, TexButton.Minus))
            n.CurLevelPercentage -= 0.1f;
        if (Mouse.IsOver(minRect))
            TooltipHandler.TipRegion(minRect, (TipSignal)"- 10%");
    }

    private void DrawThoughts(Rect inRect, Pawn pawn)
    {
        var viewRect = new Rect(0, 0, inRect.width - 20, NeedsCardUtility.thoughtGroupsPresent.Count * 30 + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref thoughtsScrollPos, viewRect);
        table.OnGUI(viewRect, pawn);
        Widgets.EndScrollView();
    }

    public static string GetThoughtTip(Pawn pawn, Thought leadingThought, Thought group)
    {
        var stringBuilder = new StringBuilder();
        if (pawn.DevelopmentalStage.Baby())
        {
            stringBuilder.AppendLine(leadingThought.BabyTalk);
            stringBuilder.AppendLine();
            stringBuilder.AppendTagged(
                ("Translation".Translate() + ": " + leadingThought.Description).Colorize(ColoredText.SubtleGrayColor));
        }
        else
            stringBuilder.Append(leadingThought.Description);

        var durationTicks = group.DurationTicks;
        if (durationTicks > 5)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            if (leadingThought is Thought_Memory thought_Memory)
            {
                if (NeedsCardUtility.thoughtGroup.Count == 1)
                    stringBuilder.AppendTagged(
                        "ThoughtExpiresIn".Translate((durationTicks - thought_Memory.age).ToStringTicksToPeriod()));
                else
                {
                    var num = int.MaxValue;
                    var num2 = int.MinValue;
                    foreach (var thought in NeedsCardUtility.thoughtGroup.OfType<Thought_Memory>())
                    {
                        num = Mathf.Min(num, thought.age);
                        num2 = Mathf.Max(num2, thought.age);
                    }

                    stringBuilder.AppendTagged(
                        "ThoughtStartsExpiringIn".Translate((durationTicks - num2).ToStringTicksToPeriod()));
                    stringBuilder.AppendLine();
                    stringBuilder.AppendTagged(
                        "ThoughtFinishesExpiringIn".Translate((durationTicks - num).ToStringTicksToPeriod()));
                }
            }
        }

        if (NeedsCardUtility.thoughtGroup.Count > 1)
        {
            var flag = false;
            for (var i = 1; i < NeedsCardUtility.thoughtGroup.Count; i++)
            {
                var flag2 = false;
                for (var j = 0; j < i; j++)
                    if (NeedsCardUtility.thoughtGroup[i].LabelCap == NeedsCardUtility.thoughtGroup[j].LabelCap)
                    {
                        flag2 = true;
                        break;
                    }

                if (!flag2)
                {
                    if (!flag)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                        flag = true;
                    }

                    stringBuilder.AppendLine("+ " + NeedsCardUtility.thoughtGroup[i].LabelCap);
                }
            }
        }

        return stringBuilder.ToString();
    }

    protected override List<UITable<Pawn>.Heading> GetHeadings() =>
        new()
        {
            new(35f),
            new("PawnEditor.Thought".Translate(), textAnchor: TextAnchor.LowerLeft),
            new("ExpiresIn".Translate().CapitalizeFirst(), 120),
            new("PawnEditor.Weight".Translate(), 60),
            new(100),
            new(30)
        };

    protected override List<UITable<Pawn>.Row> GetRows(Pawn pawn)
    {
        PawnNeedsUIUtility.GetThoughtGroupsInDisplayOrder(pawn.needs.mood, NeedsCardUtility.thoughtGroupsPresent);
        var result = new List<UITable<Pawn>.Row>(NeedsCardUtility.thoughtGroupsPresent.Count);
        for (var i = 0; i < NeedsCardUtility.thoughtGroupsPresent.Count; i++)
        {
            var items = new List<UITable<Pawn>.Row.Item>(5);
            var thoughtGroup = NeedsCardUtility.thoughtGroupsPresent[i];
            pawn.needs.mood.thoughts.GetMoodThoughts(thoughtGroup, NeedsCardUtility.thoughtGroup);
            var leadingThought = PawnNeedsUIUtility.GetLeadingThoughtInGroup(NeedsCardUtility.thoughtGroup);
            if (!leadingThought.VisibleInNeedsTab)
            {
                NeedsCardUtility.thoughtGroup.Clear();
                continue;
            }

            if (leadingThought != NeedsCardUtility.thoughtGroup[0])
            {
                NeedsCardUtility.thoughtGroup.Remove(leadingThought);
                NeedsCardUtility.thoughtGroup.Insert(0, leadingThought);
            }

            items.Add(new(iconRect =>
            {
                iconRect.xMin += 3f;
                iconRect = iconRect.ContractedBy(2f);

                if (ModsConfig.IdeologyActive)
                    if (leadingThought.sourcePrecept != null)
                    {
                        if (!Find.IdeoManager.classicMode)
                            IdeoUIUtility.DoIdeoIcon(iconRect.ContractedBy(4f), leadingThought.sourcePrecept.ideo, false,
                                () => IdeoUIUtility.OpenIdeoInfo(leadingThought.sourcePrecept.ideo));
                        return;
                    }

                GUI.DrawTexture(iconRect, Widgets.PlaceholderIconTex);
            }));

            var label = leadingThought.LabelCap;
            if (NeedsCardUtility.thoughtGroup.Count > 1) label += $" x{NeedsCardUtility.thoughtGroup.Count}";
            items.Add(new(label, i, TextAnchor.MiddleLeft));

            var durationTicks = thoughtGroup.DurationTicks;
            if (durationTicks > 5 && leadingThought is Thought_Memory thoughtMemory)
            {
                var age = NeedsCardUtility.thoughtGroup.Count == 1
                    ? thoughtMemory.age
                    : NeedsCardUtility.thoughtGroup.Cast<Thought_Memory>().Aggregate(int.MaxValue, (current, memory) => Mathf.Min(current, memory.age));

                items.Add(new((durationTicks - age).ToStringTicksToDays().Colorize(ColoredText.SubtleGrayColor), durationTicks));
            }
            else items.Add(new("Never".Translate().Colorize(ColoredText.SubtleGrayColor)));

            var moodOffset = pawn.needs.mood.thoughts.MoodOffsetOfGroup(thoughtGroup);
            items.Add(new(moodOffset.ToString("##0")
                .Colorize(moodOffset switch
                {
                    0f => NeedsCardUtility.NoEffectColor,
                    > 0f => NeedsCardUtility.MoodColor,
                    _ => NeedsCardUtility.MoodColorNegative
                }), Mathf.RoundToInt(moodOffset)));

            if (leadingThought is Thought_Memory)
            {
                var list = NeedsCardUtility.thoughtGroup.ListFullCopy();
                items.Add(new(editRect => EditUtility.EditButton(editRect, list, pawn, table)));
            }
            else items.Add(new());

            if (NeedsCardUtility.thoughtGroup.OfType<Thought_Memory>().Any())
            {
                var j = i;
                items.Add(new(TexButton.Delete, () =>
                {
                    for (var j = NeedsCardUtility.thoughtGroup.Count; j-- > 0;)
                        if (NeedsCardUtility.thoughtGroup[j] is Thought_Memory memory)
                        {
                            NeedsCardUtility.thoughtGroup.RemoveAt(j);
                            pawn.needs.mood.thoughts.memories.RemoveMemory(memory);
                        }

                    table.ClearCache();
                }));
            }
            else items.Add(new());

            result.Add(new(items, GetThoughtTip(pawn, leadingThought, thoughtGroup)));
        }

        return result;
    }
}