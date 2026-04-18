using System.Runtime.CompilerServices;
using RimWorld;
using Taffy;
using Verse;

namespace Void.Components;

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
        StyleOverride? style = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        var key = $"{b.ContextKey}:{file}:{line}";
        var isOpen = SCollapsibleState.GetValueOrDefault(key, defaultOpen);

        var mergedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            flexDirection = FlexDirection.Column
        });

        b.Div(col =>
        {
            col.Div(
                r =>
                {
                    Verse.Widgets.DrawHighlightIfMouseover(r);
                    if (Verse.Widgets.ButtonInvisible(r))
                        SCollapsibleState[key] = !isOpen;
                },
                row =>
                {
                    row.Text(title, font: GameFont.Tiny, style: new StyleOverride { flexGrow = 1f });
                    row.Icon(isOpen ? PawnColumnWorker.SortingIcon : PawnColumnWorker.SortingDescendingIcon,
                        size: UIUtility.ComponentSize.Small);
                },
                new StyleOverride { flexDirection = FlexDirection.Row, alignItems = AlignItems.Center });

            if (isOpen)
                content(col);
        }, mergedStyle);
    }
}