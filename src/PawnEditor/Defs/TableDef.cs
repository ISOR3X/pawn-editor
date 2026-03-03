using System;
using System.Collections.Generic;
using Verse;

namespace PawnEditor;

public class TableDef : Def
{
    public required List<ColumnDef> columns;
    public float defaultRowHeight = 30f;
    public bool doAlternateStyle = false;
    public bool highlightSelected = true;
    public ColumnDef? searchColumn;
    public bool showSearchBar = true;
    public Type workerClass = typeof(DefTable);
}