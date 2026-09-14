namespace Void;

/// <summary>
///     Read-only context value. Covariant — a <c>IContext&lt;Dog&gt;</c> can be used where
///     <c>IContext&lt;Animal&gt;</c> is expected. Use when you only need to read the value.
/// </summary>
public interface IContext;

/// <inheritdoc />
public interface IContext<out T> : IContext
{
    T Value { get; }
}

/// <summary>
///     Mutable reference container. Unlike <see cref="IContext{T}" />, <c>Inner</c> can be
///     reassigned, so covariance is not possible. Use when the holder needs to write back.
/// </summary>
public abstract class Ref;

/// <inheritdoc />
public class Ref<T>(T inner) : Ref
{
    public T Inner = inner;
}

/// <summary>
///     A <see cref="Ref{T}" /> whose value is computed on access via a getter/setter pair
///     rather than stored directly.
/// </summary>
public class Reactive<T>(Func<T> get, Action<T> set) : Ref
{
    public T Inner
    {
        get => get();
        set => set(value);
    }
}