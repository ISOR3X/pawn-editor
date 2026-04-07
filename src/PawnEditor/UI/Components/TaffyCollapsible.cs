using System.Runtime.CompilerServices;
using RimWorld;
using Taffy;
using Verse;

namespace PawnEditor;

public static partial class TaffyExtensions
{
    private static readonly Dictionary<string, bool> SCollapsibleState = new();

    /// <summary>
    /// Adds a collapsible section with a clickable header row and toggleable content.
    /// The header shows <paramref name="title"/> (GameFont.Tiny) with a collapse/reveal icon on the right.
    /// Clicking anywhere on the header toggles the content.
    /// <code>
    /// col.Collapsible("Filters", inner =>
    /// {
    ///     inner.Input(ref search);
    ///     inner.List(items, drawItem);
    /// });
    /// </code>
    /// </summary>
    public static void Collapsible(
        this TaffyBuilder b,
        string title,
        Action<TaffyBuilder> content,
        bool defaultOpen = true,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        var key = $"{b.ContextKey}:{file}:{line}";
        var isOpen = SCollapsibleState.GetValueOrDefault(key, defaultOpen);

        b.Column(build: col =>
        {
            col.Div(
                new Style { flexDirection = FlexDirection.Row, alignItems = AlignItems.Center },
                draw: r =>
                {
                    Verse.Widgets.DrawHighlightIfMouseover(r);
                    if (Verse.Widgets.ButtonInvisible(r))
                        SCollapsibleState[key] = !isOpen;
                },
                build: row =>
                {
                    row.Text(title, font: GameFont.Tiny, style: new Style { flexGrow = 1f });
                    row.Icon(isOpen ? PawnColumnWorker.SortingIcon : PawnColumnWorker.SortingDescendingIcon);
                });

            if (isOpen)
                content(col);
        });
    }
}