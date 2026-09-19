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
                        if (Widgets.ButtonInvisible(r))
                            state.Open = !state.Open;
                    }, builder: inner =>
                    {
                        inner.Icon(state.Open ? PawnColumnWorker.SortingIcon : PawnColumnWorker.SortingDescendingIcon,
                            size: ComponentSize.Small);
                        inner.Text(title);
                    },
                    style: new Style
                    {
                        gap = new TaffyAxes(Dimension.Px(GenUI.GapSmall)),
                        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(GenUI.GapTiny), Dimension.Px(0f),
                            Dimension.Px(GenUI.GapTiny))
                    });
                if (state.Open) content(b);
            }, style: resolvedStyle, id: key);
        }
    }
}
