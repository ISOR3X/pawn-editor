using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace Void.Native;

public static partial class Components
{
    private sealed class CollapsibleState
    {
        public bool Open;
    }

    private static readonly Style CollapsibleHeaderStyle = new()
    {
        justifyContent = TaffyAlignContent.SpaceBetween,
        gap = new TaffyAxes(Dimension.Px(GenUI.GapSmall)),
        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(GenUI.GapTiny), Dimension.Px(0f),
            Dimension.Px(GenUI.GapTiny))
    };

    private static readonly Style CollapsibleBaseStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column
    };

    extension(UIBranch branch)
    {
        /// <summary>
        ///     A section with a title that shows or hides its content when clicked. The caller owns whether it is
        ///     <paramref name="open" />; <paramref name="onToggle" /> reports the new value.
        /// </summary>
        public void Collapsible(
            bool open,
            Action<bool> onToggle,
            string title,
            Action<UIBranch> content,
            Style? style = null,
            string? id = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);
            var resolvedStyle = style == null ? CollapsibleBaseStyle : style.Merge(CollapsibleBaseStyle);

            branch.Div(b =>
            {
                b.Div(inner =>
                    {
                        inner.Text(title, style: new Style { fontSize = GameFont.Tiny });
                        inner.Icon(open ? TexUI.ArrowDown : TexUI.ArrowLeft,
                            size: ComponentSize.Small);
                    }, draw: r =>
                    {
                        Verse.Widgets.DrawHighlightIfMouseover(r);
                        if (Verse.Widgets.ButtonInvisible(r)) onToggle(!open);
                    },
                    style: CollapsibleHeaderStyle);
                if (open) content(b);
            }, style: resolvedStyle, id: key);
        }

        /// <summary>
        ///     The same, remembering whether it is open itself.
        /// </summary>
        public void Collapsible(
            string title,
            Action<UIBranch> content,
            bool defaultOpen = false,
            Style? style = null,
            string? id = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);
            var state = branch.UpsertState(key, () => new CollapsibleState { Open = defaultOpen });

            branch.Collapsible(state.Open, f => state.Open = f, title, content, style, key);
        }
    }
}
