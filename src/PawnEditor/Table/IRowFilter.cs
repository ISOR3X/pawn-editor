namespace PawnEditor.Table;

public interface IRowFilter<TRow>
{
    bool Passes(TRow row, ITableContext? ctx);
}
