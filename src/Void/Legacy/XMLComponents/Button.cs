using System.Xml;
using UnityEngine;
using Verse;
using Void.Components;

namespace Void.XMLComponents;

public class ButtonElement : XMLComponent
{
    /// <summary>Icon name from XML, resolved lazily on first render to avoid loading textures at parse time.</summary>
    private string? _iconName;

    public bool Disabled;
    public Texture2D? Icon;
    public Color? IconColor;

    public string? Label;

    public Action<Rect>? OnClick;
    public Action<Rect>? OnHover;
    public TaffyExtensions.ButtonVariant Variant;

    public override void ParseXmlAttrs(XmlNode node)
    {
        if (node.Attributes?["label"]?.Value is { } label) Label = label;
        if (node.Attributes?["icon"]?.Value is { } icon) _iconName = icon;
        if (node.Attributes?["variant"]?.Value is { } variant)
            if (Enum.TryParse(variant.CapitalizeFirst(), out TaffyExtensions.ButtonVariant result))
                Variant = result;

        ;
    }

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        // Resolve icon name lazily - UIIcons accesses textures which aren't loaded at def-load time.
        if (Icon == null && _iconName != null)
            Icon = IconRegistry.Resolve(_iconName);
        builder.Button(Label, Icon, IconColor, OnClick, OnHover, disabled: Disabled, variant: Variant, style: Style);
    }
}