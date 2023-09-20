using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_PlayerFactionOverview : TabWorker<Faction>
{
    private static List<Pawn> cachedPawns;
    private static List<string> cachedSections;
    private static int cachedSectionCount;
    private static PawnLister colonistList;
    private static Dictionary<string, UITable<Faction>> pawnLocationTables;
    private static List<SkillDef> skillsForSummary;
    private static readonly QuickSearchWidget searchWidget = new();
    private static int skillsPerColumn = -1;
    private Vector2 scrollPos;

    public override void Initialize()
    {
        base.Initialize();
        var allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
        for (var i = 0; i < allDefsListForReading.Count; i++)
        {
            var x = Text.CalcSize(allDefsListForReading[i].skillLabel.CapitalizeFirst()).x;
            if (x > SkillUI.levelLabelWidth) SkillUI.levelLabelWidth = x;
        }
    }

    public override void DrawTabContents(Rect inRect, Faction faction)
    {
        DoHeading(inRect.TakeTopPart(35), faction);
        DoTeamSkills(inRect.TakeBottomPart(120).ContractedBy(5), faction);
        DoBottomButtons(inRect.TakeBottomPart(40));

        var height = cachedPawns.Count * 34 + cachedSectionCount * Text.LineHeightOf(GameFont.Medium);
        var viewRect = new Rect(0, 0, inRect.width - 20, height);
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        foreach (var (_, table) in pawnLocationTables) table.OnGUI(viewRect.TakeTopPart(table.Height), faction);
        Widgets.EndScrollView();
    }

    public static void RecachePawns(Faction faction)
    {
        if (PawnEditor.Pregame)
        {
            cachedPawns = Find.GameInitData.startingAndOptionalPawns;
            cachedSections = Enumerable.Repeat<string>(null, cachedPawns.Count).ToList();
            cachedSections[0] = "StartingPawnsSelected".Translate();
            cachedSections[Find.GameInitData.startingPawnCount] = "StartingPawnsLeftBehind".Translate();
            cachedSectionCount = 2;
        }
        else
        {
            colonistList ??= new();
            colonistList.UpdateCache(faction, PawnCategory.Humans);
            (cachedPawns, cachedSections, cachedSectionCount) = colonistList.GetLists();
        }

        CreateLocationTables(cachedPawns, cachedSections);
    }

    private static void CreateLocationTables(List<Pawn> pawns, List<string> sections)
    {
        Dictionary<string, List<Pawn>> pawnsByLocation = new();
        var sectionIdx = 0;
        for (var i = 0; i < pawns.Count; i++)
        {
            if (!sections[i].NullOrEmpty()) sectionIdx = i;
            if (!pawnsByLocation.TryGetValue(sections[sectionIdx], out var list)) pawnsByLocation[sections[sectionIdx]] = list = new();
            if (!searchWidget.filter.Matches(pawns[i].Name.ToStringFull)) continue;
            list.Add(pawns[i]);
        }

        pawnLocationTables = pawnsByLocation.SelectValues<string, List<Pawn>, UITable<Faction>>((heading, pawns) =>
            new(GetHeadings(heading), _ => pawns.Select(p => new UITable<Faction>.Row(GetItems(p), p.GetTooltip().text))));
    }

    private static List<UITable<Faction>.Heading> GetHeadings(string heading) =>
        new()
        {
            new(heading),
            new(XenotypeDefOf.Baseliner.Icon),
            new(TexPawnEditor.GendersTex),
            new("PawnEditor.Age".Translate()),
            new("MarketValueTip".Translate()),
            new(100),
            new(30),
            new(30),
            new(30),
            new(30)
        };

    private static IEnumerable<UITable<Faction>.Row.Item> GetItems(Pawn pawn)
    {
        yield return new(pawn.Name.ToStringShort, PawnEditor.GetPawnTex(pawn, new(25, 25), Rot4.South));
        yield return new(pawn.genes.XenotypeIcon, pawn.genes.Xenotype?.index ?? pawn.genes.CustomXenotype.name.ToCharArray()[0]);
        yield return new(pawn.gender.GetIcon(), (int)pawn.gender);
        yield return new(pawn.ageTracker.AgeNumberString, pawn.ageTracker.AgeBiologicalYears);
        yield return new(pawn.MarketValue.ToStringMoney(), (int)pawn.MarketValue);
        yield return new("Edit".Translate() + "...", () => PawnEditor.Select(pawn));
        yield return new(TexButton.Save, () => { SaveLoadUtility.SaveItem(pawn); });
        yield return new(TexButton.Copy, () => { });
        yield return new(TexButton.Paste, () => { });
        yield return new(TexButton.DeleteX, () =>
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PawnEditor.ReallyDelete".Translate(pawn.NameShortColored),
                () =>
                {
                    pawn.Discard(true);
                    if (PawnEditor.Pregame)
                        Find.GameInitData.startingAndOptionalPawns.Remove(pawn);
                    else
                    {
                        var (pawns, sections, _) = PawnEditor.PawnList.GetLists();
                        PawnEditor.PawnList.OnDelete(pawn);
                        var i = pawns.IndexOf(pawn);
                        pawns.RemoveAt(i);
                        sections.RemoveAt(i);
                    }
                }, true));
        });
    }

    private static void DoBottomButtons(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(150).ContractedBy(5), "PawnEditor.AddColonist".Translate())) PawnEditor.AddPawn(PawnCategory.Humans);
        searchWidget.OnGUI(inRect.TakeRightPart(250).ContractedBy(5), static () => { CreateLocationTables(cachedPawns, cachedSections); });
    }

    private static void DoTeamSkills(Rect inRect, Faction faction)
    {
        Widgets.DrawLightHighlight(inRect);
        inRect = inRect.ContractedBy(7);
        using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "TeamSkills".Translate());
        skillsForSummary ??= DefDatabase<SkillDef>.AllDefsListForReading.Where(sd => sd.pawnCreatorSummaryVisible).ToList();
        if (skillsPerColumn < 0) skillsPerColumn = Mathf.CeilToInt(skillsForSummary.Count / 4f);
        var columnWidth = inRect.width / Mathf.FloorToInt((float)skillsForSummary.Count / skillsPerColumn);

        for (var i = 0; i < skillsForSummary.Count; i++)
        {
            var skillDef = skillsForSummary[i];
            var r = new Rect(inRect.x + columnWidth * Mathf.FloorToInt((float)i / skillsPerColumn), inRect.y + 24 * (i % skillsPerColumn), columnWidth, 24);
            var pawn = FindBestSkillOwner(skillDef, faction);
            SkillUI.DrawSkill(pawn.skills.GetSkill(skillDef), r.Rounded(), SkillUI.SkillDrawMode.Menu,
                pawn.Name.ToString().Colorize(ColoredText.TipSectionTitleColor));
        }
    }

    private static Pawn FindBestSkillOwner(SkillDef skill, Faction faction)
    {
        var map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
        var pawns = PawnEditor.Pregame
            ? Find.GameInitData.startingAndOptionalPawns
            : map?.mapPawns.PawnsInFaction(faction) ?? PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists;
        pawns.RemoveAll(pawn => pawn.skills == null);
        var pawn = pawns[0];
        var skillRecord = pawn.skills.GetSkill(skill);
        for (var i = 1; i < pawns.Count; i++)
        {
            var skill2 = pawns[i].skills.GetSkill(skill);
            if (skillRecord.TotallyDisabled || skill2.Level > skillRecord.Level || (skill2.Level == skillRecord.Level && skill2.passion > skillRecord.passion))
            {
                pawn = pawns[i];
                skillRecord = skill2;
            }
        }

        return pawn;
    }

    private static void DoHeading(Rect inRect, Faction faction)
    {
        var factionName = "PawnEditor.FactionName".Translate();
        var settlementName = "PawnEditor.SettlementName".Translate();
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            var leftLeftWidth = UIUtility.ColumnWidth(6, factionName);
            var rightLeftWidth = UIUtility.ColumnWidth(6, settlementName);
            var factionNameRect = inRect.LeftHalf();
            factionNameRect.width *= 0.75f;
            Widgets.Label(factionNameRect.TakeLeftPart(leftLeftWidth), factionName);
            faction.Name = Widgets.TextField(factionNameRect, faction.Name);

            if (!PawnEditor.Pregame)
            {
                var settlementNameRect = inRect.RightHalf();
                settlementNameRect.width *= 0.75f;
                Widgets.Label(settlementNameRect.TakeLeftPart(rightLeftWidth), settlementName);
                if ((Find.CurrentMap ?? Find.AnyPlayerHomeMap)?.Parent is Settlement settlement)
                    settlement.Name = Widgets.TextField(settlementNameRect, settlement.Name);
            }
        }
    }

    public override IEnumerable<SaveLoadItem> GetSaveLoadItems(Faction faction)
    {
        if (PawnEditor.Pregame)
            yield return new SaveLoadItem<ColonistList>("ColonistsSection".Translate(), new(cachedPawns, cachedSections), new()
            {
                OnLoad = list =>
                {
                    cachedPawns = list.Colonists;
                    cachedSections = list.Sections;
                    cachedSectionCount = cachedSections.Count(str => !str.NullOrEmpty());
                    Find.GameInitData.startingAndOptionalPawns = cachedPawns;
                    Find.GameInitData.startingPawnCount = cachedSections.IndexOf("StartingPawnsLeftBehind".Translate());
                    CreateLocationTables(cachedPawns, cachedSections);
                }
            });
    }

    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Faction faction)
    {
        yield return new("PawnEditor.FactionName".Translate(),
            () => { faction.Name = NameGenerator.GenerateName(faction.def.factionNameMaker, NamePlayerFactionDialogUtility.IsValidName); });
        if (!PawnEditor.Pregame && (Find.CurrentMap ?? Find.AnyPlayerHomeMap)?.Parent is Settlement settlement)
            yield return new("PawnEditor.SettlementName".Translate(),
                () => { settlement.Name = NameGenerator.GenerateName(faction.def.settlementNameMaker, NamePlayerSettlementDialogUtility.IsValidName); });
    }

    private struct ColonistList : IExposable
    {
        public List<Pawn> Colonists;
        public List<string> Sections;

        public ColonistList(IEnumerable<Pawn> pawns, IEnumerable<string> sections)
        {
            Colonists = pawns.ToList();
            Sections = sections.ToList();
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Colonists, "colonists", LookMode.Deep);
            Scribe_Collections.Look(ref Sections, "sections", LookMode.Value);
        }
    }
}
