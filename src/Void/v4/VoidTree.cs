using Taffy;
using UnityEngine;
using Verse;

namespace Void.v4
{
    /// <summary>
    /// Context used for calculating the size of a leaf.
    /// Modifying context marks a leaf dirty.
    /// </summary>
    public record LeafContext
    {
        public GameFont? font;
        public string? text;
    }

    /// <summary>
    /// Stores relevant data about a branch on the C# side to prevent a large amount of FFI calls per frame.
    /// Used for diff-checking and storing the actual draw callback.
    /// </summary>
    public class BranchRecord
    {
        /// <summary>
        /// Track the previous style of a branch so we can diff-check it.
        /// Context is tracked internally in the TaffyTree.
        /// </summary>
        public StyleOverride? prevStyle;

        /// <summary>
        /// Context is tracked internally in the TaffyTree as its also required for content measuring.
        /// </summary>
        // public LeafContext? prevCtx;

        /// <summary>
        /// The child branches/ leaves of a branch
        /// </summary>
        public Dictionary<string, TaffyNode> childrenByKey = [];

        /// <summary>
        /// The pending children by key. This is build up every frame and cleared at the end.
        /// It is used to check if the children have changed across frames.
        /// </summary>
        public Dictionary<string, TaffyNode> pendingChildrenByKey = [];


        public Action<Rect>? draw = null;
    }

    public class RenderTree : IDisposable
    {
        /// <summary>
        /// The native tree. We use a private version of the tree instead of subclassing it to control the public API.
        /// </summary>
        private readonly TaffyTree<LeafContext> _tree = new();

        /// <summary>
        /// The node of the root branch.
        /// </summary>
        private readonly TaffyNode _rootBranchNode;

        /// <summary>
        /// Records keyed by their branch node.
        /// </summary>
        private readonly Dictionary<TaffyNode, BranchRecord> _branchRecordsByNode = [];

        // Tracks whether anything changed since last frame
        private bool _dirtyThisFrame = true;
        private float _lastAvailableWidth = -1f;
        private float _lastAvailableHeight = -1f;

        public RenderTree()
        {
            _rootBranchNode = _tree.NewNode();
            _branchRecordsByNode[_rootBranchNode] = new BranchRecord();
        }

        public void Build(Action<Branch> builder, StyleOverride? style = null)
        {
            var branch = new Branch(this, _rootBranchNode);
            var rootRecord = _branchRecordsByNode[_rootBranchNode];

            // If the styles are not equal, and the new style isn't null, update the native style.
            if (!Utility.NullableEquals(style, rootRecord.prevStyle, equalIfNull: false) && style != null)
            {
                _dirtyThisFrame = true;
                var _branchStyle = _tree.GetStyle(_rootBranchNode);
                style.Push(_branchStyle);
                _tree.SetStyle(_rootBranchNode, _branchStyle);
            }
            // Update the style history regardless of if its null or not.
            rootRecord.prevStyle = style;

            builder(branch);
        }

        public void Draw(Rect rect)
        {
            var sizeChanged = !Mathf.Approximately(rect.width, _lastAvailableWidth)
                || !Mathf.Approximately(rect.height, _lastAvailableHeight);

            if (_dirtyThisFrame || sizeChanged)
            {
                _tree.ComputeLayoutWithMeasure(_rootBranchNode, rect.width, rect.height, DefaultMeasure);

                _dirtyThisFrame = false;
                _lastAvailableWidth = rect.width;
                _lastAvailableHeight = rect.height;
            }

            DrawNode(_rootBranchNode, rect.position);
        }

        private void DrawNode(TaffyNode node, Vector2 origin)
        {
            var layout = _tree.GetLayout(node);
            var rect = new Rect(origin.x + layout.x, origin.y + layout.y, layout.width, layout.height);

            if (!_branchRecordsByNode.TryGetValue(node, out var record)) return;

            record.draw?.Invoke(rect);
            foreach (var (_, childId) in record.childrenByKey)
                DrawNode(childId, rect.position);
        }

        private static TaffySize DefaultMeasure(
            TaffyMeasureMode widthMode, float width,
            TaffyMeasureMode heightMode, float height,
            LeafContext? context)
        {
            if (context is null || context.text == null) return new TaffySize();
            using (new TextBlock(context.font ?? GameFont.Small))
            {
                switch (widthMode)
                {
                    case TaffyMeasureMode.Exact or TaffyMeasureMode.FitContent:
                        return new TaffySize { width = width, height = Text.CalcHeight(context.text, width) };
                    case TaffyMeasureMode.MinContent:
                        {
                            var minW = context.text.Split(' ').Select(w => Text.CalcSize(w).x).Prepend(0f).Max();
                            return new TaffySize { width = minW, height = Text.CalcHeight(context.text, minW) };
                        }
                    default:
                        {
                            var sz = Text.CalcSize(context.text);
                            return new TaffySize { width = sz.x, height = sz.y };
                        }
                }
            }
        }

        /// <summary>
        /// Update or insert a new branch into the parent node.
        /// </summary>
        public TaffyNode UpsertBranch(TaffyNode parentNode, string key, Action<Rect>? draw = null, LeafContext? context = null, StyleOverride? style = null)
        {
            var parentRecord = _branchRecordsByNode[parentNode];
            var exists = parentRecord.childrenByKey.TryGetValue(key, out var branchNode);

            // Check if the branch exists according to the parent record
            if (!exists)
            {
                // Create a new node and record
                branchNode = context != null ? _tree.NewLeafWithContext(context) : _tree.NewNode();
                _branchRecordsByNode[branchNode] = new BranchRecord();

                // Update the parent
                parentRecord.childrenByKey[key] = branchNode;
                _tree.AppendChild(parentNode, branchNode);
                _dirtyThisFrame = true;
            }
            var branchRecord = _branchRecordsByNode[branchNode];

            // Apply style and context if they have changed and are not null.
            if (!Utility.NullableEquals(style, branchRecord.prevStyle, equalIfNull: false) && style != null)
            {
                _dirtyThisFrame = true;
                var _branchStyle = _tree.GetStyle(branchNode);
                style.Push(_branchStyle);
                _tree.SetStyle(branchNode, _branchStyle);
            }

            if (!Utility.NullableEquals(context, _tree.GetNodeContext(branchNode), equalIfNull: false) && context != null)
            {
                _dirtyThisFrame = true;
                _tree.SetNodeContext(branchNode, context);
            }

            // Always update the branchRecord with the values of the current frame.
            branchRecord.prevStyle = style;
            branchRecord.draw = draw;

            parentRecord.pendingChildrenByKey[key] = branchNode;

            return branchNode;
        }

        /// <summary>
        /// Compare the children of the previous and current frame.
        /// First compare if the length and order are the same,
        /// then check for added/ removed items.
        /// If differences are found, free the removed branches.
        ///
        /// NOTE: While SameOrder checks for the order of children, if just the order of the array has changed and not the length,
        /// No changes are actually pushed to the native tree.
        /// </summary>
        public void CompareChildren(TaffyNode node)
        {
            var branchRecord = _branchRecordsByNode[node];

            var prevChildKeysInOrder = branchRecord.childrenByKey.Keys.ToArray();
            var pendingChildKeysInOrder = branchRecord.pendingChildrenByKey.Keys.ToArray();

            if (SameOrder(prevChildKeysInOrder, pendingChildKeysInOrder))
            {
                branchRecord.childrenByKey = branchRecord.pendingChildrenByKey;
                branchRecord.pendingChildrenByKey = [];
                return;
            }

            _dirtyThisFrame = true;

            var removed = prevChildKeysInOrder.Except(pendingChildKeysInOrder);
            // var added = pendingChildKeysInOrder.Except(prevChildKeysInOrder);

            foreach (string r in removed)
            {
                var childNode = branchRecord.childrenByKey[r];
                RemoveSubtree(childNode);

            }

            /*
            // Branches are already added by the UpsertBranch function.
            foreach (string a in added)
            {
                var childNode = branchRecord.pendingChildrenByKey[a];
                _tree.AppendChild(node, childNode);
            }
             */

            branchRecord.childrenByKey = branchRecord.pendingChildrenByKey;
            branchRecord.pendingChildrenByKey = [];
        }

        private static bool SameOrder(string[] a, string[] b)
        {
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private void RemoveSubtree(TaffyNode node)
        {
            if (_branchRecordsByNode.TryGetValue(node, out var record))
            {
                foreach (var childNode in record.childrenByKey.Values)
                    RemoveSubtree(childNode);
                _branchRecordsByNode.Remove(node);
            }
            _tree.RemoveNode(node);
        }

        public void Dispose()
        {
            _tree.Dispose();
        }
    }

    /// <summary>
    /// Builder for creating a branch in a tree.
    /// </summary>
    public class Branch(RenderTree tree, TaffyNode rootBranch)
    {
        private readonly RenderTree _tree = tree;
        private readonly TaffyNode _rootBranch = rootBranch;

        public TaffyNode Div(string key, Action<Branch>? builder = null, Action<Rect>? draw = null, LeafContext? context = null, StyleOverride? style = null)
        {
            var node = _tree.UpsertBranch(_rootBranch, key, draw, context, style);
            // Create a new branch to which the children can attach.
            var childBranch = new Branch(_tree, node);

            builder?.Invoke(childBranch);

            // After the builder is called, all (this frame's) children should be attached.
            // Compare them to last frame and update the snapshot accordingly.
            // Also marks the native tree dirty when differences are found.
            _tree.CompareChildren(node);

            return node;
        }

        public TaffyNode Text(string key, string text, GameFont font = GameFont.Small, TextAnchor align = TextAnchor.UpperLeft)
        {
            var ctx = new LeafContext { text = text, font = font };
            void draw(Rect r)
            {
                using (new TextBlock(font, align))
                {
                    Verse.Text.WordWrap = true;
                    Verse.Widgets.Label(r, text);
                }
            }

            var node = _tree.UpsertBranch(_rootBranch, key, context: ctx, draw: draw);

            return node;
        }
    }

    static class Utility
    {
        public static bool NullableEquals<T>(T? x, T? y, bool equalIfNull = true)
        {
            if (x is null && y is null) return equalIfNull;
            return EqualityComparer<T>.Default.Equals(x!, y!);
        }
    }
}
