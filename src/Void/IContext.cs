namespace Void;

/// <summary>
///     Pass any value as context.
/// </summary>
public interface IContext
{
}

public interface IContext<out T> : IContext
{
    T Value { get; }
}

// TODO: Combine with IContext?
public abstract class Ref;

public class Ref<T>(T inner) : Ref
{
    public T Inner = inner;
}

public class Reactive<T>(Func<T> get, Action<T> set) : Ref
{
    public T Inner
    {
        get => get();
        set => set(value);
    }
}