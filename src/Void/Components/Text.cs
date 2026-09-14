using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;

public static partial class VoidComponents
{
    extension(UIBranch branch)
    {
        public TaffyNode Text(string text, Style? style = null, string? id = null, [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);

            var fontSize = style?.fontSize ?? GameFont.Small;
            var align = style?.textAnchor ?? TextAnchor.UpperLeft;
            var wrap = style?.wordWrap ?? true;

            void _draw(Rect r)
            {
                using (new GUIColor(style?.color ?? Color.white))
                using (new TextBlock(fontSize, align, wrap))
                {
                    Verse.Widgets.Label(r, wrap ? text : text.Truncate(r.width));
                }
            }

            var ctx = new LeafContext { text = text, fontSize = fontSize, textAnchor = align, textWrap = wrap };
            var node = branch.Div(context: ctx, draw: _draw, style: style, id: key);

            return node;
        }
    }
}
