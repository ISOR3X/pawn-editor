# Taffy

A C# port of [Taffy](https://github.com/DioxusLabs/taffy), a high-performance UI layout library written in Rust.

Taffy implements the CSS flexbox and grid layout algorithms. This port was created to bring the same layout engine to
RimWorld mod UI without any native or unsafe dependencies, targeting .NET Framework 4.7.2.
Note that this library does not implement a direct RimWorld interface. For that, please see the Void UI library instead.

## Structure

```
Taffy/
  Types/       - Core geometry and style types (Rect, Style, Dimension, …)
  Tree/        - Layout tree, node IDs, and cached results
  Compute/
    Block/     - Block layout algorithm
    Flexbox/   - Flexbox layout algorithm
    Grid/      - CSS Grid layout algorithm
  TaffyTree.cs - Public API: build a node tree and compute layout
  tests/       - xUnit tests targeting net9.0 (no RimWorld dependency)
```
