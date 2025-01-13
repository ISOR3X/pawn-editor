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
    private static Faction cachedFaction;
    private Vector2 scrollPos;

    protected void DrawPawnTables(Rect inRect, Faction faction)
    {
        inRect.yMin += 8f;
        inRect.xMin += 4f;
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "CaravanColonists".Translate().Colorize(ColoredText.TipSectionTitleColor));

        var height = cachedPawns.Count * 34 + cachedSectionCount * Text.LineHeightOf(GameFont.Medium);
        var viewRect = new Rect(0, 0, inRect.width - 20, height);
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        foreach (var (_, table) in pawnLocationTables) table.OnGUI(viewRect.TakeTopPart(table.Height), faction);
        Widgets.EndScrollView();
    }

    protected static void DoSearch(ref Rect inRect)
    {
        searchWidget.OnGUI(inRect.TakeRightPart(250), static () => { CreateLocationTables(cachedPawns, cachedSections); });
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
                },
                TypePostfix = PawnCategory.Humans.ToString()
            });
    }

    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Faction faction) =>
        Gen.YieldSingle(new FloatMenuOption("PawnEditor.FactionName".Translate(),
            () => { faction.Name = NameGenerator.GenerateName(faction.def.factionNameMaker, NamePlayerFactionDialogUtility.IsValidName); }));

    public static void CheckRecache(Faction faction)
    {
        if (cachedFaction == faction) RecachePawns(faction);
    }

    //Used to recache the pawn list when viewing pawns without a faction
    public static void RecachePawnsWithPawnList(List<Pawn> listOfPawns)
    {
        List<Pawn> noFPawns = listOfPawns;
        colonistList ??= new();
        colonistList.UpdateCache(null, PawnCategory.Humans);
        (cachedPawns, cachedSections, cachedSectionCount) = colonistList.GetLists();
        cachedPawns = noFPawns;
        CreateLocationTables(cachedPawns, cachedSections);
    }
    public static void RecachePawns(Faction faction)
    {
        cachedFaction = faction;
        if (PawnEditor.Pregame)
        {
            cachedPawns = Find.GameInitData.startingAndOptionalPawns;
            cachedSections = Enumerable.Repeat<string>(null, cachedPawns.Count).ToList();
            cachedSections[0] = "StartingPawnsSelected".Translate();
            if (Find.GameInitData.startingPawnCount < cachedSections.Count)
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

    /// <summary>
    /// I think this is where the pawn items rows for the faction overview are made.
    /// </summary>
    /// <param name="pawns"></param>
    /// <param name="sections"></param>
    private static void CreateLocationTables(List<Pawn> pawns, List<string> sections)
    {
        Dictionary<string, List<Pawn>> pawnsByLocation = new();
        var sectionIdx = 0;
        for (var i = 0; i < pawns.Count; i++)
        {
            if (!sections[i].NullOrEmpty()) sectionIdx = i;
            if (!pawnsByLocation.TryGetValue(sections[sectionIdx], out var list)) pawnsByLocation[sections[sectionIdx]] = list = new();
            if (!searchWidget.filter.Matches(pawns[i].Name.ToStringFull)) 
                continue;

            list.Add(pawns[i]);
        }

        pawnLocationTables = pawnsByLocation.SelectValues<string, List<Pawn>, UITable<Faction>>((heading, pawns) =>
            new(GetHeadings(heading), _ => pawns.Select(p => new UITable<Faction>.Row(GetItems(p), p.GetTooltip().text))));
    }

    private static List<UITable<Faction>.Heading> GetHeadings(string heading)
    {
        var headings = new List<UITable<Faction>.Heading>
        {
            new(35), // Icon
            new("PawnEditor.Name".Translate(), textAnchor: TextAnchor.LowerLeft),
            new(TexPawnEditor.GendersTex, 100),
            new("PawnEditor.Age".Translate(), 100),
            new("MarketValueTip".Translate(), 100),
            new(24), // Paste
            new(24), // Copy
            new(24), // Go to
            new(24), // Save
            new(24) // Delete
        };

        if (ModsConfig.BiotechActive) headings.Insert(2, new(XenotypeDefOf.Baseliner.Icon, 100));

        return headings;
    }

    private static IEnumerable<UITable<Faction>.Row.Item> GetItems(Pawn pawn)
    {
        yield return new(PawnEditor.GetPawnTex(pawn, new(25, 25), Rot4.South, cameraZoom: 2f));
        yield return new(pawn.Name.ToStringShort, pawn.Name.ToStringShort.ToCharArray()[0], TextAnchor.MiddleLeft);
        if (ModsConfig.BiotechActive) yield return new(pawn.genes.XenotypeIcon, pawn.genes.Xenotype?.index ?? pawn.genes.CustomXenotype.name.ToCharArray()[0]);
        yield return new(pawn.gender.GetIcon(), (int)pawn.gender);
        yield return new(pawn.ageTracker.AgeNumberString, pawn.ageTracker.AgeBiologicalYears);
        yield return new(pawn.MarketValue.ToStringMoney(), (int)pawn.MarketValue);
        yield return new(TexButton.Paste, () =>
        {
            PawnEditor.Paste(pawn);
            string section = PawnEditor.Pregame
                ? Find.GameInitData.startingAndOptionalPawns.IndexOf(pawn) >= Find.GameInitData.startingPawnCount
                    ? "StartingPawnsLeftBehind"
                       .Translate()
                    : "StartingPawnsSelected".Translate()
                : PawnLister.LocationLabel(colonistList.GetLocation(pawn));
            pawnLocationTables[section].ClearCache();
        }, () => PawnEditor.CanPaste);
        yield return new(TexButton.Copy, () => PawnEditor.Copy(pawn));
        yield return new(TexPawnEditor.GoToPawn, () => PawnEditor.Select(pawn));
        yield return new(TexPawnEditor.Save, () => SaveLoadUtility.SaveItem(pawn, typePostfix: PawnCategory.Humans.ToString()));
        yield return new(TexButton.Delete, () =>
        {
            Find.WindowStack.Add(new Dialog_Confirm("PawnEditor.ReallyDelete".Translate(pawn.NameShortColored), "ConfirmDeleteHuman",
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

                    PawnEditor.Notify_PointsUsed();
                }, true));
        });
    }

    private struct ColonistList : IExposable, ISaveable
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

        public string DefaultFileName() => "Colonists";
    }
}
