using System.Drawing;
using System.Runtime.CompilerServices;
using Taffy;
using Verse;
using Void.Taffy;
using static Void.Components.TaffyExtensions;

public static class PawnEditorComponents
{
    private static readonly Style _sectionLabelStyle = new() { color = ColoredText.TipSectionTitleColor, width = Dimension.Px(100f) };

    extension(UIBranch branch)
    {
        public TaffyNode SectionLabel(string label,
            Style? style = null, string? id = null, [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);
            var mergedStyle = style == null ? _sectionLabelStyle : style.Merge(_sectionLabelStyle);
            return branch.Text(label, mergedStyle, key);

        }
    }
}
