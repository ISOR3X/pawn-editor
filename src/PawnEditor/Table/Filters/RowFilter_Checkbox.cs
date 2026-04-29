using Taffy;
using Void;

namespace PawnEditor.Table;

public class RowFilter_Checkbox<T>(Func<T, bool> passes, string label) : RowFilter<T>
{
    private bool _active;

    public override bool Passes(T row)
    {
        return !_active || passes(row);
    }

    public override void DrawFilter(TaffyBuilder builder)
    {
        builder.Item(r =>
            {
                var prev = _active;
                Verse.Widgets.CheckboxLabeled(r, label, ref _active);
                if (prev != _active) MarkDirty();
            },
            new StyleOverride { height = UIUtility.ButtonHeight, width = Dimension.Percent(1f) });
    }
}