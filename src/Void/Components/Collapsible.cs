using System.Runtime.CompilerServices;
using RimWorld;
using Taffy;
using Verse;
using Void.Taffy;

public static partial class VoidComponents
{
    private sealed class CollapsibleState
    {
        public bool Open;
    }

    extension(UIBranch branch)
    {
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

            var resolvedStyle = (style ?? new Style()).Merge(new Style
            {
                display = TaffyDisplay.Flex,
                flexDirection = TaffyFlexDirection.Column
            });

            branch.Div(b =>
            {
                b.Div(draw: r =>
                    {
                        Widgets.DrawHighlightIfMouseover(r);
                        if (Widgets.ButtonInvisible(r)) onToggle(!open);
                    }, builder: inner =>
                    {
                        inner.Icon(open ? PawnColumnWorker.SortingIcon : PawnColumnWorker.SortingDescendingIcon,
                            size: ComponentSize.Small);
                        inner.Text(title);
                    },
                    style: new Style
                    {
                        gap = new TaffyAxes(Dimension.Px(GenUI.GapSmall)),
                        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(GenUI.GapTiny), Dimension.Px(0f),
                            Dimension.Px(GenUI.GapTiny))
                    });
                if (open) content(b);
            }, style: resolvedStyle, id: key);
        }

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
