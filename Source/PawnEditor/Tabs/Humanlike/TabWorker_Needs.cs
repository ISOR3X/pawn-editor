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
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        var bottomButRect = rect.TakeBottomPart(30 + 2 * 4);
        PawnEditor.DrawPawnPortrait(portraitRect);

        rect.yMin += 8;
        DrawBottomButtons(bottomButRect, pawn);
        DrawNeeds(rect.TakeLeftPart(225f), pawn);
        DrawThoughts(rect, pawn);
    }

    private static void DrawBottomButtons(Rect inRect, Pawn pawn)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(inRect.width / 4).ContractedBy(4), "PawnEditor.QuickActions".Translate()))
            Find.WindowStack.Add(new FloatMenu(new()
            {
                new("PawnEditor.RefillNeeds".Translate(), () =>
                {
                    foreach (var need in pawn.needs.AllNeeds) need.CurLevelPercentage = 1f;
                }),
                new("PawnEditor.CancelBreakdown".Translate(),
                    () => pawn.mindState.mentalStateHandler.CurState?.RecoverFromState())
            }));

        if (Widgets.ButtonText(inRect.TakeLeftPart(inRect.width / 4).ContractedBy(4), "PawnEditor.AddThought".Translate())) { }
    }

    private void DrawNeeds(Rect inRect, Pawn pawn)
    {
        NeedsCardUtility.UpdateDisplayNeeds(pawn);
        var needs = NeedsCardUtility.displayNeeds;
        needs.Insert(0, pawn.needs.mood);
        var viewRect = new Rect(0, 0, inRect.width - 20f,
            needs.Sum(need => need.def.major ? 70f : 50f));
        var oldDevMode = Prefs.DevMode;
        var oldGodMode = DebugSettings.godMode;
        Prefs.DevMode = true;
        DebugSettings.godMode = true;
        Widgets.BeginScrollView(inRect, ref needsScrollPos, viewRect);
        foreach (var n in needs)
        {
            var height = n.def.major ? 70f : 50f;
            var width = n.def.major ? 1 : 0.74f;
            n.DrawOnGUI(viewRect.TakeTopPart(height).LeftPart(width), customMargin: 16f);
        }

        Widgets.EndScrollView();
        Prefs.DevMode = oldDevMode;
        DebugSettings.godMode = oldGodMode;
    }

    private void DrawThoughts(Rect inRect, Pawn pawn)
    {
        var viewRect = new Rect(0, 0, inRect.width - 20, NeedsCardUtility.thoughtGroupsPresent.Count * 30 + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref thoughtsScrollPos, viewRect);
        table.OnGUI(viewRect, pawn);
        Widgets.EndScrollView();
    }

    private static string GetThoughtTip(Pawn pawn, Thought leadingThought, Thought group)
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
            new("PawnEditor.Thoughts".Translate()),
            new("ExpiresIn".Translate(), 120),
            new("PawnEditor.Weight".Translate(), 60),
            new(30)
        };

    protected override List<UITable<Pawn>.Row> GetRows(Pawn pawn)
    {
        PawnNeedsUIUtility.GetThoughtGroupsInDisplayOrder(pawn.needs.mood, NeedsCardUtility.thoughtGroupsPresent);
        var result = new List<UITable<Pawn>.Row>(NeedsCardUtility.thoughtGroupsPresent.Count);
        for (var i = 0; i < NeedsCardUtility.thoughtGroupsPresent.Count; i++)
        {
            var items = new List<UITable<Pawn>.Row.Item>(4);
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

            var label = leadingThought.LabelCap;
            if (NeedsCardUtility.thoughtGroup.Count > 1) label += $" x{NeedsCardUtility.thoughtGroup.Count}";
            items.Add(new(label, Widgets.PlaceholderIconTex, i));

            var durationTicks = thoughtGroup.DurationTicks;
            if (durationTicks > 5 && leadingThought is Thought_Memory thoughtMemory)
            {
                var age = NeedsCardUtility.thoughtGroup.Count == 1
                    ? thoughtMemory.age
                    : NeedsCardUtility.thoughtGroup.Cast<Thought_Memory>().Aggregate(int.MaxValue, (current, memory) => Mathf.Min(current, memory.age));

                items.Add(new((durationTicks - age).ToStringTicksToDays(), durationTicks));
            }
            else items.Add(new("Never".Translate()));

            var moodOffset = pawn.needs.mood.thoughts.MoodOffsetOfGroup(thoughtGroup);
            items.Add(new(moodOffset.ToString("##0")
               .Colorize(moodOffset switch
                {
                    0f => NeedsCardUtility.NoEffectColor,
                    > 0f => NeedsCardUtility.MoodColor,
                    _ => NeedsCardUtility.MoodColorNegative
                }), Mathf.RoundToInt(moodOffset)));

            if (NeedsCardUtility.thoughtGroup.OfType<Thought_Memory>().Any())
                items.Add(new(TexButton.DeleteX, () =>
                {
                    for (var j = NeedsCardUtility.thoughtGroup.Count; j-- > 0;)
                        if (NeedsCardUtility.thoughtGroup[j] is Thought_Memory memory)
                        {
                            NeedsCardUtility.thoughtGroup.RemoveAt(j);
                            pawn.needs.mood.thoughts.memories.RemoveMemory(memory);
                        }

                    table.ClearCache();
                }));
            else items.Add(new());

            result.Add(new(items, GetThoughtTip(pawn, leadingThought, thoughtGroup)));
        }

        return result;
    }
}
