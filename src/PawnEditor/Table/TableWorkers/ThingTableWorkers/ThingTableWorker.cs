using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnEditor;

public abstract class ThingTableWorker(ThingTableDef def, Func<IEnumerable<Thing>> thingsGetter, Thing? defaultThing = null)
    : TableWorker<Thing>(def, thingsGetter, defaultThing)
{
    protected override IEnumerable<ColumnWorker<Thing>> AllColumns => def.columns.Select(c => c.Worker);
}