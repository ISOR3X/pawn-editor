// Port of taffy/src/tree/node.rs

namespace Taffy;

/// <summary>
///     An opaque identifier for a node in a <see cref="TaffyTree" />.
///     Internally wraps a <c>uint</c> index.
/// </summary>
public readonly struct NodeId : IEquatable<NodeId>
{
    public NodeId(uint id)
    {
        Value = id;
    }

    public static NodeId From(uint id)
    {
        return new NodeId(id);
    }

    public uint Value { get; }

    public bool Equals(NodeId other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is NodeId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Value;
    }

    public static bool operator ==(NodeId a, NodeId b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(NodeId a, NodeId b)
    {
        return a.Value != b.Value;
    }

    public override string ToString()
    {
        return $"NodeId({Value})";
    }
}