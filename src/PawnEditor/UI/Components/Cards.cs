using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Void;
using Void.Extensions;

namespace PawnEditor;

public static class Widgets
{
    // TODO: Convert to TaffyPawnCard?

    private const float CardGap = 8f;
    private const float SectionGap = 8f;
    public static readonly Vector2 CardSize = new(132f, 52f);

    private static readonly Dictionary<int, int>
        ReorderableGroupIDs =
            new(); // A dictionary is used instead of a list for the case that there are multiple reorderable groups.

    private static Vector2 _cardScrollPosition;

    public static void DrawPawnCard(Rect inRect, Pawn pawn, bool highlight, out bool isHovered)
    {
        string name = pawn.def.LabelCap;
        string? lowerLabel = null;
        if (pawn.Name != null)
        {
            if (pawn.Name is NameTriple nameTriple)
                name = string.IsNullOrEmpty(nameTriple.Nick) ? nameTriple.First : nameTriple.Nick;
            else
                name = Text.CalcSize(pawn.Name.ToStringFull).x > inRect.width - 8f
                    ? pawn.Name.ToStringShort
                    : pawn.Name.ToStringFull;
            lowerLabel = pawn.def.LabelCap;
        }

        if (pawn.story != null)
            lowerLabel = Text.CalcSize(pawn.story.TitleCap).x > inRect.width - 8f
                ? pawn.story.TitleShortCap
                : pawn.story.TitleCap;

        var rotation = pawn.RaceProps.Humanlike ? Rot4.South : Rot4.East;
        var cameraZoom = pawn.def.race.baseBodySize > 1f ? 1.7f / pawn.def.race.baseBodySize : 1f;
        Texture texture = PortraitsCache.Get(pawn, Page_ConfigureStartingPawns.PawnSelectorPortraitSize, rotation,
            cameraZoom: cameraZoom);
        DrawCard(inRect, name, lowerLabel, highlight, texture, out isHovered);
    }

    public static void DrawCard(Rect inRect, string upperLabel, string? lowerLabel, bool highlight, Texture texture,
        out bool isHovered)
    {
        isHovered = Mouse.IsOver(inRect);

        Verse.Widgets.BeginGroup(inRect.ExpandedBy(4f));
        var innerRect = inRect with { x = 4f, y = 4f };
        Verse.Widgets.DrawOptionBackground(innerRect, highlight);
        innerRect = innerRect.ContractedBy(4f, 2f);
        var size = Page_ConfigureStartingPawns.PawnSelectorPortraitSize;
        var portraitRect = new Rect(innerRect.xMax - innerRect.height + 2f, innerRect.y, innerRect.height,
            innerRect.height);
        Verse.Widgets.BeginGroup(portraitRect);
        portraitRect = portraitRect.AtZero();

        portraitRect = new Rect(portraitRect.x - 10f, portraitRect.y - 20f, size.x, size.y);
        using (new GUIColor(new Color(1f, 1f, 1f, 0.2f)))
        {
            GUI.DrawTexture(portraitRect.ContractedBy(8f, 12f), texture);
        }

        Verse.Widgets.EndGroup();
        Verse.Widgets.Label(innerRect.TakeTopPart(Text.LineHeight), upperLabel);
        Verse.Widgets.Label(innerRect.TakeBottomPart(Text.LineHeight), lowerLabel);

        MouseoverSounds.DoRegion(innerRect);
        Verse.Widgets.EndGroup();
    }


    // TODO: Clean up
    public static void DrawReorderablePawnList(Rect inRect, List<Pawn> pawns, Pawn? selectedPawn,
        out Pawn? newSelectedPawn)
    {
        newSelectedPawn = selectedPawn;
        var pawnsByLocation = PawnUtility.GroupByLocation(pawns, PawnLister.AllLocations);
        var height = pawns.Count * (CardSize.y + CardGap); // Height of all cards
        var singleSectionHeight = SectionGap + Text.LineHeightOf(GameFont.Tiny) * 2; // Height of a single section
        height += pawnsByLocation.Sum(p =>
            p.Value.NullOrEmpty() ? singleSectionHeight + (8f + CardGap) : singleSectionHeight);
        height -= SectionGap; // Remove the last section gap
        var viewRect = new Rect(inRect.x, inRect.y, inRect.width - UIUtility.ScrollBarWidth, height);
        Verse.Widgets.BeginScrollView(inRect, ref _cardScrollPosition, viewRect);
        for (var i = 0; i < pawnsByLocation.Count; i++)
        {
            var (key, pawnsAtLocation) = pawnsByLocation.ElementAt(i);
            using (new TextBlock(GameFont.Tiny))
            {
                Verse.Widgets.Label(inRect.TakeTopPart(Text.LineHeight), key.Label.TruncateWithTooltip(inRect));
            }

            var locationRect = inRect.TakeTopPart(pawnsAtLocation.Count * (CardSize.y + CardGap));

            if (Event.current.type == EventType.Repaint)
                ReorderableGroupIDs[i] = ReorderableWidget.NewGroup(
                    (from, to) => OnReorder(from, to, ref pawnsAtLocation), ReorderableDirection.Vertical, Rect.zero,
                    CardGap);

            if (pawnsAtLocation
                .NullOrEmpty()) // Add a reorderable widget for empty sections that pawns can be dragged to.
            {
                var r = inRect.TakeTopPart(8f) with { width = CardSize.x, x = locationRect.x + 4f };
                Verse.Widgets.DrawRectFast(r, new Color(1, 1, 1, 0.1f));
                ReorderableWidget.Reorderable(i, r);
                inRect.yMin += CardGap;
            }

            if (!pawnsAtLocation.NullOrEmpty()) pawnsAtLocation.SortBy(p => (int)PawnUtility.GetPawnCategory(p));
            foreach (var pawn in pawnsAtLocation)
            {
                var outerCardRect = locationRect.TakeTopPart(CardSize.y) with { width = CardSize.x };
                outerCardRect.x += 4f;
                DrawPawnCard(outerCardRect, pawn, pawn == selectedPawn, out _);

                if (Mouse.IsOver(outerCardRect))
                {
                    var deleteRect = outerCardRect.TopPartPixels(Verse.Widgets.InfoCardButtonSize)
                        .RightPartPixels(Verse.Widgets.InfoCardButtonSize);
                    if (Verse.Widgets.ButtonImage(deleteRect, TexButton.Delete))
                    {
                        pawns.Remove(pawn);
                        pawn.FullDelete();
                        if (pawn == selectedPawn) newSelectedPawn = pawnsAtLocation.FirstOrDefault(p => p != pawn);
                        break;
                    }

                    if (Event.current.type == EventType.MouseDown)
                    {
                        var currentMap = Find.CurrentMap;
                        if (Event.current.button == 0 && Event.current.clickCount == 2)
                        {
                            if (pawn.Map == currentMap || pawn.MapHeld == currentMap)
                            {
                                var pos = pawn.Position;
                                // pos.x -= 15; // To have the pawn show up next to the window instead of the center of the screen.
                                CameraJumper.TryJump(pos, pawn.Map ?? pawn.Map);
                                Find.Selector.ClearSelection();
                                Find.Selector.Select(pawn);
                            }
                            else
                            {
                                Messages.Message("Cannot jump to a pawn that is not on the current map.",
                                    MessageTypeDefOf.RejectInput);
                            }
                        }
                        else
                        {
                            newSelectedPawn = pawn;
                            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        }
                    }

                    var tooltip = pawn.GetTooltip().text;
                    tooltip += "\n\n" +
                               "Click to select this pawn. Double-click to jump to their location (if they are on the current map). \n\nDrag to a new location to teleport."
                                   .Colorize(ColoredText.SubtleGrayColor);
                    TooltipHandler.TipRegion(outerCardRect, tooltip);
                }

                ReorderableWidget.Reorderable(i, outerCardRect);
                locationRect.yMin += CardGap;
            }

            // var addRect = inRect.TakeTopPart(Text.LineHeightOf(GameFont.Tiny)) with { width = CardSize.x };
            // addRect.x += 4f;
            // Verse.Widgets.ButtonText(addRect, "+");

            inRect.yMin += SectionGap;
        }

        ReorderableWidget.NewMultiGroup(ReorderableGroupIDs.Values.ToList(),
            (from, fromGroup, to, toGroup) => OnReorderMulti(from, fromGroup, to, toGroup, ref pawnsByLocation));
        Verse.Widgets.EndScrollView();
        return;

        static void OnReorder(int from, int to, ref List<Pawn> _)
        {
            Messages.Message("From: " + from + " To: " + to, MessageTypeDefOf.NeutralEvent);
        }

        static void OnReorderMulti(int from, int fromGroup, int to, int toGroup,
            ref SortedDictionary<PawnLocation, List<Pawn>> values)
        {
            // Messages.Message($"From: {from} ({fromGroup}) To: {to} ({toGroup})", MessageTypeDefOf.NeutralEvent);
            var fromGroupItems = values.Values.ElementAt(fromGroup);
            var toGroupItems = values.Values.ElementAt(toGroup);
            var fromPawn = fromGroupItems[from];
            var toPawn = toGroupItems.Count > to ? toGroupItems[to] : null;
            if (toPawn != null)
                PawnUtility.TeleportTo(fromPawn, toPawn);
            else
                PawnUtility.TeleportTo(fromPawn, values.Keys.ElementAt(toGroup));
        }
    }
}