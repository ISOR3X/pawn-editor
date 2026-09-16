namespace Void.Taffy;

/// <summary>
///     Provide scoped context to child branches.
///     Similar to provide/inject from Vue.
///     An immutable linked list: <see cref="With{T}" /> returns a new link, so a scope can never
///     mutate the scope it was handed. Lookup walks toward the root, so the innermost
///     <see cref="With{T}" /> of a type shadows any outer one.
/// </summary>
public sealed class RenderContext
{
    private readonly RenderContext? _parent;
    private readonly Type _type;
    private readonly object? _value;

    private RenderContext(RenderContext? parent, Type type, object? value)
    {
        (_parent, _type, _value) = (parent, type, value);
    }

    public static RenderContext Empty { get; } = new(null, typeof(void), null);

    /// <summary>
    ///     Note that the key is the STATIC type of <paramref name="value" />: providing a
    ///     <c>Pawn</c> as <c>Thing</c> makes it retrievable as <c>Thing</c> only, and vice versa.
    /// </summary>
    public RenderContext With<T>(T value)
    {
        return new RenderContext(this, typeof(T), value);
    }

    public bool TryGet<T>(out T value)
    {
        for (var c = this; c is not null; c = c._parent)
            if (c._type == typeof(T))
            {
                value = (T)c._value!;
                return true;
            }

        value = default!;
        return false;
    }

    public T Get<T>()
    {
        return TryGet<T>(out var v)
            ? v
            : throw new InvalidOperationException($"No {typeof(T)} attached in this scope");
    }
}
