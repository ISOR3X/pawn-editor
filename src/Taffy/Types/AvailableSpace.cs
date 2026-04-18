// Port of taffy/src/style/available_space.rs

namespace Taffy;

/// <summary>
///     The amount of space available to a node along one axis.
///     <para>
///         <see cref="Definite" />: a concrete pixel budget.<br />
///         <see cref="MinContent" />: lay out under min-content constraints.<br />
///         <see cref="MaxContent" />: lay out under max-content constraints (effectively unbounded).
///     </para>
/// </summary>
public readonly struct AvailableSpace : IEquatable<AvailableSpace>
{
    private enum Kind : byte
    {
        Definite,
        MinContent,
        MaxContent
    }

    private readonly Kind _kind;
    private readonly float _value; // only meaningful for Definite

    private AvailableSpace(Kind kind, float value = 0f)
    {
        _kind = kind;
        _value = value;
    }

    // ── Factory methods ───────────────────────────────────────────────────

    public static AvailableSpace Definite(float value)
    {
        return new AvailableSpace(Kind.Definite, value);
    }

    public static readonly AvailableSpace MinContent = new(Kind.MinContent);
    public static readonly AvailableSpace MaxContent = new(Kind.MaxContent);
    public static readonly AvailableSpace ZERO = Definite(0f);

    // ── Conversions ───────────────────────────────────────────────────────

    public static implicit operator AvailableSpace(float value)
    {
        return Definite(value);
    }

    public static implicit operator AvailableSpace(float? option)
    {
        return option.HasValue ? Definite(option.Value) : MaxContent;
    }

    // ── Query helpers ─────────────────────────────────────────────────────

    public bool IsDefinite => _kind == Kind.Definite;
    public bool IsMinContent => _kind == Kind.MinContent;
    public bool IsMaxContent => _kind == Kind.MaxContent;

    /// <summary>Returns the definite value, or <c>null</c> for constraints.</summary>
    public float? IntoOption()
    {
        return IsDefinite ? _value : null;
    }

    /// <summary>Returns the definite value, or <paramref name="default" /> if constrained.</summary>
    public float UnwrapOr(float @default)
    {
        return IsDefinite ? _value : @default;
    }

    /// <summary>Returns the definite value. Throws if not definite.</summary>
    public float Unwrap()
    {
        if (!IsDefinite) throw new InvalidOperationException("AvailableSpace is not Definite");
        return _value;
    }

    /// <summary>Returns self if definite, otherwise <paramref name="default" />.</summary>
    public AvailableSpace Or(AvailableSpace @default)
    {
        return IsDefinite ? this : @default;
    }

    /// <summary>If <paramref name="value" /> is non-null, returns Definite(value); otherwise returns self.</summary>
    public AvailableSpace MaybeSet(float? value)
    {
        return value.HasValue ? Definite(value.Value) : this;
    }

    /// <summary>Maps the inner value for Definite, passes through constraints unchanged.</summary>
    public AvailableSpace MapDefiniteValue(Func<float, float> f)
    {
        return IsDefinite ? Definite(f(_value)) : this;
    }

    /// <summary>
    ///     Computes free space given <paramref name="usedSpace" />:
    ///     MaxContent → ∞, MinContent → 0, Definite → available − used.
    /// </summary>
    public float ComputeFreeSpace(float usedSpace)
    {
        return _kind switch
        {
            Kind.MaxContent => float.PositiveInfinity,
            Kind.MinContent => 0f,
            _ => _value - usedSpace
        };
    }

    /// <summary>
    ///     Epsilon-aware equality for definite values; exact equality for constraints.
    /// </summary>
    public bool IsRoughlyEqual(AvailableSpace other)
    {
        if (_kind != other._kind) return false;
        if (_kind == Kind.Definite) return MathF.Abs(_value - other._value) < float.Epsilon;
        return true;
    }

    // ── Equality ──────────────────────────────────────────────────────────

    public bool Equals(AvailableSpace other)
    {
        return _kind == other._kind && _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AvailableSpace other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)_kind, _value);
    }

    public static bool operator ==(AvailableSpace a, AvailableSpace b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(AvailableSpace a, AvailableSpace b)
    {
        return !a.Equals(b);
    }

    public override string ToString()
    {
        return _kind switch
        {
            Kind.Definite => $"Definite({_value})",
            Kind.MinContent => "MinContent",
            _ => "MaxContent"
        };
    }
}

/// <summary>Extension helpers for <see cref="Size{T}" /> of <see cref="AvailableSpace" />.</summary>
public static class AvailableSpaceSizeExt
{
    /// <summary>Convert <see cref="Size{T}" /> of <see cref="AvailableSpace" /> to <see cref="Size{T}" /> of <c>float?</c>.</summary>
    public static Size<float?> IntoOptions(this Size<AvailableSpace> s)
    {
        return new Size<float?>(s.Width.IntoOption(), s.Height.IntoOption());
    }

    /// <summary>
    ///     For each axis: if the corresponding value in <paramref name="value" /> is non-null,
    ///     override that axis with Definite(value); otherwise keep the current constraint.
    /// </summary>
    public static Size<AvailableSpace> MaybeSet(this Size<AvailableSpace> s, Size<float?> value)
    {
        return new Size<AvailableSpace>(s.Width.MaybeSet(value.Width), s.Height.MaybeSet(value.Height));
    }
}