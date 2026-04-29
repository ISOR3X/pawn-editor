using Void;

namespace PawnEditor.Table;

public abstract class RowFilter<TRow>
{
    private Action? _onChanged;

    internal void Setup(IReadOnlyList<TRow> allRows, Action onChanged)
    {
        _onChanged = onChanged;
        Initialize(allRows);
    }

    protected virtual void Initialize(IReadOnlyList<TRow> allRows)
    {
    }

    public abstract bool Passes(TRow row);

    public abstract void DrawFilter(TaffyBuilder builder);

    protected void MarkDirty()
    {
        _onChanged?.Invoke();
    }
}