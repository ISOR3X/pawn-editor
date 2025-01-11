using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

public static partial class PawnEditor
{
    private static Vector2 pawnListScrollPos;
    private static int reorderableGroupID;

    private static void DoPawnList(Rect inRect, List<Pawn> pawns, List<string> sections, int sectionCount, Action<Pawn, int, int> onReorder,
        Action<Pawn> onDelete)
    {
        sectionCount = sections.Count(s => s != null) + 1;
        var height = pawns.Count * 59f + sectionCount * 20f;

        if (pawns.Count > 0)
        {
            var rect = inRect.TakeBottomPart(20f);
            if (height <= inRect.height)
            {
                inRect.height = height + 20;
                rect = inRect.TakeBottomPart(20f);
                rect.y -= 20;
            }

            using (new TextBlock(GameFont.Tiny)) Widgets.Label(rect, "DragToReorder".Translate().Colorize(ColoredText.SubtleGrayColor));
        }

        var viewRect = new Rect(0, 0, inRect.width, height);
        inRect.xMax += 16f;
        Widgets.BeginScrollView(inRect, ref pawnListScrollPos, viewRect);
        if (Event.current.type == EventType.Repaint)
            reorderableGroupID = ReorderableWidget.NewGroup(delegate(int from, int to)
            {
                var item = pawns[from];
                pawns.Insert(to, item);
                pawns.RemoveAt(from < to ? from : from + 1);
                onReorder(item, from, to);
            }, ReorderableDirection.Vertical, viewRect, -1f, null, false);
        for (var i = 0; i < pawns.Count; i++)
        {
            if (sections[i] != null)
                using (new TextBlock(GameFont.Tiny))
                    Widgets.Label(viewRect.TakeTopPart(20f), sections[i]);

            var pawn = pawns[i];
            var outerRect = viewRect.TakeTopPart(59).ContractedBy(2, 3);
            Widgets.DrawOptionBackground(outerRect, !showFactionInfo && selectedPawn == pawn);
            MouseoverSounds.DoRegion(outerRect);
            Widgets.BeginGroup(outerRect);
            var rect = outerRect.AtZero();
            rect = rect.ContractedBy(5);
            GUI.color = new(1f, 1f, 1f, 0.2f);
            var portraitSize = Page_ConfigureStartingPawns.PawnSelectorPortraitSize;
            GUI.DrawTexture(new(105f - portraitSize.x / 2f, 40f - portraitSize.y / 2f, portraitSize.x, portraitSize.y),
                GetPawnTex(pawn, portraitSize, selectedCategory == PawnCategory.Humans ? Rot4.South : Rot4.East,
                    selectedCategory == PawnCategory.Humans ? default : new Vector3(-0.01f, 0, 0),
                    1 / Mathf.Clamp(pawn.BodySize, 1, 5)));
            GUI.color = Color.white;
            var label = pawn.Name is NameTriple nameTriple ? nameTriple.Nick.NullOrEmpty() ? nameTriple.First : nameTriple.Nick : pawn.LabelShort;
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(rect.TopPart(0.5f).Rounded(), label.Truncate(rect.width));
                if (pawn.story != null)
                    Widgets.Label(rect.BottomPart(0.5f).Rounded(),
                        Text.CalcSize(pawn.story.TitleCap).x > rect.width ? pawn.story.TitleShortCap : pawn.story.TitleCap);
                else
                    Widgets.Label(rect.BottomPart(0.5f).Rounded(), pawn.KindLabel.CapitalizeFirst());
            }

            if (Mouse.IsOver(rect))
            {
                var deleteRect = rect.RightPartPixels(16f).TopPartPixels(16f);
                if (Widgets.ButtonImage(deleteRect, TexButton.CloseXSmall, Color.red))
                {
                    var index = i;
                    Find.WindowStack.Add(new Dialog_Confirm("PawnEditor.ReallyDelete".Translate(pawn.NameShortColored), "ConfirmDeletePawn",
                        () =>
                        {
                            onDelete(pawn);
                            if (!Pregame)
                                pawns.RemoveAt(index);
                            sections.RemoveAt(index);
                        }, true));
                }
                else if (Event.current.type == EventType.MouseDown)
                {
                    selectedPawn = pawn;
                    showFactionInfo = false;
                    CheckChangeTabGroup();
                }
            }

            Widgets.EndGroup();

            if (ReorderableWidget.Reorderable(reorderableGroupID, outerRect))
                Widgets.DrawRectFast(outerRect.ContractedBy(3), Widgets.WindowBGFillColor * new Color(1f, 1f, 1f, 0.5f));
        }

        Widgets.EndScrollView();
    }
}