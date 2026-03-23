// Port of taffy/src/tree/node.rs

using System;

namespace PawnEditor.TaffySharp
{
    /// <summary>
    /// An opaque identifier for a node in a <see cref="TaffyTree"/>.
    /// Internally wraps a <c>uint</c> index.
    /// </summary>
    public readonly struct NodeId : IEquatable<NodeId>
    {
        private readonly uint _id;

        public NodeId(uint id) => _id = id;
        public static NodeId From(uint id) => new NodeId(id);

        public uint Value => _id;

        public bool Equals(NodeId other) => _id == other._id;
        public override bool Equals(object? obj) => obj is NodeId other && Equals(other);
        public override int GetHashCode() => (int)_id;
        public static bool operator ==(NodeId a, NodeId b) => a._id == b._id;
        public static bool operator !=(NodeId a, NodeId b) => a._id != b._id;
        public override string ToString() => $"NodeId({_id})";
    }
}
