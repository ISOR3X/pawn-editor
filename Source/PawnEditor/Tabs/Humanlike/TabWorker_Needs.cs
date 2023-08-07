using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_Needs : TabWorker<Pawn>
{
    private Vector2 needsScrollPos;

    private Vector2 thoughtsScrollPos;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);

        headerRect.xMin += 7;
        DrawQuickActions(headerRect, pawn);
        DrawNeeds(rect.TakeLeftPart(225f), pawn);
        DrawThoughts(rect, pawn);
    }

    private static void DrawQuickActions(Rect inRect, Pawn pawn)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ColumnWidth /= 4;
        listing.Label("PawnEditor.QuickActions".Translate().AsTipTitle());
        listing.Indent(3);
        if (listing.ButtonText("PawnEditor.RefillNeeds".Translate()))
            foreach (var need in pawn.needs.AllNeeds)
                need.CurLevelPercentage = 1f;
        if (listing.ButtonText("PawnEditor.CancelBreakdown".Translate())) pawn.mindState.mentalStateHandler.CurState?.RecoverFromState();
        listing.End();
    }

    private void DrawNeeds(Rect inRect, Pawn pawn)
    {
        NeedsCardUtility.UpdateDisplayNeeds(pawn);
        var viewRect = new Rect(0, 0, inRect.width - 20, NeedsCardUtility.displayNeeds.Count * 70);
        var oldDevMode = Prefs.DevMode;
        var oldGodMode = DebugSettings.godMode;
        Prefs.DevMode = true;
        DebugSettings.godMode = true;
        Widgets.BeginScrollView(inRect, ref needsScrollPos, viewRect);
        for (var i = 0; i < NeedsCardUtility.displayNeeds.Count; i++) NeedsCardUtility.displayNeeds[i].DrawOnGUI(viewRect.TakeTopPart(70));
        Widgets.EndScrollView();
        Prefs.DevMode = oldDevMode;
        DebugSettings.godMode = oldGodMode;
    }

    private void DrawThoughts(Rect inRect, Pawn pawn)
    {
        const int buttonsWidth = 30;
        const int weightWidth = 60;
        const int expiresWidth = 120;

        inRect.xMin += 6;

        var buttonsRect = inRect.TakeBottomPart(40);

        PawnNeedsUIUtility.GetThoughtGroupsInDisplayOrder(pawn.needs.mood, NeedsCardUtility.thoughtGroupsPresent);
        var viewRect = new Rect(0, 0, inRect.width - 20, NeedsCardUtility.thoughtGroupsPresent.Count * 30 + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref thoughtsScrollPos, viewRect);
        var headerRect = viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Medium));

        headerRect.xMax -= buttonsWidth;

        using (new TextBlock(TextAnchor.LowerCenter))
        {
            Widgets.Label(headerRect.TakeRightPart(weightWidth), "PawnEditor.Weight".Translate());
            Widgets.Label(headerRect.TakeRightPart(expiresWidth), "ExpiresIn".Translate());
        }

        using (new TextBlock(GameFont.Medium)) Widgets.Label(headerRect, "PawnEditor.Thoughts".Translate());

        viewRect.yMin += 2;
        GUI.color = Widgets.SeparatorLineColor;
        Widgets.DrawLineHorizontal(viewRect.x, viewRect.y - 1, viewRect.width);
        GUI.color = Color.white;
        for (var i = 0; i < NeedsCardUtility.thoughtGroupsPresent.Count; i++)
        {
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

            var rect = viewRect.TakeTopPart(30);
            rect.xMin += 10;
            var fullRect = new Rect(rect);
            if (i % 2 == 1) Widgets.DrawLightHighlight(fullRect);
            if (Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(2.5f), TexButton.DeleteX))
            {
                for (var j = NeedsCardUtility.thoughtGroup.Count; j-- > 0;)
                    if (NeedsCardUtility.thoughtGroup[j] is Thought_Memory memory)
                    {
                        NeedsCardUtility.thoughtGroup.RemoveAt(j);
                        pawn.needs.mood.thoughts.memories.RemoveMemory(memory);
                    }

                if (NeedsCardUtility.thoughtGroup.Count == 0) continue;
            }

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(fullRect);
                TooltipHandler.TipRegion(fullRect, GetThoughtTip(pawn, leadingThought, thoughtGroup));
            }

            rect.xMin += 2;
            GUI.DrawTexture(rect.TakeLeftPart(30).ContractedBy(10f), BaseContent.GreyTex);

            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                var moodOffset = pawn.needs.mood.thoughts.MoodOffsetOfGroup(thoughtGroup);
                Widgets.Label(rect.TakeRightPart(weightWidth), moodOffset.ToString("##0")
                   .Colorize(moodOffset switch
                    {
                        0f => NeedsCardUtility.NoEffectColor,
                        > 0f => NeedsCardUtility.MoodColor,
                        _ => NeedsCardUtility.MoodColorNegative
                    }));
                var expiresRect = rect.TakeRightPart(expiresWidth);
                var durationTicks = thoughtGroup.DurationTicks;
                if (durationTicks > 5 && leadingThought is Thought_Memory thought_Memory)
                {
                    var age = int.MaxValue;
                    if (NeedsCardUtility.thoughtGroup.Count == 1)
                        age = thought_Memory.age;
                    else
                        foreach (var thought in NeedsCardUtility.thoughtGroup)
                        {
                            var thought_Memory2 = (Thought_Memory)thought;
                            age = Mathf.Min(age, thought_Memory2.age);
                        }

                    Widgets.Label(expiresRect, (durationTicks - age).ToStringTicksToDays());
                }
                else
                    Widgets.Label(expiresRect, "Never".Translate());
            }

            var label = leadingThought.LabelCap;
            if (NeedsCardUtility.thoughtGroup.Count > 1) label += $" x{NeedsCardUtility.thoughtGroup.Count}";
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(rect, label);
        }

        Widgets.EndScrollView();

        if (Widgets.ButtonText(buttonsRect.TakeLeftPart(150).ContractedBy(5), "PawnEditor.AddThought".Translate())) { }

        if (Widgets.ButtonText(buttonsRect.TakeLeftPart(250).ContractedBy(5), "PawnEditor.RemoveNegative".Translate()))
        {
            var memories = pawn.needs.mood.thoughts.memories;
            for (var i = memories.Memories.Count; i-- > 0;)
                if (memories.Memories[i].MoodOffset() < 0)
                    memories.RemoveMemory(memories.Memories[i]);
        }
    }

    private string GetThoughtTip(Pawn pawn, Thought leadingThought, Thought group)
    {
        var stringBuilder = new StringBuilder();
        if (pawn.DevelopmentalStage.Baby())
        {
            stringBuilder.AppendLine(leadingThought.BabyTalk);
            stringBuilder.AppendLine();
            stringBuilder.AppendTagged(("Translation".Translate() + ": " + leadingThought.Description).Colorize(ColoredText.SubtleGrayColor));
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
                    stringBuilder.AppendTagged("ThoughtExpiresIn".Translate((durationTicks - thought_Memory.age).ToStringTicksToPeriod()));
                else
                {
                    var num = int.MaxValue;
                    var num2 = int.MinValue;
                    foreach (var thought in NeedsCardUtility.thoughtGroup)
                    {
                        var thought_Memory2 = (Thought_Memory)thought;
                        num = Mathf.Min(num, thought_Memory2.age);
                        num2 = Mathf.Max(num2, thought_Memory2.age);
                    }

                    stringBuilder.AppendTagged("ThoughtStartsExpiringIn".Translate((durationTicks - num2).ToStringTicksToPeriod()));
                    stringBuilder.AppendLine();
                    stringBuilder.AppendTagged("ThoughtFinishesExpiringIn".Translate((durationTicks - num).ToStringTicksToPeriod()));
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
}
