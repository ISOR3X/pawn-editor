using System.Xml;
using Verse;
using Void.Components;

namespace Void.XMLComponents;

public class InputElement : XMLComponent
{
    public Ref? Value;
    public FloatRange? MinMax;
    private string _inputType = "text";

    public override void ParseXmlAttrs(XmlNode node)
    {
        if (node.Attributes?["type"]?.Value is { } variant)
        {
            _inputType = variant switch
            {
                "range" => "range",
                "number" => "number",
                _ => _inputType
            };
        }
    }

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        // Resolved min max.
        var resMm =  MinMax ?? new FloatRange(0, 9999);
        switch (_inputType)
        {
            case "text" when Value is Ref<string> s:
                builder.Input(ref s.Inner, style: Style);
                break;

            case "number" when Value is Ref<int> f:
                builder.InputNumber(ref f.Inner, style: Style);
                break;

            case "range" when Value is Ref<float> f:
                builder.InputRange(ref f.Inner, min: resMm.min, max: resMm.max, style: Style);
                break;
            case "range" when Value is Ref<int> i:
                builder.InputRange(() => i.Inner, v => i.Inner = (int)v, min: resMm.min, max: resMm.max, style: Style);
                break;

            case "range" when Value is Reactive<float> f:
                builder.InputRange(() => f.Inner, v => f.Inner = v, min: resMm.min, max: resMm.max, style: Style);
                break;
            case "range" when Value is Reactive<int> i:
                builder.InputRange(() => i.Inner, v => i.Inner = (int)v, min: resMm.min, max: resMm.max, style: Style);
                break;
        }
    }
}