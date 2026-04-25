using Taffy;
using UnityEngine;
using Verse;
using Void;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

public class SectionWorker_Debug(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder b, Pawn pawn)
    {
        b.Div(
            draw: r =>
            {
                if (Time.frameCount % 120 == 0)
                    Log.Message($"[LayoutDebug] outer-col  w={r.width:F1} h={r.height:F1}");
            },
            builder: col =>
            {
                Variation(col, "1: grow min-w-0",
                    new StyleOverride { flexWrap = FlexWrap.Wrap, flexGrow = 1f, minWidth = Dimension.Length(0f) });

                Variation(col, "2: w-full (100%)",
                    new StyleOverride { flexWrap = FlexWrap.Wrap, width = Dimension.Percent(1f) });

                Variation(col, "3: grow min-w-0 basis:0",
                    new StyleOverride { flexWrap = FlexWrap.Wrap, flexGrow = 1f, minWidth = Dimension.Length(0f), flexBasis = Dimension.Length(0f) });

                Variation(col, "4: shrink min-w-0 (no grow)",
                    new StyleOverride { flexWrap = FlexWrap.Wrap, flexShrink = 1f, minWidth = Dimension.Length(0f) });

                Variation(col, "5: parent w-full + grow min-w-0",
                    new StyleOverride { flexWrap = FlexWrap.Wrap, flexGrow = 1f, minWidth = Dimension.Length(0f) },
                    parentRowWidth: Dimension.Percent(1f));
            },
            style: new StyleOverride
            {
                flexDirection = FlexDirection.Column,
                flexGrow = 1f,
                gap = new Size<LengthPercentage>(LengthPercentage.ZERO, LengthPercentage.Length(8f))
            });
    }

    private static void Variation(TaffyBuilder b, string label, StyleOverride nameBlockStyle,
        Dimension? parentRowWidth = null)
    {
        b.Div(col =>
        {
            col.Item(r => Verse.Widgets.Label(r, label),
                new StyleOverride { height = Dimension.Length(UIUtility.ButtonHeight) });

            var rowStyle = new StyleOverride
            {
                gap = new Size<LengthPercentage>(LengthPercentage.Length(4f), LengthPercentage.ZERO)
            };
            if (parentRowWidth is { } pw) rowStyle.width = pw;

            col.Div(
                draw: r =>
                {
                    if (Time.frameCount % 120 == 0)
                        Log.Message($"[LayoutDebug] {label,-30} row        w={r.width:F1} h={r.height:F1}");
                },
                builder: row =>
                {
                    row.Div(
                        draw: r =>
                        {
                            Verse.Widgets.DrawBox(r);
                            if (Time.frameCount % 120 == 0)
                                Log.Message($"[LayoutDebug] {label,-30} name_block w={r.width:F1} h={r.height:F1}");
                        },
                        builder: inputs =>
                        {
                            inputs.Item(
                                r =>
                                {
                                    Verse.Widgets.DrawBoxSolid(r, new Color(0.3f, 0.5f, 0.8f, 0.5f));
                                    Verse.Widgets.Label(r, "First");
                                    if (Time.frameCount % 120 == 0)
                                        Log.Message($"[LayoutDebug] {label,-30} First      x={r.x:F1} y={r.y:F1} w={r.width:F1}");
                                },
                                new StyleOverride { width = Dimension.Length(120f), height = Dimension.Length(UIUtility.ButtonHeight) });
                            inputs.Item(
                                r =>
                                {
                                    Verse.Widgets.DrawBoxSolid(r, new Color(0.3f, 0.5f, 0.8f, 0.5f));
                                    Verse.Widgets.Label(r, "Nick");
                                    if (Time.frameCount % 120 == 0)
                                        Log.Message($"[LayoutDebug] {label,-30} Nick       x={r.x:F1} y={r.y:F1} w={r.width:F1}");
                                },
                                new StyleOverride { width = Dimension.Length(120f), height = Dimension.Length(UIUtility.ButtonHeight) });
                            inputs.Item(
                                r =>
                                {
                                    Verse.Widgets.DrawBoxSolid(r, new Color(0.3f, 0.5f, 0.8f, 0.5f));
                                    Verse.Widgets.Label(r, "Last");
                                    if (Time.frameCount % 120 == 0)
                                        Log.Message($"[LayoutDebug] {label,-30} Last       x={r.x:F1} y={r.y:F1} w={r.width:F1}");
                                },
                                new StyleOverride { width = Dimension.Length(120f), height = Dimension.Length(UIUtility.ButtonHeight) });
                        },
                        style: nameBlockStyle);

                    row.Item(r => Verse.Widgets.DrawBoxSolid(r, new Color(0.8f, 0.3f, 0.3f)),
                        new StyleOverride { width = Dimension.Length(30f), height = Dimension.Length(UIUtility.ButtonHeight) });
                },
                style: rowStyle);
        }, new StyleOverride
        {
            flexDirection = FlexDirection.Column,
            gap = new Size<LengthPercentage>(LengthPercentage.ZERO, LengthPercentage.Length(2f))
        });
    }
}
