namespace Void.XMLComponents;

public class DivElement : XMLComponent
{
    /// <summary>
    /// If set, C# fully owns children. If null, XML children are rendered via the default tree walk.
    /// </summary>
    public Action<TaffyBuilder>? Children;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
        => builder.Div(Children ?? children ?? (_ => { }), Style);
}