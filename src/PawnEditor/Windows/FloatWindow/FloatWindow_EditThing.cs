using System.Linq;
using HotSwap;
using PawnEditor.Layout;
using RimWorld;
using UnityEngine;
using Verse;
using L = PawnEditor.Layout.FlexLayoutHelper;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_EditThing(Rect boundWidgetRect, Thing thing) : FloatWindow(boundWidgetRect)
{
    protected override Window? Owner => Find.WindowStack.WindowOfType<Window_Editor>();
    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomRight;
    protected override bool UseWidgetWidth => true;
    private Thing _thing = thing;

    public override void DoWindowContents(Rect inRect)
    {
        var layout = L.Row([
            L.Cell(rect =>
            {
                // Stuff
                if (UIUtility.ButtonTextLabeled_WithIcon(rect, "StatsReport_Material".Translate(),
                        _thing.Stuff.LabelCap, Verse.Widgets.GetIconFor(_thing.Stuff),
                        _thing.Stuff.stuffProps.color))
                {
                    Find.WindowStack.Add(new FloatMenu(GenStuff.AllowedStuffsFor(_thing.def)
                        .Select(stuff => new FloatMenuOption(stuff.LabelCap, () =>
                            {
                                _thing.SetStuffDirect(stuff);
                                _thing.SetColor(stuff.stuffProps.color);
                                _thing.Notify_ColorChanged();
                            },
                            Verse.Widgets.GetIconFor(stuff),
                            stuff.stuffProps.color
                        ))
                        .ToList()));
                }
            }).When(_thing.def.MadeFromStuff)
        ], gapX: 24f, wrap: true, childFlexBasis: 0.5f);

        using (new TextBlock(GameFont.Small))
        {
            var height = FlexLayoutEngine.Draw(layout, inRect, (action, rect) =>
            {
                action(rect);
                return UIUtility.ButtonHeight;
            });
            if (!Mathf.Approximately(windowRect.height, height))
                windowRect.height = height + Margin * 2;
        }
    }
}