using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_Health : TabWorker_Table<Pawn>
{
    private readonly List<Hediff> hediffs = new();
    private Vector2 scrollPos;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);
        headerRect.xMin += 10;
        DoCapacities(headerRect, pawn);
        DoBottomOptions(rect.TakeBottomPart(UIUtility.RegularButtonHeight), pawn);
        DoHediffs(rect, pawn);
    }

    public override IEnumerable<SaveLoadItem> GetSaveLoadItems(Pawn pawn)
    {
        yield return new SaveLoadItem<HediffSet>("PawnEditor.Hediffs".Translate(), pawn.health.hediffSet, new()
        {
            OnLoad = _ => pawn.health.CheckForStateChange(null, null)
        });
    }

    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Pawn pawn)
    {
        yield return new("PawnEditor.Hediffs".Translate(), () => { });
        if (pawn.RaceProps.Humanlike)
            yield return new("PawnEditor.Prosthetics".Translate(), () => { });
    }

    private static void DoBottomOptions(Rect inRect, Pawn pawn)
    {
        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.QuickActions".Translate(), 80f))
            Find.WindowStack.Add(new FloatMenu(new()
            {
                new("PawnEditor.TendAll".Translate(), () =>
                {
                    var i = 0;
                    foreach (var hediff in pawn.health.hediffSet.GetHediffsTendable()) hediff.Tended(1, 1, ++i);
                }),
                new("PawnEditor.RemoveNegative.Hediffs".Translate(),
                    () =>
                    {
                        var bad = pawn.health.hediffSet.hediffs.Where(hediff => hediff.def.isBad).ToList();
                        foreach (var hediff in bad) pawn.health.RemoveHediff(hediff);
                    })
            }));
        inRect.xMin += 4f;
        
        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.AddHediff".Translate()))
            Find.WindowStack.Add(new Dialog_SelectHediff(DefDatabase<HediffDef>.AllDefsListForReading, pawn));
        inRect.xMin += 4f;
        
        Widgets.CheckboxLabeled(inRect, "PawnEditor.ShowHidden.Hediffs".Translate(), ref HealthCardUtility.showAllHediffs,
            placeCheckboxNearText: true);
    }

    private void DoHediffs(Rect inRect, Pawn pawn)
    {
        var viewRect = new Rect(0, 0, inRect.width - 20, hediffs.Count * 30 + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        table.OnGUI(viewRect, pawn);
        Widgets.EndScrollView();
    }

    private static void DoCapacities(Rect inRect, Pawn pawn)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ColumnWidth /= 2;
        IEnumerable<PawnCapacityDef> source;
        if (pawn.def.race.Humanlike)
            source = from x in DefDatabase<PawnCapacityDef>.AllDefs
                where x.showOnHumanlikes
                select x;
        else if (pawn.def.race.Animal)
            source = from x in DefDatabase<PawnCapacityDef>.AllDefs
                where x.showOnAnimals
                select x;
        else
            source = from x in DefDatabase<PawnCapacityDef>.AllDefs
                where x.showOnMechanoids
                select x;

        foreach (var pawnCapacityDef in from act in source
                 where PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, act)
                 orderby act.listOrder
                 select act)
        {
            var capacity = pawnCapacityDef;
            var efficiencyLabel = HealthCardUtility.GetEfficiencyLabel(pawn, pawnCapacityDef);
            var rect = listing.GetRect(20);
            if (Mouse.IsOver(rect))
            {
                GUI.color = HealthCardUtility.HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
                GUI.color = Color.white;
            }

            Widgets.Label(new(rect.x, rect.y, rect.width * 0.65f, 30f),
                pawnCapacityDef.GetLabelFor(pawn.RaceProps.IsFlesh, pawn.RaceProps.Humanlike).CapitalizeFirst());
            Widgets.Label(new(rect.x + rect.width * 0.65f, rect.y, rect.width * 0.35f, 30f), efficiencyLabel.First.Colorize(efficiencyLabel.Second));
            if (Mouse.IsOver(rect))
                TooltipHandler.TipRegion(rect, () => pawn.Dead ? "" : HealthCardUtility.GetPawnCapacityTip(pawn, capacity),
                    pawn.thingIDNumber ^ pawnCapacityDef.index);
        }

        listing.End();
    }

    protected override List<UITable<Pawn>.Heading> GetHeadings() =>
        new()
        {
            new("Health".Translate(), 262),
            new("PawnEditor.HediffType".Translate()),
            new(100),
            new(30)
        };

    protected override List<UITable<Pawn>.Row> GetRows(Pawn pawn)
    {
        hediffs.Clear();
        hediffs.AddRange(HealthCardUtility.VisibleHediffs(pawn, true));
        var result = new List<UITable<Pawn>.Row>(hediffs.Count);
        for (var i = 0; i < hediffs.Count; i++)
        {
            var items = new List<UITable<Pawn>.Row.Item>(4);
            var hediff = hediffs[i];
            items.Add(new(hediff.LabelCap.Colorize(hediff.LabelColor), Widgets.PlaceholderIconTex,
                Mathf.RoundToInt(HealthCardUtility.GetListPriority(hediff.Part))));

            if (hediff.Part != null)
                items.Add(new(hediff.Part.LabelCap.Colorize(HealthUtility.GetPartConditionLabel(pawn, hediff.Part).Second), hediff.Part.Index));
            else
                items.Add(new("WholeBody".Translate().Colorize(HealthUtility.RedColor)));
            items.Add(new("Edit".Translate() + "...",
                () => { Find.WindowStack.Add(new Dialog_SelectHediff(DefDatabase<HediffDef>.AllDefsListForReading, pawn, hediff)); }));
            items.Add(new(TexButton.DeleteX, () =>
            {
                pawn.health.RemoveHediff(hediff);
                table.ClearCache();
            }));

            result.Add(new(items, hediff.GetTooltip(pawn, HealthCardUtility.showHediffsDebugInfo)));
        }

        return result;
    }
}
