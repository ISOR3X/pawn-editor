namespace Void;

public interface IContext { }

public interface IContext<T> : IContext
{
    T Value { get; }
}
