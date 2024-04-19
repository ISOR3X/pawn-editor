using System.Collections.Generic;
using JetBrains.Annotations;
using RimUI;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
[HotSwappable]
public class Dialog_EditRelation : Dialog_EditItem<SocialCardUtility.CachedSocialTabEntry>
{
    private static readonly List<ISocialThought> thoughts = new();
    private readonly UITable<SocialCardUtility.CachedSocialTabEntry> thoughtTable;

    public Dialog_EditRelation(SocialCardUtility.CachedSocialTabEntry item, Pawn pawn = null, UITable<Pawn> table = null) : base(item, pawn, table) =>
        thoughtTable = new(
            new()
            {
                new(35f),
                new("PawnEditor.Thought".Translate(), textAnchor: TextAnchor.LowerLeft),
                new("ExpiresIn".Translate().CapitalizeFirst(), 360),
                new("PawnEditor.Weight".Translate(), 60),
                new(30)
            },
            GetRows
        );

    protected override float MinWidth => 720;

    protected override void DoContents(Listing_Horizontal listing)
    {
        thoughtTable.CheckRecache(listing.ListingRect, Selected); // Need to make sure the rows are up-to-date for the Height to be correct
        thoughtTable.OnGUI(listing.GetRect(height: thoughtTable.Height), Selected);
    }

    private List<UITable<SocialCardUtility.CachedSocialTabEntry>.Row> GetRows(SocialCardUtility.CachedSocialTabEntry entry)
    {
        Pawn.needs.mood.thoughts.GetDistinctSocialThoughtGroups(entry.otherPawn, thoughts);
        var result = new List<UITable<SocialCardUtility.CachedSocialTabEntry>.Row>(thoughts.Count);
        for (var i = 0; i < thoughts.Count; i++)
        {
            var items = new List<UITable<SocialCardUtility.CachedSocialTabEntry>.Row.Item>(5);
            var thought = (Thought)thoughts[i];

            items.Add(new(iconRect =>
            {
                iconRect.xMin += 3f;
                iconRect = iconRect.ContractedBy(2f);

                if (ModsConfig.IdeologyActive)
                    if (thought.sourcePrecept != null)
                    {
                        if (!Find.IdeoManager.classicMode)
                            IdeoUIUtility.DoIdeoIcon(iconRect.ContractedBy(4f), thought.sourcePrecept.ideo, false,
                                () => IdeoUIUtility.OpenIdeoInfo(thought.sourcePrecept.ideo));
                        return;
                    }

                GUI.DrawTexture(iconRect, Widgets.PlaceholderIconTex);
            }));

            var label = thought.LabelCap;
            items.Add(new(label, i, TextAnchor.MiddleLeft));

            var durationTicks = thought.DurationTicks;
            if (durationTicks > 5 && thought is Thought_Memory thoughtMemory)
                items.Add(new(rect =>
                {
                    var progress = Mathf.InverseLerp(durationTicks, 0, thoughtMemory.age);
                    progress = Widgets.HorizontalSlider(rect, progress,
                        0, 1, true, (durationTicks - thoughtMemory.age).ToStringTicksToPeriodVerbose(), "0 " + "SecondsLower".Translate(),
                        durationTicks.ToStringTicksToPeriodVerbose());
                    thoughtMemory.age = (int)Mathf.Lerp(durationTicks, 0, progress);
                }));
            else items.Add(new("Never".Translate().Colorize(ColoredText.SubtleGrayColor)));

            var moodOffset = thoughts[i].OpinionOffset();
            items.Add(new(moodOffset.ToString("##0")
               .Colorize(moodOffset switch
                {
                    0f => NeedsCardUtility.NoEffectColor,
                    > 0f => NeedsCardUtility.MoodColor,
                    _ => NeedsCardUtility.MoodColorNegative
                }), Mathf.RoundToInt(moodOffset)));

            if (thought is Thought_Memory memory)
                items.Add(new(TexButton.Delete, () =>
                {
                    Pawn.needs.mood.thoughts.memories.RemoveMemory(memory);
                    thoughtTable.ClearCache();
                }));
            else items.Add(new());

            result.Add(new(items));
        }

        return result;
    }
}
