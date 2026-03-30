using Verse;

namespace PawnEditor.Table;

public sealed class ContentSourceFilter<TDef> : IRowFilter<TDef> where TDef : Def
{
    public ModContentPack? Selected;

    public bool Passes(TDef row, ITableContext? ctx)
        => Selected == null || row.modContentPack == Selected;
}
