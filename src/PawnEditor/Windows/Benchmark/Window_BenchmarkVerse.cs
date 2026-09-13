using UnityEngine;
using Verse;

namespace PawnEditor;

/// <summary>
///     Same scenario as <see cref="Window_BenchmarkTaffy" />/<see cref="Window_BenchmarkVoid" />
///     (two text blocks, a fixed-size sidebar with a toggle button, and a box that
///     appears/disappears) but built using only RimWorld's own <see cref="Widgets" /> API —
///     manual rect math, no Taffy involved at all. This is the vanilla-immediate-mode baseline the
///     other two are compared against.
/// </summary>
public class Window_BenchmarkVerse : Window
{
    private const float SidebarWidth = 200f;
    private const float ButtonHeight = 30f;
    private const float BoxMinWidth = 400f;
    private const float BoxHeight = 200f;

    private const string LoremIpsum =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
        "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
        "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

    private bool _showBox;

    public Window_BenchmarkVerse()
    {
        resizeable = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var y = inRect.y;

        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft))
        {
            Text.WordWrap = true;
            var text1Height = Text.CalcHeight(LoremIpsum, inRect.width);
            Verse.Widgets.Label(new Rect(inRect.x, y, inRect.width, text1Height), LoremIpsum);
            y += text1Height;
        }

        var dynColY = y;
        var dynColHeight = ButtonHeight;

        if (Verse.Widgets.ButtonText(new Rect(inRect.x, dynColY, SidebarWidth, ButtonHeight), _showBox ? "Hide" : "Show"))
            _showBox = !_showBox;

        if (_showBox)
        {
            var boxWidth = Mathf.Max(BoxMinWidth, SidebarWidth);
            Verse.Widgets.DrawRectFast(new Rect(inRect.x, dynColY + ButtonHeight, boxWidth, BoxHeight), Color.blue);
            dynColHeight += BoxHeight;
        }

        y = dynColY + dynColHeight;

        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft))
        {
            Text.WordWrap = true;
            var text2Height = Text.CalcHeight(LoremIpsum, inRect.width);
            Verse.Widgets.Label(new Rect(inRect.x, y, inRect.width, text2Height), LoremIpsum);
        }
    }
}
