# XML Layout System

## Overview

This feature adds XML-driven layout and styling to `SectionWorker`, separating visual
structure (XML) from runtime behaviour (C#). It is intentionally scoped to the new
infrastructure only — no existing sections are migrated in this phase.

---

## Core Concepts

### What XML owns
- Element hierarchy (`<div>`, `<button>`, `<text>`, etc.)
- Static props: `label`, `icon`, `class`, `style`
- Text content via inner text: `<text>Hello world</text>`
- Layout and style via inline `style="..."` and `class="..."`

### What C# owns
- Event handlers (`OnClick`, etc.)
- Dynamic content (foreach loops, conditionals)
- Runtime prop overrides (e.g. replacing a label with a pawn value)

### Named elements (`id`)
Elements with an `id` attribute are **configuration points** — XML declares their
appearance, C# configures their behaviour via `layout.ComponentById<T>(id)` in
`OnLayout`.

Elements without `id` are purely XML-owned and rendered as-is.

If `ComponentById` is never called for a given `id`, the element renders with its
XML props as a static, inert fallback. This is intentional — partial wiring is valid.

---

## Frame Flow

Each frame:
1. `SectionWorker` base calls `OnLayout(layout, pawn)` with a **fresh `UILayout`
   instance** (cloned from the immutable `SectionDef.Layout` template)
2. C# code calls `ComponentById` to register overrides and event handlers on the
   mutable copy — this is cheap (dictionary lookups + field sets)
3. Base class calls `layout.Render(builder)` — walks the node tree, calling
   `element.Render(builder)` on each node, merging XML props with registered overrides
4. The `UILayout` instance is discarded — `SectionDef.Layout` is never mutated

---

## New Types

### `UIElement` (and subclasses)
Per-element objects. XML props are cloned in as defaults; C# can
override any field. Each subclass also owns its own rendering logic via `Render`.

```csharp
public abstract class UIElement
{
    public StyleOverride Style;
    // Raw untyped attributes from XML, for extensibility by other mods
    public Dictionary<string, string> Attrs;

    public string? Get(string key) => Attrs.GetValueOrDefault(key);
    public T? Get<T>(string key, Func<string, T> parse) =>
        Attrs.TryGetValue(key, out var v) ? parse(v) : default;

    public abstract UIElement Clone();

    // Emits this component into the builder. Children are passed in already-rendered
    // form for container types (div), or null for leaf types (button, text).
    public abstract void Render(TaffyBuilder builder, Action<TaffyBuilder>? children);
}

public class ButtonElement : UIElement
{
    public string? Label;
    public Texture2D? Icon;
    public Action<Rect>? OnClick;
    public Action<Rect>? OnHover;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
        => builder.Button(Label, Icon, onClick: OnClick, onHover: OnHover, style: Style);

    public override UIElement Clone() => (ButtonElement)MemberwiseClone();
}

public class DivElement : UIElement
{
    // If set, C# fully owns children. If null, XML children are rendered via the
    // default tree walk.
    public Action<TaffyBuilder>? Children;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
        => builder.Div(Children ?? children ?? (_ => { }), Style);

    public override UIElement Clone() => (DivElement)MemberwiseClone();
}

public class TextElement : UIElement
{
    public string? Content;
    public Color? Color;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
        => builder.Text(Content, color: Color, style: Style);

    public override UIElement Clone() => (TextElement)MemberwiseClone();
}
```

### `UILayout`
Frame-scoped mutable wrapper around the immutable `UILayoutNode` template tree.

```csharp
public class UILayout
{
    private readonly UILayoutNode _template;
    private readonly Dictionary<string, UIElement> _overrides = new();

    public T ComponentById<T>(string id) where T : UIElement
    {
        if (!_overrides.TryGetValue(id, out var config))
        {
            var node = _template.FindById(id)
                ?? throw new Exception($"[PawnEditor] No element with id '{id}' in layout");
            config = node.Props.Clone();
            _overrides[id] = config;
        }

        return config as T
            ?? throw new Exception($"[PawnEditor] Element '{id}' is not a {typeof(T).Name}");
    }

    internal void Render(TaffyBuilder builder) => RenderNode(builder, _template);

    private void RenderNode(TaffyBuilder builder, UILayoutNode node)
    {
        var config = node.Id != null && _overrides.TryGetValue(node.Id, out var ov)
            ? ov
            : node.Props;

        // For container nodes whose Children wasn't overridden in C#,
        // fall back to rendering XML children recursively.
        Action<TaffyBuilder>? xmlChildren = node.Children.Count > 0
            ? inner => { foreach (var child in node.Children) RenderNode(inner, child); }
            : null;

        config.Render(builder, xmlChildren);
    }
}
```

### `UILayoutNode`
Immutable parsed representation of one XML element. Never mutated after load.

```csharp
public class UILayoutNode
{
    public string Tag;                  // "div", "button", "text", etc.
    public string? Id;                  // from id="..."
    public UIElement Props;     // resolved style + typed props, read-only template
    public List<UILayoutNode> Children;

    public UILayoutNode? FindById(string id)
    {
        if (Id == id) return this;
        foreach (var child in Children)
            if (child.FindById(id) is { } found) return found;
        return null;
    }
}
```

### `UILayoutParser`
Parses the `<layout>` XmlNode into a `UILayoutNode` tree.

Tag → element type mapping:
- `div` → `DivElement`
- `button` → `ButtonElement` — reads `label`, `icon` attributes
- `text` → `TextElement` — reads `XmlNode.InnerText` as `Content`, `color` attribute

Text content: `<text>Hello world</text>` sets `TextElement.Content = "Hello world"`.
Translate attribute: `<text translate="true">SomeTranslationKey</text>` calls
`Content.Translate()` at parse time.

All unknown attributes go into `Attrs` dict — no warning, just stored for C# access.
Unknown tags: log warning, skip node and its children.

Style resolution per node:
```
ResolveClasses(class="...").Merge(ParseInlineStyle(style="..."))
// inline wins over class
```

### `TaffyStyleDef`
A RimWorld `Def` that maps CSS class names to inline style strings. Any mod can
define one.

```xml
<PawnEditor.TaffyStyleDef>
    <defName>PawnEditorStyles</defName>
    <styles>
        <li name="row" value="flex-direction: row" />
        <li name="wrap" value="flex-wrap: wrap" />
        <li name="w-full" value="width: 100%" />
        <li name="grow" value="flex-grow: 1" />
        <li name="gap-sm" value="gap: 4px" />
    </styles>
</PawnEditor.TaffyStyleDef>
```

`ResolveReferences` parses each value string into a `StyleOverride` and stores it
in a `Dictionary<string, StyleOverride>` for fast lookup at layout parse time.

### `UIIcons`
Resolves icon names to `Texture2D`. Tries short-name registry first, then reflection.

```csharp
public static class UIIcons
{
    public static void Register(string name, Texture2D icon);

    // "delete"                    -> registry lookup
    // "RimWorld.TexButton.Delete" -> reflection, cached
    public static Texture2D? Resolve(string name);
}
```

Resolution order:
1. Registry lookup (short names, e.g. `"delete"`)
2. Reflection: split on last `.`, resolve type via `AppDomain.CurrentDomain.GetAssemblies()`,
   get static field. Cache results.

Built-in short names are registered in a `[StaticConstructorOnStartup]` class.

---

## Inline Style Parser

Parses `"flex-direction: row; gap: 4px; width: 100%"` into a `StyleOverride`.

**Before implementing from scratch**, check:
1. `TaffyLayoutNode.ParseStyleAttributes` in the existing codebase — this already
   handles style attribute parsing and should be used as the basis or extended rather
   than duplicated.
2. The local Taffy Rust source at `C:\Users\Joram Hoogerwerf\Projects\rust\taffy` may
   have CSS parsing logic worth referencing for property name coverage and value formats.

Supported properties (minimum viable set — expand as needed):
- `flex-direction`: `row` | `column`
- `flex-wrap`: `wrap` | `nowrap`
- `flex-grow`: float
- `flex-shrink`: float
- `flex-basis`: `auto` | `Npx` | `N%`
- `width`, `height`, `min-width`, `max-width`: `auto` | `Npx` | `N%`
- `gap`: `Npx`
- `padding`: `Npx` (uniform) — expand to per-side later
- `align-content`: `flex-start` | `flex-end` | `center` | `stretch`

Unknown properties: log warning, skip.

---

## `SectionDef` and `SectionWorker` Changes

**This is a non-breaking change.** The `<layout>` field is optional. Workers without
it continue to work exactly as before via their existing `DoSectionContents` override.
`OnLayout` has a default no-op implementation so workers that don't need it don't have
to override it.

### `SectionDef`
Add an optional `<layout>` field, stored as a parsed `UILayoutNode` tree after
`ResolveReferences`:

```xml
<PawnEditor.SectionDef>
    <defName>PawnEditor_Abilities</defName>
    <workerClass>PawnEditor.SectionWorker_Abilities</workerClass>
    <layout>
        <div style="flex-direction: row; flex-wrap: wrap; width: 100%">
            <div id="abilityIcons" style="flex-wrap: wrap; gap: 4px; flex-grow: 1" />
            <button id="addAbility" label="Add ability" icon="plus" />
        </div>
    </layout>
</PawnEditor.SectionDef>
```

### `SectionWorker` base class

```csharp
protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
{
    if (SectionDef.Layout != null)
    {
        var layout = new UILayout(SectionDef.Layout);
        OnLayout(layout, pawn);
        layout.Render(builder);
    }
}

// Override this in workers that have a <layout>
protected virtual void OnLayout(UILayout layout, Pawn pawn) { }
```

---

## Usage Example (`SectionWorker_Abilities`)

```csharp
protected override void OnLayout(UILayout layout, Pawn pawn)
{
    layout.ComponentById<ButtonElement>("addAbility").OnClick = _ =>
        Find.WindowStack.Add(new Window_Table<AbilityDef>(GetTraitsTable(pawn), ...));

    layout.ComponentById<DivElement>("abilityIcons").Children = inner =>
    {
        foreach (var ability in GetAbilitiesForPawn(pawn))
            inner.Div(r => DrawAbilityIcon(r, ability, pawn),
                style: new StyleOverride { padding = Taffy.Padding(5f) });
    };
}
```

---

## Implementation Order

1. `UIElement` subclasses (`ButtonElement`, `DivElement`, `TextElement`) with
   `Render` and `Clone`
2. `UILayoutNode` — immutable parsed tree + `FindById`
3. Inline style parser — extend `TaffyLayoutNode.ParseStyleAttributes` as needed
4. `TaffyStyleDef` — def type + class registry
5. `UILayoutParser` — XML → `UILayoutNode` tree, resolving classes + inline style +
   text content
6. `UILayout` — frame-scoped wrapper + `ComponentById` + `Render`
7. `SectionDef` — add optional `<layout>` field, parse in `ResolveReferences`
8. `SectionWorker` — add `OnLayout` virtual + wire into `DoSectionContents`
9. Demo: migrate `SectionWorker_Abilities` to `OnLayout`
10. `UIIcons` — registry + reflection resolver + cache (polish, do last)

---

## Out of Scope (this phase)

- `<foreach>` or any template logic in XML
- Stateful components (inputs, dropdowns) via XML
- Hot reload of XML layout
- Migration of any section other than `SectionWorker_Abilities`

---

## [NEW] Tab–Section Style Composition

`TabDef.Layout` arranges sections within a tab and can apply styles to them via
`<section>` tags. Those styles (flex-grow, min-width, etc.) need to be merged onto
the section's own layout at render time.

### How `<section>` tags work in `TabDef.Layout`

A `<section>` tag in a tab layout references a `SectionDef` by inner text and
optionally carries styles that control how that section sits within the tab:

```xml
<div style="display: flex; flex-direction: row; flex-wrap: wrap; gap: 10px">
    <section style="flex-direction: column; flex-grow: 1; max-width: 400px">
        PawnEditor_Abilities
    </section>
</div>
```

The `UILayoutParser` maps `<section>` to a new `SectionElement`:

```csharp
public class SectionElement : UIElement
{
    public string SectionDefName;   // inner text of the <section> tag
    public SectionDef? ResolvedDef; // resolved at parse time

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        // Delegates to the section worker, passing this element's Style
        // as the tab-provided style to merge onto the section root
        ResolvedDef?.Worker.DoSection(builder, tabStyle: Style);
    }
}
```

### Style merging rules

When a section has a `<layout>` with a **single root node**, the tab's `<section style="...">` style
is merged onto that root node. **Tab wins on conflict** — tab styles take precedence over section
root styles for the same property. The merge uses a per-frame clone of the root's effective config
so the immutable template is never mutated:

```csharp
// Tab is "this" → tab wins. config is cloned so the template stays immutable.
var config = uiLayout.GetConfigForNode(rootNode).Clone();
config.Style = tabStyle.Merge(config.Style);
config.Render(builder, xmlChildren);
```

When a section has **multiple root nodes**, tab styles are **not applied** — no wrapper div is
inserted. A dev-mode warning is logged to make this visible during development. **To receive tab
styles, always wrap a section's contents in a single root `<div>`.**

For sections **without a `<layout>`** (legacy `DoSectionContents` workers), tab styles are applied
via a wrapper div since there is no root node to merge onto:

```csharp
builder.Div(inner => worker.BuildSection(inner, pawn), style: tabSectionStyle);
```

### Attribute format

Both `<section>` and `<div>` elements use `style="..."` for all CSS properties.
`mayRequire` is the only attribute that lives outside `style="..."`:

```xml
<!-- correct -->
<section style="flex-direction: column; flex-grow: 1">PawnEditor_Traits</section>
<section style="align-items: center" mayRequire="Ludeon.Rimworld.Ideology">PawnEditor_FavColor</section>

<!-- wrong — individual attributes are supported for backward compat but not recommended -->
<section flex-direction="column" flex-grow="1">PawnEditor_Traits</section>
```