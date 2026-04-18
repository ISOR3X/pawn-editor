using System.Xml;
using UnityEngine;
using Void.Components;

namespace Void.XMLComponents;

public class ButtonElement : XMLComponent
{
    public string? Label;
    public Texture2D? Icon;

    /// <summary>Icon name from XML, resolved lazily on first render to avoid loading textures at parse time.</summary>
    public string? IconName;

    public Action<Rect>? OnClick;
    public Action<Rect>? OnHover;

    public override void ParseXmlAttrs(XmlNode node)
    {
        if (node.Attributes?["label"]?.Value is { } label) Label = label;
        if (node.Attributes?["icon"]?.Value is { } icon) IconName = icon;
    }

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        // Resolve icon name lazily — UIIcons accesses textures which aren't loaded at def-load time.
        if (Icon == null && IconName != null)
            Icon = UIIcons.Resolve(IconName);
        builder.Button(Label, Icon, onClick: OnClick, onHover: OnHover, style: Style);
    }
}