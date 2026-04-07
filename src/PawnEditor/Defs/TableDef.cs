using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class TableDef : Def
{
    public float defaultRowHeight = 30f;
    public bool highlightSelected = true;

    public virtual ColumnDef? SearchColumn => null;
}

/// <summary>
/// These casts are required because Defs can't have generic types.
/// </summary>
public class DefTableDef : TableDef
{
    public required List<DefColumnDef> columns;
    public DefColumnDef? searchColumn;
    public Type workerClass = typeof(DefTableWorker);

    public override ColumnDef? SearchColumn => searchColumn;

    /// <summary>
    /// Creates a new <see cref="Table{TRow}"/> from this Def's column list.
    /// The caller is responsible for caching the result if needed.
    /// </summary>
    public Table<Def> CreateTable(
        IEnumerable<Def> rows,
        ITableContext? context = null,
        IReadOnlyList<IRowFilter<Def>>? filters = null,
        Action<Rect, Def, ITableContext?>? onRowHover = null,
        Action<Def?>? onSelectChanged = null,
        Func<Def, string>? searchProjection = null)
        => new Table<Def>(
            rows,
            columns.Select(c => c.Worker).ToList(),
            context,
            filters,
            onRowHover,
            searchProjection,
            onSelectChanged);
}

public class ThingTableDef : TableDef
{
    public required List<ThingColumnDef> columns;
    public ThingColumnDef? searchColumn;
    public Type workerClass = typeof(ThingTableWorker);

    public override ColumnDef? SearchColumn => searchColumn;

    /// <summary>
    /// Creates a new <see cref="Table{TRow}"/> from this Def's column list.
    /// The caller is responsible for caching the result if needed.
    /// </summary>
    public Table<Thing> CreateTable(
        IEnumerable<Thing> rows,
        ITableContext? context = null,
        IReadOnlyList<IRowFilter<Thing>>? filters = null,
        Action<Rect, Thing, ITableContext?>? onRowHover = null,
        Action<Thing?>? onSelectChanged = null,
        Func<Thing, string>? searchProjection = null)
        => new Table<Thing>(
            rows,
            columns.Select(c => c.Worker).ToList(),
            context,
            filters,
            onRowHover,
            searchProjection,
            onSelectChanged);
}