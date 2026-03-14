using System;
using System.Collections.Generic;
using Verse;

namespace PawnEditor;

public abstract class TableDef : Def
{
    public float defaultRowHeight = 30f;
    public bool doAlternateStyle = false;
    public bool highlightSelected = true;
    public bool showSearchBar = true;

    public virtual ColumnDef? SearchColumn => null;
}

public class DefTableDef : TableDef
{
    public required List<DefColumnDef> columns;
    public DefColumnDef? searchColumn;
    public Type workerClass = typeof(DefTableWorker);

    public override ColumnDef? SearchColumn => searchColumn;
}