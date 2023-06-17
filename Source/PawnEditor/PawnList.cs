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
        var height = pawns.Count * 60f + sectionCount * 20f + 10f;

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
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            var portraitSize = Page_ConfigureStartingPawns.PawnSelectorPortraitSize;
            GUI.DrawTexture(new Rect(105f - portraitSize.x / 2f, 40f - portraitSize.y / 2f, portraitSize.x, portraitSize.y),
                PortraitsCache.Get(pawn, portraitSize, Rot4.South));
            GUI.color = Color.white;
            var label = pawn.Name is NameTriple nameTriple ? nameTriple.Nick.NullOrEmpty() ? nameTriple.First : nameTriple.Nick : pawn.LabelShort;
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(rect.TopPart(0.5f).Rounded(), label);
                if (pawn.story != null)
                    Widgets.Label(rect.BottomPart(0.5f).Rounded(),
                        Text.CalcSize(pawn.story.TitleCap).x > rect.width ? pawn.story.TitleShortCap : pawn.story.TitleCap);
            }

            if (Mouse.IsOver(rect))
            {
                var deleteRect = rect.RightPartPixels(16f).TopPartPixels(16f);
                if (Widgets.ButtonImage(deleteRect, TexButton.CloseXSmall, Color.red))
                {
                    var index = i;
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PawnEditor.ReallyDelete".Translate(pawn.NameShortColored),
                        () =>
                        {
                            onDelete(pawn);
                            pawns.RemoveAt(index);
                            sections.RemoveAt(index);
                        }, true));
                }
                else if (Event.current.type == EventType.MouseDown)
                {
                    selectedPawn = pawn;
                    showFactionInfo = false;
                }
            }

            Widgets.EndGroup();

            if (ReorderableWidget.Reorderable(reorderableGroupID, outerRect))
                Widgets.DrawRectFast(outerRect.ContractedBy(3), Widgets.WindowBGFillColor * new Color(1f, 1f, 1f, 0.5f));
        }

        using (new TextBlock(GameFont.Tiny))
            Widgets.Label(viewRect.TakeTopPart(20f), "DragToReorder".Translate().Colorize(ColoredText.SubtleGrayColor));

        Widgets.EndScrollView();
    }
}
