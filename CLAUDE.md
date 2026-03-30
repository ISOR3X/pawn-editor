# Table System Implementation Plan

## Overview

Replace the existing Def-driven `TableWorker<T>` / `ColumnWorker<T>` system with a new, code-first table architecture. The existing classes should be considered deprecated — new tables are constructed directly via `Table<TRow>`. An XML connector can be layered on top later without changing the core.

---

## 1. Context System (`ITableContext`)

Some columns require runtime state (e.g. a `Pawn`) to render their cells. Context is optional and per-table — at most one context object per table instance.

```csharp
// Marker interface — allows storing context without generics at the table level
public interface ITableContext { }

// Typed accessor used by context-aware column workers
public interface ITableContext<T> : ITableContext {
    T Value { get; }
}
```

**Concrete implementations** are plain sealed classes, one per context type needed:

```csharp
public sealed class PawnContext : ITableContext<Pawn> {
    public Pawn Value { get; }
    public PawnContext(Pawn pawn) => Value = pawn;
}
```

Context is passed into `Table<TRow>` at construction time. It is stored as `ITableContext?` — nullable, so context-free tables are equally first-class.

---

## 2. Column Workers

Column workers are the rendering unit for a single column. The hierarchy has three levels:

### 2a. `ColumnWorker<TRow>` — base, no context

Handles header drawing, width hints, and cell rendering for rows that need no runtime context.

```csharp
public abstract class ColumnWorker<TRow> {
    public abstract float Width { get; }           // flexBasis hint passed to layout engine
    public abstract void DrawHeader(Rect r);
    public abstract void DrawCell(Rect r, TRow row);
}
```

### 2b. `IContextColumn` — internal marker interface

Allows the table renderer to check for context-awareness without reflection and without exposing `ITableContext` in the base class signature.

```csharp
internal interface IContextColumn {
    void DrawCell(Rect r, object row, ITableContext ctx);
}
```

### 2c. `ColumnWorker<TRow, TContext>` — context-aware

Subclasses override `DrawCell(Rect, TRow, TContext)`. The base plumbs the untyped `IContextColumn` implementation internally.

```csharp
public abstract class ColumnWorker<TRow, TContext> : ColumnWorker<TRow>, IContextColumn
    where TContext : ITableContext {

    // Context-aware entry point for subclasses
    protected abstract void DrawCell(Rect r, TRow row, TContext ctx);

    // Fallback — called when table has no context; subclasses may override
    public override void DrawCell(Rect r, TRow row) { }

    // IContextColumn — casts and dispatches
    void IContextColumn.DrawCell(Rect r, object row, ITableContext ctx)
        => DrawCell(r, (TRow)row, (TContext)ctx);
}
```

### 2d. `ColumnWorker<TRow>.Create` — delegate factory for one-offs

A pair of static factory methods on `ColumnWorker<TRow>` covers simple columns without requiring a new class:

```csharp
// No context
ColumnWorker<TRow>.Create(
    header: "Def Name",
    width: 120f,
    drawCell: (rect, row) => Widgets.Label(rect, row.defName)
);

// Context-aware
ColumnWorker<TRow>.Create<PawnContext>(
    header: "Title",
    width: 120f,
    drawCell: (rect, row, ctx) => Widgets.Label(rect, row.GetTitleFor(ctx.Value))
);
```

Both return a `ColumnWorker<TRow>` (or `ColumnWorker<TRow, TContext>`) backed by a private delegate-holding subclass. Use a full subclass for anything non-trivial.

Context-free and context-aware columns coexist in the same column list. The renderer pattern:

```csharp
foreach (var col in _columns) {
    if (col is IContextColumn ctxCol && _context != null)
        ctxCol.DrawCell(cellRect, row, _context);
    else
        col.DrawCell(cellRect, row);
}
```

---

## 3. Filter Layer (`IRowFilter<TRow>`)

Filters are a separate concern from context but may share it (e.g. a filter that checks backstory applicability still needs the pawn). Filters are applied before rendering to cull the visible row set.

```csharp
public interface IRowFilter<TRow> {
    bool Passes(TRow row, ITableContext? ctx);
}
```

The table holds an `IReadOnlyList<IRowFilter<TRow>>` (empty by default). Filtered rows are computed once per frame before the render loop, not inside the per-cell draw call.

Multiple filters are AND-composed — a row must pass all active filters to be visible.

---

## 4. `Table<TRow>` — runtime table instance

Constructed directly — context and filters are both optional.

```csharp
public sealed class Table<TRow> {
    public Table(
        IEnumerable<TRow> rows,
        IReadOnlyList<ColumnWorker<TRow>> columns,
        ITableContext? context = null,
        IReadOnlyList<IRowFilter<TRow>>? filters = null);

    public void Draw(Rect r);
}
```

**Example — backstory table mixing subclassed and delegate columns:**

```csharp
var table = new Table<BackstoryDef>(
    rows: DefDatabase<BackstoryDef>.AllDefs,
    columns: new ColumnWorker<BackstoryDef>[] {
        ColumnWorker<BackstoryDef>.Create(            // delegate, no context
            header: "Def Name",
            width: 120f,
            drawCell: (r, row) => Widgets.Label(r, row.defName)
        ),
        ColumnWorker<BackstoryDef>.Create<PawnContext>( // delegate, context-aware
            header: "Title",
            width: 200f,
            drawCell: (r, row, ctx) => Widgets.Label(r, row.GetTitleFor(ctx.Value))
        ),
        new ApplicabilityIconColumn(),               // subclass, for anything non-trivial
    },
    context: new PawnContext(pawn),
    filters: new IRowFilter<BackstoryDef>[] {
        new ApplicableBackstoryFilter(),
    }
);
```

Key responsibilities:
- Apply filters to produce the visible row list (cached per frame, invalidated when filter set or row source changes). Filters receive the same `ITableContext?` instance the columns do — a filter checking backstory applicability gets the pawn the same way `TitleCapColumn` does.
- Drive the layout engine for column widths using each column's `Width` property as `flexBasis`
- Handle scrolling and early culling (row height caching, same pattern as the existing `TableWorker`)
- Dispatch to `IContextColumn.DrawCell` or `ColumnWorker.DrawCell` per column per visible row

---

## 6. XML Connector (future)

When XML-defined tables are needed, a thin `TableDef` → `Table<TRow>` bridge is added. The non-generic `ColumnDefBase` / `TableDefBase` hierarchy remains (to avoid open-generic issues with RimWorld's `PlayDataLoader`), but during `ResolveReferences` they construct and cache a `Table<TRow>` directly. No changes to the core system are required.

---

## Implementation Order

1. `ITableContext` / `ITableContext<T>` + first concrete context (`PawnContext`)
2. `ColumnWorker<TRow>` base + `IContextColumn` marker
3. `ColumnWorker<TRow, TContext>` with internal dispatch
4. `ColumnWorker<TRow>.Create` delegate factories (context-free and context-aware)
5. `IRowFilter<TRow>`
6. `Table<TRow>` with constructor + rendering loop
7. Migrate one real table (backstory table is a good candidate — exercises both column types, delegate columns, and a context-aware filter)
8. XML connector (deferred)