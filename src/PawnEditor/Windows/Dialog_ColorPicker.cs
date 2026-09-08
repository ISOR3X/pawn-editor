using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor;

public class Dialog_ColorPicker : Window
{
    private readonly List<Color> _colors;
    private readonly Color _oldColor;
    private readonly Action<Color> _onSelect;

    private Color _selectedColor;

    public Dialog_ColorPicker(Action<Color> onSelect, Color oldColor, List<Color>? colors = null)
    {
        _onSelect = onSelect;
        _oldColor = oldColor;
        _selectedColor = oldColor;

        closeOnAccept = false;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;

        layer = WindowLayer.Super;

        _colors = colors ?? [.. DefDatabase<ColorDef>.AllDefsListForReading.Select(def => def.color)];
    }


    public override Vector2 InitialSize => new(600f, 450f);

    public override void DoWindowContents(Rect inRect)
    {
        Void.Taffy.Div(inRect, builder =>
        {
            builder.Text("Choose a color", style: new StyleOverride { fontSize = GameFont.Medium });
            builder.Div(contentBuilder =>
            {
                contentBuilder.Div(leftBuilder =>
                    {
                        leftBuilder.Div(widgetBuilder =>
                        {
                            widgetBuilder.Item(r => ColorUtility.ColorRect(r, ref _selectedColor),
                                new StyleOverride { flexGrow = 1f });
                            widgetBuilder.Item(r => ColorUtility.HueSlider(r, ref _selectedColor),
                                new StyleOverride { width = Dimension.Px(16f) });
                        }, new StyleOverride { width = Dimension.Px(200f), height = Dimension.Px(200f), gap = new TaffyAxes(Dimension.Px(GenUI.GapSmall), Dimension.Px(GenUI.GapSmall)) });

                        // HSL inputs row
                        ColorUtility.ColorToHSL(_selectedColor, out var fh, out var fs, out var fl);
                        var hInt = Mathf.RoundToInt(fh * 360f);
                        var sInt = Mathf.RoundToInt(fs * 100f);
                        var lInt = Mathf.RoundToInt(fl * 100f);
                        leftBuilder.Div(hslBuilder =>
                        {
                            hslBuilder.Text("H", TextAnchor.MiddleCenter,
                                style: new StyleOverride { fontSize = GameFont.Tiny });
                            hslBuilder.Text("S", TextAnchor.MiddleCenter,
                                style: new StyleOverride { fontSize = GameFont.Tiny });
                            hslBuilder.Text("L", TextAnchor.MiddleCenter,
                                style: new StyleOverride { fontSize = GameFont.Tiny });
                            hslBuilder.InputNumber(ref hInt, 0, 360, id: "color_h",
                                style: new StyleOverride { minWidth = Dimension.Px(50f), width = Dimension.Auto() });
                            hslBuilder.InputNumber(ref sInt, 0, 100, id: "color_s",
                                style: new StyleOverride { minWidth = Dimension.Px(50f), width = Dimension.Auto() });
                            hslBuilder.InputNumber(ref lInt, 0, 100, id: "color_l",
                                style: new StyleOverride { minWidth = Dimension.Px(50f), width = Dimension.Auto() });
                        }, new StyleOverride
                        {
                            display = TaffyDisplay.Grid,
                            gridTemplateColumns = [TrackSizingFunction.Fr(), TrackSizingFunction.Fr(), TrackSizingFunction.Fr()],
                            gap = new TaffyAxes(Dimension.Px(4f), Dimension.Px(0f)),
                            justifyItems = TaffyAlignItems.Stretch
                        });
                        _selectedColor = ColorUtility.HSLToColor(hInt / 360f, sInt / 100f, lInt / 100f);
                    },
                    new StyleOverride
                        { flexDirection = TaffyFlexDirection.Column, gap = new TaffyAxes(Dimension.Px(0f), Dimension.Px(4f)), width = Dimension.Px(200f) });
                contentBuilder.Div(rightBuilder =>
                    {
                        rightBuilder.Div(paletteBuilder =>
                            {
                                foreach (var c in _colors)
                                    paletteBuilder.Item(r =>
                                        {
                                            if (Verse.Widgets.ButtonInvisible(r)) _selectedColor = c;
                                            if (ColorUtility.ApproximatelyEqual(_selectedColor, c))
                                                Verse.Widgets.DrawRectFast(r, Color.white);
                                            else Verse.Widgets.DrawLightHighlight(r);
                                            Verse.Widgets.DrawHighlightIfMouseover(r);
                                            Verse.Widgets.DrawRectFast(r.ContractedBy(GenUI.GapTiny), c);
                                        },
                                        new StyleOverride
                                            { width = Dimension.Px(GenUI.SmallIconSize), height = Dimension.Px(GenUI.SmallIconSize) });
                                paletteBuilder.Item(r =>
                                {
                                    if (Verse.Widgets.ButtonInvisible(r)) _selectedColor = GenColor.RandomColorOpaque();
                                    Verse.Widgets.DrawHighlightIfMouseover(r);
                                    Verse.Widgets.DrawLightHighlight(r);
                                    GUI.DrawTexture(r.ContractedBy(GenUI.GapTiny), TexPawnEditor.Randomize);
                                }, new StyleOverride
                                    { width = Dimension.Px(GenUI.SmallIconSize), height = Dimension.Px(GenUI.SmallIconSize) });
                            },
                            new StyleOverride
                            {
                                flexWrap = TaffyFlexWrap.Wrap, alignContent = TaffyAlignContent.Start,
                                gap = new TaffyAxes(Dimension.Px(GenUI.GapTiny), Dimension.Px(GenUI.GapTiny))
                            });
                        rightBuilder.Div(colorReadoutBuilder =>
                            {
                                colorReadoutBuilder.Item(r => Verse.Widgets.DrawRectFast(r, _oldColor),
                                    new StyleOverride { flexGrow = 1f });
                                colorReadoutBuilder.Item(r => Verse.Widgets.DrawRectFast(r, _selectedColor),
                                    new StyleOverride { flexGrow = 1f });
                            },
                            new StyleOverride
                                { height = Dimension.Px(Text.LineHeightOf(GameFont.Small)), gap = new TaffyAxes(Dimension.Px(GenUI.GapTiny), Dimension.Px(GenUI.GapTiny)) });
                    },
                    new StyleOverride
                        { flexDirection = TaffyFlexDirection.Column, justifyContent = TaffyAlignContent.SpaceBetween });
            }, new StyleOverride { flexGrow = 1f, gap = new TaffyAxes(Dimension.Px(GenUI.Gap), Dimension.Px(GenUI.Gap)) });

            builder.Div(footerBuilder =>
            {
                footerBuilder.Button("Cancel", onClick: _ => Close(), size: UIUtility.ComponentSize.Large);
                footerBuilder.Button("Accept", onClick: _ => Accept(), size: UIUtility.ComponentSize.Large);
            }, new StyleOverride { justifyContent = TaffyAlignContent.SpaceBetween });
        }, new StyleOverride { flexDirection = TaffyFlexDirection.Column, gap = new TaffyAxes(Dimension.Px(0f), Dimension.Px(GenUI.GapSmall)) });
    }

    private void Accept()
    {
        _onSelect.Invoke(_selectedColor);
        Close();
    }
}