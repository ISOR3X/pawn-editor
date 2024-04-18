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
        DoHediffs(rect.ContractedBy(4f), pawn);
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

    private void DoBottomOptions(Rect inRect, Pawn pawn)
    {
        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.QuickActions".Translate(), 80f))
        {
            var list = new List<FloatMenuOption>
            {
                new("PawnEditor.TendAll".Translate(), () =>
                {
                    var i = 0;
                    foreach (var hediff in pawn.health.hediffSet.GetHediffsTendable()) hediff.Tended(1, 1, ++i);
                    table.ClearCache();
                }),
                new("PawnEditor.RemoveNegative.Hediffs".Translate(),
                    () =>
                    {
                        var bad = pawn.health.hediffSet.hediffs.Where(hediff => hediff.def.isBad).ToList();
                        foreach (var hediff in bad) pawn.health.RemoveHediff(hediff);
                        table.ClearCache();
                    })
            };
            if (pawn.Dead)
                list.Add(new("PawnEditor.Resurrect".Translate(), () => { ResurrectionUtility.TryResurrect(pawn); }));
            Find.WindowStack.Add(new FloatMenu(list));
        }

        inRect.xMin += 4f;

        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.AddHediff".Translate()))
            Find.WindowStack.Add(new ListingMenu_Hediffs(pawn, table));
        inRect.xMin += 4f;

        Widgets.CheckboxLabeled(inRect, "PawnEditor.ShowHidden.Hediffs".Translate(), ref HealthCardUtility.showAllHediffs,
            placeCheckboxNearText: true);
    }

    private void DoHediffs(Rect inRect, Pawn pawn)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)),
                "PawnEditor.Hediffs".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        inRect.xMin += 4f;
        var viewRect = new Rect(0, 0, inRect.width - 20, table.Height + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        table.OnGUI(viewRect, pawn);
        Widgets.EndScrollView();
    }

    private static void DoCapacities(Rect inRect, Pawn pawn)
    {
        // using (new TextBlock(TextAnchor.MiddleLeft))
        //     Widgets.Label(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "Health".Translate().Colorize(ColoredText.TipSectionTitleColor));
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ColumnWidth /= 2;
        if (pawn.def.race.IsFlesh)
        {
            var rect = listing.GetRect(20);
            var painLabel = HealthCardUtility.GetPainLabel(pawn);
            var painTip = HealthCardUtility.GetPainTip(pawn);
            Widgets.Label(new(rect.x, rect.y, rect.width * 0.65f, 30f),
                "PainLevel".Translate().CapitalizeFirst());
            Widgets.Label(new(rect.x + rect.width * 0.65f, rect.y, rect.width * 0.35f, 30f), painLabel.First.Colorize(painLabel.Second));
            if (Mouse.IsOver(rect))
                TooltipHandler.TipRegion(rect, painTip);
        }

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

            Widgets.Label(new(rect.x, rect.y, rect.width * 0.65f, 30f), pawnCapacityDef.GetLabelFor(pawn).CapitalizeFirst());
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
            // new(32), // Info icon
            new(38), // Icon
            new("PawnEditor.BodyPart".Translate(), 140f, TextAnchor.LowerLeft),
            new("PawnEditor.HediffType".Translate(), 240f, TextAnchor.LowerLeft),
            new("PawnEditor.AdditionalInfo".Translate(), textAnchor: TextAnchor.LowerLeft),
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
            // items.Add(new(iconsRect =>
            // {
            //     iconsRect.x += 4f;
            //     iconsRect = iconsRect.ContractedBy(4f);
            //     Widgets.InfoCardButton(iconsRect, hediff);
            // }));
            items.Add(new(iconsRect =>
            {
                iconsRect.width = 32f;
                iconsRect = iconsRect.ContractedBy(4f);
                if (hediff.Bleeding)
                    GUI.DrawTexture(iconsRect.ContractedBy(GenMath.LerpDouble(0.0f, 0.6f, 5f, 0.0f, Mathf.Min(hediff.BleedRate, 1f))),
                        HealthCardUtility.BleedingIcon);
                else
                {
                    GUI.color = hediff.StateIcon.color;
                    GUI.DrawTexture(iconsRect, hediff.StateIcon.texture);
                    GUI.color = Color.white;
                }
            }));
            if (hediff.Part != null)
                items.Add(new(hediff.Part.LabelCap.Colorize(HealthUtility.GetPartConditionLabel(pawn, hediff.Part).Second), hediff.Part.Index,
                    TextAnchor.MiddleLeft));
            else
                items.Add(new("WholeBody".Translate().Colorize(HealthUtility.RedColor), textAnchor: TextAnchor.MiddleLeft));
            items.Add(new(hediff.LabelCap, hediff.LabelColor,
                Mathf.RoundToInt(HealthCardUtility.GetListPriority(hediff.Part)), TextAnchor.MiddleLeft));
            items.Add(new(infoRect =>
            {
                using (new TextBlock(TextAnchor.MiddleLeft))
                {
                    var immunizable = hediff.TryGetComp<HediffComp_Immunizable>();
                    if (immunizable != null)
                        Widgets.Label(infoRect, $"{hediff.Severity:0.##}% (immunity {immunizable.Immunity:0.##}%)".Colorize(ColoredText.SubtleGrayColor));
                }
            }));
            items.Add(new(editRect => EditUtility.EditButton(editRect, hediff, pawn, table)));
            items.Add(new(TexButton.Delete, () =>
            {
                pawn.health.RemoveHediff(hediff);
                pawn.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
                ClearCacheFor<TabWorker_Needs>();
                PawnEditor.Notify_PointsUsed();
                table.ClearCache();
            }));

            result.Add(new(items, hediff.GetTooltip(pawn, HealthCardUtility.showHediffsDebugInfo)));
        }

        return result;
    }
}