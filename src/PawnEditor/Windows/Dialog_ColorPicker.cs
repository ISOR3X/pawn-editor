using HotSwap;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Display = Taffy.Display;

namespace PawnEditor;

[HotSwappable]
public class Dialog_ColorPicker : Window
{
    private readonly List<Color> _colors;
    private readonly Color _oldColor;
    private readonly Action<Color> _onSelect;

    private Color _selectedColor;
    
    public Dialog_ColorPicker(Action<Color> onSelect, Color oldColor, List<Color>? colors = null,
        Dictionary<string, Color>? specialColors = null)
    {
        _onSelect = onSelect;
        _oldColor = oldColor;
        _selectedColor = oldColor;

        closeOnAccept = false;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;

        if (specialColors != null && !specialColors.TryGetValue("Old", out _)) specialColors["Old"] = oldColor;

        if (colors == null)
        {
            _colors = [];
            DefDatabase<ColorDef>.AllDefsListForReading.ForEach(c => _colors.Add(c.color));
        }
        else
        {
            _colors = colors;
        }

        layer = WindowLayer.Super;
    }


    public override Vector2 InitialSize => new(600f, 450f);

    public override void DoWindowContents(Rect inRect)
    {
        Taffy.Div(inRect, builder =>
        {
            builder.Text("Choose a color", GameFont.Medium);
            builder.Div(contentBuilder =>
            {
                contentBuilder.Div(leftBuilder =>
                {
                    leftBuilder.Div(widgetBuilder =>
                    {
                        widgetBuilder.Item(r => ColorUtility.ColorRect(r, ref _selectedColor),
                            new StyleOverride { flexGrow = 1f });
                        widgetBuilder.Item(r => ColorUtility.HueSlider(r, ref _selectedColor),
                            new StyleOverride { width = UIUtility.ButtonHeight });
                    }, new StyleOverride { width = 200f, height = 200f, gap = Taffy.Gap(GenUI.GapSmall)});
                    // 2D color rect (saturation × value, fixed hue)

                    // HSL inputs row
                    ColorUtility.ColorToHSL(_selectedColor, out var fh, out var fs, out var fl);
                    var hInt = Mathf.RoundToInt(fh * 360f);
                    var sInt = Mathf.RoundToInt(fs * 100f);
                    var lInt = Mathf.RoundToInt(fl * 100f);
                    leftBuilder.Div(hslBuilder =>
                    {
                        hslBuilder.Text("H", GameFont.Tiny, TextAnchor.MiddleCenter);
                        hslBuilder.Text("S", GameFont.Tiny, TextAnchor.MiddleCenter);
                        hslBuilder.Text("L", GameFont.Tiny, TextAnchor.MiddleCenter);
                        hslBuilder.InputNumber(ref hInt, 0, 360, id: "color_h",
                            style: new StyleOverride { minWidth = 50f, width = Dimension.AUTO });
                        hslBuilder.InputNumber(ref sInt, 0, 100, id: "color_s",
                            style: new StyleOverride { minWidth = 50f, width = Dimension.AUTO });
                        hslBuilder.InputNumber(ref lInt, 0, 100, id: "color_l",
                            style: new StyleOverride { minWidth = 50f, width = Dimension.AUTO });
                    }, new StyleOverride
                    {
                        display = Display.Grid,
                        gridTemplateColumns = [Taffy.Fr(), Taffy.Fr(), Taffy.Fr()],
                        gap = Taffy.Gap(4f, 0f),
                        justifyItems = AlignItems.Stretch,
                    });
                    _selectedColor = ColorUtility.HSLToColor(hInt / 360f, sInt / 100f, lInt / 100f);
                }, new StyleOverride { flexDirection = FlexDirection.Column, gap = Taffy.Gap(0f, 4f), width = 200f });
                contentBuilder.Div(rightBuilder =>
                    {
                        rightBuilder.Div(paletteBuilder =>
                            {
                                foreach (var c in _colors)
                                {
                                    paletteBuilder.Item(r =>
                                        {
                                            if (Verse.Widgets.ButtonInvisible(r)) _selectedColor = c;
                                            if (!ColorUtility.ApproximatelyEqual(_selectedColor, c)) Verse.Widgets.DrawLightHighlight(r);
                                            else Verse.Widgets.DrawRectFast(r, Color.white);
                                            Verse.Widgets.DrawRectFast(r.ContractedBy(GenUI.GapTiny), c);
                                        },
                                        new StyleOverride
                                            { width = GenUI.SmallIconSize, height = GenUI.SmallIconSize });
                                }
                            },
                            new StyleOverride
                            {
                                flexWrap = FlexWrap.Wrap, alignContent = AlignContent.Start,
                                gap = Taffy.Gap(GenUI.GapTiny)
                            });
                        rightBuilder.Div(colorReadoutBuilder =>
                            {
                                colorReadoutBuilder.Item(r => Verse.Widgets.DrawRectFast(r, _oldColor),
                                    new StyleOverride { flexGrow = 1f });
                                colorReadoutBuilder.Item(r => Verse.Widgets.DrawRectFast(r, _selectedColor),
                                    new StyleOverride { flexGrow = 1f });
                            },
                            new StyleOverride
                                { height = Text.LineHeightOf(GameFont.Small), gap = Taffy.Gap(GenUI.GapTiny) });
                    },
                    new StyleOverride
                        { flexDirection = FlexDirection.Column, justifyContent = AlignContent.SpaceBetween });
            }, new StyleOverride { flexGrow = 1f, gap = Taffy.Gap(GenUI.Gap) });

            builder.Div(footerBuilder =>
            {
                footerBuilder.Button("Cancel", onClick: _ => Close(), size: UIUtility.ComponentSize.Large);
                footerBuilder.Button("Accept", onClick: _ => Accept(), size: UIUtility.ComponentSize.Large);
            }, new StyleOverride { justifyContent = AlignContent.SpaceBetween });
        }, new StyleOverride { flexDirection = FlexDirection.Column, gap = Taffy.Gap(0f, GenUI.GapSmall) });
    }

    private void Accept()
    {
        _onSelect.Invoke(_selectedColor);
        Close();
    }
}