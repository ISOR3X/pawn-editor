using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnEditor;

public abstract class DefTableWorker(DefTableDef def, Func<IEnumerable<Def>> thingsGetter, Def? defaultThing = null)
    : TableWorker<Def>(def, thingsGetter, defaultThing)
{
    protected override IEnumerable<ColumnWorker<Def>> AllColumns => def.columns.Select(c => c.Worker);
}