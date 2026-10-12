using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Void.Native;

public static class PawnEditorComponents
{
    private static readonly Style _sectionLabelStyle = new() { color = ColoredText.TipSectionTitleColor, width = Dimension.Px(100f), margin = new TaffyEdges(8f, 0f, 0f, 0f) };
    private static readonly Style _widgetLabelStyle = new() { width = Dimension.Px(100f) };
    private static readonly Style _widgetStyle = new() { display = TaffyDisplay.Flex, alignItems = TaffyAlignItems.Center, gap = new TaffyAxes(16f, 8f) };

    extension(UIBranch branch)
    {
        public UINode SectionLabel(string label,
            Style? style = null, string? id = null, [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);
            var mergedStyle = style == null ? _sectionLabelStyle : style.Merge(_sectionLabelStyle);
            return branch.Text(label, mergedStyle, key);
        }

        public UINode LabeledWidget(string label, Action<UIBranch> builder, Style? style = null, string? id = null, [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);
            var mergedStyle = style == null ? _widgetStyle : style.Merge(_sectionLabelStyle);
            return branch.Div(b =>
            {
                b.Text(label, style: _widgetLabelStyle);
                builder(b);
            }, style: mergedStyle);
        }

        public UINode SectionHeading(string label, string? id = null, [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);

            return branch.Div(b =>
            {
                b.Text(label, style: new Style {color = ColoredText.SubtleGrayColor});
            }, draw: r => Verse.Widgets.DrawLineHorizontal(r.x, r.yMax, r.width, PawnTable.BorderColor));
        }
    }
}
