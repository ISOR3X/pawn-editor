using Void.Native;

namespace PawnEditor.v2;

/// <summary>
///     A filter in the side panel of a <see cref="Window_Table{T}" />. It decides which rows pass, draws its own
///     controls, and calls <see cref="MarkDirty" /> when what it lets through changed.
/// </summary>
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

    public abstract void DrawFilter(UIBranch builder);

    protected void MarkDirty()
    {
        _onChanged?.Invoke();
    }
}
