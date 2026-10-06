using UnityEngine;
using Verse;

namespace Void.Native;

/// <summary>
///     Context used for calculating the size of a leaf.
///     Modifying context marks a leaf dirty.
///     TODO: Rename to something more befitting of the function, e.g. MeasureSpec.
/// </summary>
public record LeafContext
{
    public GameFont fontSize = GameFont.Small;

    public string? text;
    public TextAnchor textAnchor = TextAnchor.UpperLeft;
    public bool textWrap = true;
}
