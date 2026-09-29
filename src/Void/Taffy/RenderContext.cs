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
    private readonly string _key;
    private readonly object? _value;

    private RenderContext(RenderContext? parent, string key, object? value)
    {
        (_parent, _key, _value) = (parent, key, value);
    }

    public static RenderContext Empty { get; } = new(null, "empty", null);

    public RenderContext With<T>(string key, T value)
    {
        return new RenderContext(this, key, value);
    }

    public bool TryGet<T>(string key, out T value)
    {
        for (var c = this; c is not null; c = c._parent)
            if (c._key == key)
            {
                if (c._value is T t) { value = t; return true; }
                value = default!;
                return false;
            }

        value = default!;
        return false;
    }

    public T Get<T>(string key)
    {
        return TryGet<T>(key, out var v)
            ? v
            : throw new InvalidOperationException($"No {typeof(T)} attached in this scope");
    }
}
