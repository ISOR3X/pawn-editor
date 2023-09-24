using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class TabWorker_FactionOverview : TabWorker<Faction>
{
    private static List<Pawn> cachedPawns;
    private static List<string> cachedSections;
    private static int cachedSectionCount;
    private static PawnLister colonistList;
    private static Dictionary<string, UITable<Faction>> pawnLocationTables;
    private static readonly QuickSearchWidget searchWidget = new();
    private Vector2 scrollPos;

    protected void DrawPawnTables(Rect inRect, Faction faction)
    {
        var height = cachedPawns.Count * 34 + cachedSectionCount * Text.LineHeightOf(GameFont.Medium);
        var viewRect = new Rect(0, 0, inRect.width - 20, height);
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        foreach (var (_, table) in pawnLocationTables) table.OnGUI(viewRect.TakeTopPart(table.Height), faction);
        Widgets.EndScrollView();
    }

    protected static void DoSearch(ref Rect inRect)
    {
        searchWidget.OnGUI(inRect.TakeRightPart(250).ContractedBy(5), static () => { CreateLocationTables(cachedPawns, cachedSections); });
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

    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Faction faction) =>
        Gen.YieldSingle(new FloatMenuOption("PawnEditor.FactionName".Translate(),
            () => { faction.Name = NameGenerator.GenerateName(faction.def.factionNameMaker, NamePlayerFactionDialogUtility.IsValidName); }));

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
