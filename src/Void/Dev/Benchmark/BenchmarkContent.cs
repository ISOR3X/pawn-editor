using UnityEngine;
using Verse;

namespace Void.Dev.Benchmark;

/// <summary>
///     The content and sizes all three benchmark windows draw, generated once from a fixed seed so every window and
///     every session shows exactly the same thing.
/// </summary>
internal static class BenchmarkContent
{
    public const string Heading = "Benchmark";

    // Sizes shared by all three windows, so they lay out the same.
    public const float Gap = 8f;
    public const int GridColumns = 4;
    public const float GridGap = 2f;
    public const float CellHeight = 24f;
    public const float CellPadding = 4f;
    public const float ListHeight = 400f;
    public const float RowHeight = 28f;
    public const float RowPadding = 6f;
    public const float RowGap = 6f;
    public const float IconSize = 18f;
    public const float ValueWidth = 60f;

    /// <summary>Room a scroll view keeps free for its scrollbar, the same as Void's.</summary>
    public const float ScrollbarGutter = UIUtility.ScrollBarWidth + GenUI.GapTiny;

    private const int Seed = 1234;

    private static readonly string[] Words =
    [
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod",
        "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "enim", "ad", "minim", "veniam",
        "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi", "aliquip", "ex", "ea", "commodo",
        "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate", "velit", "esse", "cillum"
    ];

    public static readonly string[] Paragraphs;
    public static readonly string[][] ButtonRows;
    public static readonly string[] Cells;
    public static readonly (string label, string value)[] Rows;

    /// <summary>The rows with their position, for list components that hand items to the row builder.</summary>
    public static readonly (int index, string label, string value)[] IndexedRows;

    // The dynamic scenario: everything below is a function of the frame number, so every window shows exactly the
    // same thing in the same frame, and a run is the same every time.

    /// <summary>Frames between changes to the number of dynamic buttons.</summary>
    public const int DynamicStepFrames = 20;

    /// <summary>Frames between changes to the ticker text.</summary>
    public const int TickerFrames = 10;

    private const float ScrollPixelsPerFrame = 24f;

    /// <summary>The labels the dynamic buttons are taken from.</summary>
    public static readonly string[] DynamicLabels;

    static BenchmarkContent()
    {
        var random = new System.Random(Seed);

        string Phrase(int min, int max)
        {
            var count = random.Next(min, max + 1);
            var words = new string[count];
            for (var i = 0; i < count; i++) words[i] = Words[random.Next(Words.Length)];
            words[0] = char.ToUpper(words[0][0]) + words[0].Substring(1);
            return string.Join(" ", words);
        }

        Paragraphs = Enumerable.Range(0, 3).Select(_ => Phrase(40, 70) + ".").ToArray();
        ButtonRows = Enumerable.Range(0, 3).Select(_ => Enumerable.Range(0, 5).Select(_ => Phrase(1, 2)).ToArray())
            .ToArray();
        Cells = Enumerable.Range(0, 24).Select(_ => Phrase(1, 3)).ToArray();
        Rows = Enumerable.Range(0, 200).Select(_ => (Phrase(2, 5), random.Next(0, 10000).ToString())).ToArray();
        IndexedRows = Rows.Select((row, i) => (i, row.label, row.value)).ToArray();

        // Generated last, so the content of the static scenario stays what it was.
        DynamicLabels = Enumerable.Range(0, 24).Select(_ => Phrase(1, 2)).ToArray();
    }

    /// <summary>
    ///     How many dynamic buttons there are in <paramref name="frame" />. Buttons are added and removed every
    ///     <see cref="DynamicStepFrames" /> frames, between 3 and 22 of them.
    /// </summary>
    public static int DynamicCount(int frame)
    {
        return 3 + frame / DynamicStepFrames * 7 % 20;
    }

    /// <summary>A text that gets new content every <see cref="TickerFrames" /> frames, so it has to be measured again.</summary>
    public static string Ticker(int frame)
    {
        var tick = frame / TickerFrames;
        return $"Frame {tick * TickerFrames}, value {tick * 7919 % 1000}";
    }

    /// <summary>
    ///     Where the list is scrolled to in <paramref name="frame" />: down to the last row and back up again, at a
    ///     constant speed.
    /// </summary>
    public static float ListScroll(int frame)
    {
        var range = Rows.Length * RowHeight - ListHeight;
        var t = frame * ScrollPixelsPerFrame % (2f * range);
        return t < range ? t : 2f * range - t;
    }

    public static Color CellColor(int index)
    {
        return index % 2 == 0 ? new Color(0.2f, 0.2f, 0.24f) : new Color(0.17f, 0.17f, 0.2f);
    }

    public static void DrawRowBackground(Rect rect, int index)
    {
        if (index % 2 == 0) Verse.Widgets.DrawLightHighlight(rect);
        Verse.Widgets.DrawHighlightIfMouseover(rect);
    }
}
