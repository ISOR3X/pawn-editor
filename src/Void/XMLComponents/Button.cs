using System.Xml;
using UnityEngine;
using Void.Components;

namespace Void.XMLComponents;

public class ButtonElement : XMLComponent
{
    public Texture2D? Icon;
    public Color? IconColor;
    public bool Disabled;

    /// <summary>Icon name from XML, resolved lazily on first render to avoid loading textures at parse time.</summary>
    private string? _iconName;

    public string? Label;

    public Action<Rect>? OnClick;
    public Action<Rect>? OnHover;

    public override void ParseXmlAttrs(XmlNode node)
    {
        if (node.Attributes?["label"]?.Value is { } label) Label = label;
        if (node.Attributes?["icon"]?.Value is { } icon) _iconName = icon;
    }

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        // Resolve icon name lazily — UIIcons accesses textures which aren't loaded at def-load time.
        if (Icon == null && _iconName != null)
            Icon = IconRegistry.Resolve(_iconName);
        builder.Button(Label, Icon, IconColor, onClick: OnClick, onHover: OnHover, disabled: Disabled, style: Style);
    }
}