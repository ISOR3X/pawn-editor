using System;
using UnityEngine;

namespace FlexLayout;

/// <summary>
///     A CSS-style length value that can be a pixel value, a percentage of a parent size, or Auto.
///     Auto means the layout engine decides the size based on content or available space.
/// </summary>
public readonly struct StyleSize : IEquatable<StyleSize>
{
    public enum Kind
    {
        Px,
        Pct,
        Auto
    }

    public readonly Kind type;

    /// <summary>
    ///     The raw value. For Px: pixels. For Pct: 0..100. Unused for Auto.
    /// </summary>
    public readonly float value;

    private StyleSize(Kind type, float value)
    {
        this.type = type;
        this.value = value;
    }

    /// <summary>Fixed pixel size.</summary>
    public static StyleSize Px(float pixels)
    {
        return new StyleSize(Kind.Px, pixels);
    }

    /// <summary>Percentage of the parent's size along the same axis. 0..100.</summary>
    public static StyleSize Pct(float percent)
    {
        return new StyleSize(Kind.Pct, percent);
    }

    /// <summary>Let the layout engine decide.</summary>
    public static StyleSize Auto()
    {
        return new StyleSize(Kind.Auto, 0f);
    }

    public bool IsAuto => type == Kind.Auto;
    public bool IsPx => type == Kind.Px;
    public bool IsPct => type == Kind.Pct;

    /// <summary>
    ///     Resolves this length to an absolute pixel value given a parent size.
    ///     Returns null if Auto (caller must handle).
    /// </summary>
    public float? Resolve(float parentSize)
    {
        return type switch
        {
            Kind.Px => value,
            Kind.Pct => parentSize * (value / 100f),
            Kind.Auto => null,
            _ => throw new InvalidOperationException($"Unknown StyleLength kind: {type}")
        };
    }

    /// <summary>
    ///     Resolves to a pixel value, returning the provided fallback if Auto.
    /// </summary>
    public float ResolveOr(float parentSize, float fallback)
    {
        return Resolve(parentSize) ?? fallback;
    }

    public bool Equals(StyleSize other)
    {
        return type == other.type && Mathf.Approximately(value, other.value);
    }

    public override bool Equals(object? obj)
    {
        return obj is StyleSize s && Equals(s);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(type, value);
    }

    public static bool operator ==(StyleSize a, StyleSize b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(StyleSize a, StyleSize b)
    {
        return !a.Equals(b);
    }

    public override string ToString() => type switch
    {
        Kind.Px => $"Px({value})",
        Kind.Pct => $"Pct({value}%)",
        Kind.Auto => "Auto",
        _ => "Unknown"
    };


    // Convenience: implicit from float → Px
    public static implicit operator StyleSize(float px)
    {
        return Px(px);
    }
}