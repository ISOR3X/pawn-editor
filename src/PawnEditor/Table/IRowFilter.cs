using Void;

namespace PawnEditor.Table;

public interface IRowFilter<TRow>
{
    bool Passes(TRow row, IContext? ctx);

    void DrawFilter(TaffyBuilder builder, Table<TRow> table);
}