namespace PawnEditor.Table;

public interface IRowFilter<TRow>
{
    bool Passes(TRow row, IEditorContext? ctx);

    void DrawFilter(TaffyBuilder builder, Table<TRow> table);
}