# CLAUDE.md — RimWorld Layout Engine (Taffy Port)

This file gives you persistent context for every session. Read it fully before doing any work.

---

## Project Goal

We are porting [Taffy](https://github.com/DioxusLabs/taffy) — a high-performance Rust UI layout library — to C# for use as a RimWorld mod. The port targets **Block**, **Flexbox**, and **CSS Grid** layout algorithms. The result should be a self-contained `TaffySharp/` module that RimWorld UI code can call into, with no dependency on Taffy's Rust runtime.

---

## Repository Layout

```
<mod-root>/
├── CLAUDE.md                  ← you are here
└── src/
    └── PawnEditor/
        ├── Layout/            ← IGNORE — abandoned first attempt, do not use or reference
        ├── Layout.v2/         ← IGNORE — abandoned second attempt, do not use or reference
        └── TaffySharp/                    ← all new ported Taffy code lives here
            ├── Types/                     ← core data structures (Style, Size, Rect, …)
            ├── Tree/                      ← node tree, dirty flags, layout cache
            ├── Compute/
            │   ├── Block/                 ← port of src/compute/block.rs
            │   ├── Flexbox/               ← port of src/compute/flexbox.rs
            │   └── Grid/                  ← port of src/compute/grid/
            └── TaffyTree.cs               ← public entry point
```

The Taffy Rust source lives at:
```
C:\Users\Joram\Projects\rust\taffy\src\
├── compute/
│   ├── block.rs               ← Block layout reference
│   ├── flexbox.rs             ← Flexbox reference
│   └── grid/                  ← Grid reference
├── style/                     ← Style structs reference
└── tree/                      ← tree/node reference
```

When asked to port something, always read the corresponding Rust source file from `C:\Users\Joram\Projects\rust\taffy\src\` before writing any C#.

### Important: Ignore Previous Attempts

The folders `src/PawnEditor/Layout/` and `src/PawnEditor/Layout.v2/` are earlier abandoned attempts at a flexbox engine. **Do not read, reference, copy from, or base any decisions on code found in these folders.** They have known issues and are the reason we are starting fresh from Taffy. All new code goes exclusively into `src/PawnEditor/TaffySharp/`.

---

## Unity / RimWorld Type Reuse

RimWorld runs on Unity, so `UnityEngine` types are available at runtime. Use them as follows:

- **Inside `TaffySharp/` compute code** — use plain C# structs (`Size<float>`, `Point<float>`, etc.). Keep compute logic decoupled from Unity so it remains unit-testable without a Unity context.
- **At the RimWorld integration boundary** (where `TaffySharp` results are handed to `Widgets` calls) — convert to Unity types freely:
    - `UnityEngine.Vector2` for positions and sizes passed to RimWorld UI calls
    - `UnityEngine.Rect` for final widget rects passed to `Widgets.Draw*` etc.

Provide explicit conversion helpers (e.g. `Layout.ToUnityRect()`) rather than scattering manual conversions throughout calling code.

---

## Porting Rules (follow these every time, without being asked)

### 1. Structs over classes for all layout primitives
Every layout type that is passed around during compute — `Size<T>`, `Rect<T>`, `Point<T>`, `AvailableSpace`, `FlexItem`, `GridItem`, `LineItem` — **must be a `struct`**, not a `class`. This is the single most important performance rule.

```csharp
// correct
public readonly struct Size<T> { public T Width; public T Height; }

// wrong — causes heap allocation on every layout pass
public class Size<T> { public T Width; public T Height; }
```

### 2. No heap allocation in hot paths
Inside any method that runs per-node or per-frame, flag every `new List<>`, `new []`, `new SomeClass()` as a red flag. Use:
- `Span<T>` or `stackalloc` for small temporary collections
- Array pools (`ArrayPool<T>.Shared`) for larger temporary buffers
- Pre-allocated arrays on the node/tree for collections that persist

### 3. `readonly struct` + `in` parameters for read-only data
When passing layout types into compute methods without mutation, use `in` to avoid defensive copies:

```csharp
public static Size<float> ComputeSize(in Style style, in Size<AvailableSpace> space) { … }
```

### 4. Dirty flag caching — never skip this
Every node must track an `IsDirty` flag. Layout should only be recomputed for subtrees that are dirty. Mirror Taffy's `LayoutTree` trait: nodes store a cached `Layout` result and only recompute when marked dirty by a style or tree change.

### 5. Rust → C# translation patterns

| Rust pattern | C# equivalent |
|---|---|
| `Option<T>` | `T?` (nullable value type) or a dedicated `Option<T>` struct |
| `enum` with data (sum types) | Discriminated union struct or separate fields with a `Kind` enum |
| `Vec<T>` in hot path | `Span<T>` / `stackalloc` / pooled array |
| Trait implementations | Interface + explicit struct implementation |
| `f32` | `float` |
| `Iterator` chains | `for` loops (avoid LINQ in hot paths — it allocates) |
| `match` | `switch` expression |
| Tuple returns `(A, B)` | `out` parameters or a small dedicated `readonly struct` |

### 6. No LINQ in compute methods
LINQ is convenient but allocates enumerators. Use plain `for` / `foreach` loops inside `Compute/`. LINQ is fine in test code or one-time setup.

### 7. Keep TaffySharp/ independent of RimWorld
Nothing inside `TaffySharp/` should reference RimWorld or UnityEngine assemblies directly. All RimWorld/Unity integration belongs in the calling code outside this module. This keeps the layout engine unit-testable in isolation.

---

## Porting Order

Work in this order. Do not start a phase until the previous one has passing unit tests.

1. **Core types** — `Style`, `Size<T>`, `Rect<T>`, `Point<T>`, `AvailableSpace`, `Dimension`, `LengthPercentage`, `FlexDirection`, `AlignItems`, etc.  
   Reference: `C:\Users\Joram\Projects\rust\taffy\src\style\`

2. **Tree & cache** — `TaffyTree`, `NodeId`, `Layout`, dirty flags, layout cache.  
   Reference: `C:\Users\Joram\Projects\rust\taffy\src\tree\`

3. **Block compute** — required by both Flexbox and Grid to measure leaf node intrinsic sizes. Port this before the container algorithms. Expose as a full public layout mode.  
   Reference: `C:\Users\Joram\Projects\rust\taffy\src\compute\block.rs`

4. **Flexbox compute** — the main algorithm.  
   Reference: `C:\Users\Joram\Projects\rust\taffy\src\compute\flexbox.rs`

5. **CSS Grid compute** — port after Flexbox is solid.  
   Reference: `C:\Users\Joram\Projects\rust\taffy\src\compute\grid\`

---

## Performance Budget (goals, not hard limits)

- Layout of a 200-node tree should complete in **< 1ms** on a mid-range CPU.
- Zero allocations per frame once the tree is built and unchanged.
- Dirty re-layout of a single leaf node should recompute only its ancestors, not the full tree.

---

## What We Are NOT Porting

- Taffy's Rust test harness (we will write C# unit tests instead)
- `taffy::style::Style` CSS parsing from strings (we set style properties directly in C#)
- `taffy::util::debug` — not needed
- Float layout (`compute/float.rs`, `style/float.rs`) — not needed for RimWorld UI

---

## Do Not Deviate from Rust Taffy in `TaffySharp/`

**The code inside `TaffySharp/` must be a faithful port of the Rust Taffy source.** Do not add
convenience APIs, C#-specific abstractions, or shortcuts that have no equivalent in Rust. If you
think a deviation is necessary, stop and explain why before writing any code — the bar is high.

The only accepted difference is mechanical translation noise:
- `Option<T>` → `T?`, `Vec<T>` → arrays/Span, traits → interfaces, `f32` → `float`, etc.
- `TaffyTree<NodeContext>` → `TaffyTree` with `object? Context` (type erasure only, no behavioral change)

RimWorld/Unity-specific conveniences belong **exclusively** in `src/PawnEditor/UI/Taffy.cs`, which
is the integration layer and is explicitly allowed to diverge.

---

## Session Startup Checklist

Before writing any code in a new session:
1. Re-read this file.
2. Check `src/PawnEditor/TaffySharp/` to understand what has already been ported.
3. Read the relevant Rust source file from `C:\Users\Joram\Projects\rust\taffy\src\` before starting a new port task.
4. If a type already exists in `TaffySharp/Types/`, do not recreate it — extend it.
5. Do not look at or reference `src/PawnEditor/Layout/` or `src/PawnEditor/Layout.v2/`.

---

## Building the Project

Use Rider's bundled MSBuild, **not** `dotnet build`. From the repo root (this is a PowerShell command):

```
& "C:\Users\Joram\AppData\Local\Programs\Rider\tools\MSBuild\Current\Bin\amd64\MSBuild.exe" src\PawnEditor\PawnEditor.csproj /p:Configuration=Debug
```

`dotnet build` will fail due to SDK version mismatch. Always use the MSBuild path above to verify the project compiles after making changes.

### After every file you create or edit:
1. Check that all required `using` directives are present — missing usings are the most common cause of build failures and are easy to overlook.
2. Run the build command above to confirm no new errors were introduced.
3. Fix any errors before moving on to the next file.