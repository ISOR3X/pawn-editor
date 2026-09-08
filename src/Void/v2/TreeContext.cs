using UnityEngine;
using Verse;

namespace Void.v2;

/// <summary>
///     Per-leaf visual payload for measured nodes: text, font, or a texture. Compared by value
///     each frame so <c>SetNodeContext</c> (and the native <c>mark_dirty</c> it triggers) is only
///     called when this actually changed since last frame.
///     <para>
///         A reference type (not a struct) because <c>TaffyTree&lt;TContext&gt;</c> constrains
///         <c>TContext</c> to <c>class</c> — still gets compiler-generated, per-field equality via
///         <c>record</c>, just heap-allocated per instance rather than a stack value.
///     </para>
/// </summary>
public sealed record TreeContext(string? Text = null, GameFont? Font = null, Texture2D? Tex = null);
