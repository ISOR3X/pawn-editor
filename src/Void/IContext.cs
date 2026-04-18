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