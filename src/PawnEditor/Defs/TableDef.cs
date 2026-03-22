using System;
using System.Collections.Generic;
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
}

public class ThingTableDef : TableDef
{
    public required List<ThingColumnDef> columns;
    public ThingColumnDef? searchColumn;
    public Type workerClass = typeof(ThingTableWorker);

    public override ColumnDef? SearchColumn => searchColumn;
}