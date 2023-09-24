using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

// ReSharper disable once InconsistentNaming
public class Dialog_SelectPawnTrait : Dialog_SelectThing<Dialog_SelectPawnTrait.TraitInfo>
{
    protected override string PageTitle => "ChooseStuffForRelic".Translate() + " " + "Trait".Translate().ToLower();

    public Dialog_SelectPawnTrait(Pawn pawn) : base(DefDatabase<TraitDef>.AllDefs
        .SelectMany(traitDef =>
            traitDef.degreeDatas.Select(degree => new TraitInfo(traitDef, degree)))
        .ToList(), pawn)
    {
        Listing = new Listing_Thing<TraitInfo>(_quickSearchWidget.filter, ThingList, CurPawn, showIcon: false);
        OnSelected = traitInfo => CurPawn.story.traits.GainTrait(new Trait(traitInfo.Trait.def, traitInfo.TraitDegreeData.degree));
    }

    protected override List<TFilter<TraitInfo>> Filters()
    {
        List<TFilter<TraitInfo>> filters = base.Filters();

        var modSourceDict = new Dictionary<FloatMenuOption, Func<TraitInfo, bool>>();
        LoadedModManager.runningMods
            .Where(m => m.AllDefs.OfType<TraitDef>().Any()).ToList()
            .ForEach(m =>
            {
                string label = m.Name;
                FloatMenuOption option = new FloatMenuOption(label, () => { });
                modSourceDict.Add(option, traitInfo => traitInfo.Trait.def.modContentPack.Name == m.Name);
            });
        filters.Add(new TFilter<TraitInfo>("Source".Translate(), true, modSourceDict, "PawnEditor.SourceDesc".Translate()));

        // For some reason I'm having a lot of trouble getting this one to work...
        // var statFactorDict = new Dictionary<FloatMenuOption, Func<TraitInfo, bool>>();
        // var traitStats = ItemList()
        //     .Where(ti => ti?.TraitDegreeData.statFactors != null)
        //     .SelectMany(ti => ti.TraitDegreeData.statFactors);
        // var uniqueTraitStats = traitStats
        //     .GroupBy(ts => ts.stat.defName)
        //     .Select(group => group.First())
        //     .ToList();
        // uniqueTraitStats
        //     .ForEach(sm =>
        //     {
        //         string label = sm.stat.LabelCap;
        //         FloatMenuOption option = new FloatMenuOption(label, () => ListDirty = true);
        //         statFactorDict.Add(option, traitInfo =>
        //         {
        //             Log.Message(traitInfo.Trait.def.defName);
        //             return traitInfo.TraitDegreeData?.statFactors?.Contains(sm) ?? false;
        //         });
        //     });
        // filters.Add(new ListFilter<TraitInfo>("HasStatFactor", false, statFactorDict));

        return filters;
    }

    protected override void DrawInfoCard(ref Rect inRect)
    {
        base.DrawInfoCard(ref inRect);
        Rect outRect = GenUI.DrawElementStack(inRect, 24, CurPawn.story.traits.TraitsSorted, delegate(Rect r, Trait trait)
        {
            GUI.color = CharacterCardUtility.StackElementBackground;
            GUI.DrawTexture(r, BaseContent.WhiteTex);
            GUI.color = Color.white;
            if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

            if (trait.Suppressed) GUI.color = ColoredText.SubtleGrayColor;
            else if (trait.sourceGene != null) GUI.color = ColoredText.GeneColor;

            Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height), trait.LabelCap);
            if (Mouse.IsOver(r))
            {
                var trLocal = trait;
                TooltipHandler.TipRegion(r, () => trLocal.TipString(CurPawn), r.GetHashCode());
                if (Widgets.ButtonImage(r.RightPartPixels(r.height).ContractedBy(4), TexButton.DeleteX))
                {
                    CurPawn.story.traits.RemoveTrait(trait, true);
                }
            }
        }, trait => Text.CalcSize(trait.LabelCap).x + 10f, 4f, 5f, false);
        inRect.yMin += outRect.height + 16f; // To update inRect
    }


    public class TraitInfo
    {
        public readonly TraitDegreeData TraitDegreeData;
        public readonly Trait Trait;

        public TraitInfo(TraitDef traitDef, TraitDegreeData degree)
        {
            TraitDegreeData = degree;
            Trait = new Trait(traitDef, degree.degree);
        }
    }
}