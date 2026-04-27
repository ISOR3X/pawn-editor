using Void.Components;

namespace Void.XMLComponents;

public class InputElement : XMLComponent
{
    public Ref<string>? Value;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        if (Value != null)
            builder.Input(ref Value.Inner);
    }
}

public class Ref<T>(T inner)
{
    public T Inner = inner;
}