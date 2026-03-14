using System;
using System.Collections.Generic;
using Verse;

namespace PawnEditor;

public abstract class TableDef<T> : Def where T : class
{
    public required List<ColumnDef<T>> columns;
    public float defaultRowHeight = 30f;
    public bool doAlternateStyle = false;
    public bool highlightSelected = true;
    public ColumnDef<T>? searchColumn;
    public bool showSearchBar = true;
    public Type workerClass = typeof(TableWorker<T>);
}

public class DefTableDef : TableDef<Def>;