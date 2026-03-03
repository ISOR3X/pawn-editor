using System;
using System.Collections.Generic;
using Verse;

namespace PawnEditor;

public class TableDef : Def
{
    public List<ColumnDef> columns;
    public Type workerClass = typeof(DefTable);
    public bool doAlternateStyle = false;
    public float defaultRowHeight = 30f;
    public bool highlightSelected = true;
    public bool showSearchBar = true;
    public ColumnDef searchColumn;
}