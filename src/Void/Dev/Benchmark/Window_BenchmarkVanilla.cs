using UnityEngine;
using Verse;
using static Void.Dev.Benchmark.BenchmarkContent;

namespace Void.Dev.Benchmark;

/// <summary>
///     The benchmark content drawn with plain <see cref="Verse.Widgets" /> calls and manual rect math, laid out to the
///     same sizes as the two Void versions. Like void_native, it only draws the list rows that are in view.
/// </summary>
public class Window_BenchmarkVanilla() : Window_Benchmark("Vanilla", 0)
{
    private int _clicks;
    private Vector2 _scroll;

    protected override void DrawContents(Rect inRect)
    {
        var width = inRect.width;
        var y = inRect.y;

        using (new TextBlock(GameFont.Medium))
        {
            var height = Text.CalcHeight(Heading, width);
            Verse.Widgets.Label(new Rect(inRect.x, y, width, height), Heading);
            y += height + Gap;
        }

        using (new TextBlock(GameFont.Small))
        {
            foreach (var paragraph in Paragraphs)
            {
                var height = Text.CalcHeight(paragraph, width);
                Verse.Widgets.Label(new Rect(inRect.x, y, width, height), paragraph);
                y += height + Gap;
            }

            foreach (var labels in ButtonRows) y = ButtonRow(labels, inRect.x, y, width) + Gap;
        }

        y = Grid(inRect.x, y, width) + Gap;
        List(new Rect(inRect.x, y, width, ListHeight));
    }

    /// <summary>Buttons sized to their label, wrapping onto a new line when the row is full.</summary>
    /// <returns>The bottom of the row.</returns>
    private float ButtonRow(string[] labels, float left, float top, float width)
    {
        var x = left;
        var y = top;
        foreach (var label in labels)
        {
            var buttonWidth = Text.CalcSize(label).x + GenUI.GapLabel * 2f;
            if (x > left && x + buttonWidth > left + width)
            {
                x = left;
                y += UIUtility.ButtonHeight + Gap;
            }

            if (Verse.Widgets.ButtonText(new Rect(x, y, buttonWidth, UIUtility.ButtonHeight), label)) _clicks++;
            x += buttonWidth + Gap;
        }

        return y + UIUtility.ButtonHeight;
    }

    /// <returns>The bottom of the grid.</returns>
    private static float Grid(float left, float top, float width)
    {
        var cellWidth = (width - (GridColumns - 1) * GridGap) / GridColumns;
        var rows = (Cells.Length + GridColumns - 1) / GridColumns;

        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, false))
        {
            for (var i = 0; i < Cells.Length; i++)
            {
                var cell = new Rect(left + i % GridColumns * (cellWidth + GridGap),
                    top + i / GridColumns * (CellHeight + GridGap), cellWidth, CellHeight);
                Verse.Widgets.DrawBoxSolid(cell, CellColor(i));

                var inner = cell.ContractedBy(CellPadding, 0f);
                Verse.Widgets.Label(inner, Cells[i].Truncate(inner.width));
            }
        }

        return top + rows * CellHeight + (rows - 1) * GridGap;
    }

    private void List(Rect outRect)
    {
        var viewRect = new Rect(0f, 0f, outRect.width - ScrollbarGutter, Rows.Length * RowHeight);
        Verse.Widgets.BeginScrollView(outRect, ref _scroll, viewRect);

        // Only the rows in view, as vanilla UI code normally does.
        var first = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / RowHeight));
        var last = Mathf.Min(Rows.Length - 1, Mathf.CeilToInt((_scroll.y + outRect.height) / RowHeight));

        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, false))
        {
            for (var i = first; i <= last; i++)
            {
                var (label, value) = Rows[i];
                var row = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                DrawRowBackground(row, i);

                var icon = new Rect(row.x + RowPadding, row.y + (RowHeight - IconSize) / 2f, IconSize, IconSize);
                GUI.DrawTexture(icon, TexUI.ArrowRight);

                var valueRect = new Rect(row.xMax - RowPadding - ValueWidth, row.y, ValueWidth, RowHeight);
                var labelRect = new Rect(icon.xMax + RowGap, row.y, valueRect.x - RowGap - (icon.xMax + RowGap),
                    RowHeight);
                Verse.Widgets.Label(labelRect, label.Truncate(labelRect.width));

                Text.Anchor = TextAnchor.MiddleRight;
                Verse.Widgets.Label(valueRect, value.Truncate(valueRect.width));
                Text.Anchor = TextAnchor.MiddleLeft;
            }
        }

        Verse.Widgets.EndScrollView();
    }
}
