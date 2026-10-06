//! The retained tree. C# describes the whole UI every frame as a flat pre-order list of [`NodeRecord`]s;
//! [`VoidTree::frame`] reconciles that against the previous frame, lays it out when anything changed, and
//! writes one [`NodeOut`] per record.

use taffy::compute_leaf_layout;
use taffy::prelude::*;
use taffy::style::Overflow;

use crate::measure::MeasureRegistry;
use crate::style::StyleRegistry;

/// One node of this frame's description, in pre-order (a parent always comes before its children).
#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct NodeRecord {
    /// The node's identity across frames, assigned by C#. Keys are small and dense: C# hands them out in order
    /// and reuses a key once this side has reported its node removed. They index [`VoidTree`]'s node table.
    pub key: u64,
    /// Index one past this node's last descendant. Children of record `i` are found by starting at `i + 1`
    /// and jumping from each child to its `subtree_end`.
    pub subtree_end: u32,
    /// Style handle, 0 = default style.
    pub style: u32,
    /// Measure handle, 0 = not measured.
    pub measure: u32,
    pub _pad: u32,
}

// Bits of `NodeOut::flags`.
pub const NODE_SCROLL_X: u32 = 0x1;
pub const NODE_SCROLL_Y: u32 = 0x2;

/// Layout of one record. Positions are absolute, except that children of a scroll container are relative
/// to the container's content (the coordinate space of `Widgets.BeginScrollView`).
#[repr(C)]
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct NodeOut {
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
    pub content_width: f32,
    pub content_height: f32,
    pub scrollbar_width: f32,
    pub scrollbar_height: f32,
    pub flags: u32,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct FrameResult {
    /// Keys of nodes that disappeared this frame; fetch them with `void_take_removed`. C# may reuse them after.
    pub removed_count: u32,
    /// 1 when layout was recomputed this frame.
    pub laid_out: u32,
    pub root_width: f32,
    pub root_height: f32,
}

struct NodeEntry {
    id: NodeId,
    style: u32,
    measure: u32,
    /// Frame generation in which this key was last described.
    seen: u32,
    children: Vec<NodeId>,
}

pub struct VoidTree {
    /// Node context is the measure handle.
    taffy: TaffyTree<u32>,
    /// Indexed by [`NodeRecord::key`]; `None` for keys not currently in use.
    nodes: Vec<Option<NodeEntry>>,
    generation: u32,
    last_available: Option<(f32, f32)>,
    removed: Vec<u64>,
    // Scratch buffers, reused every frame.
    ids: Vec<NodeId>,
    children: Vec<NodeId>,
    origins: Vec<(u32, f32, f32)>,
}

impl Default for VoidTree {
    fn default() -> Self {
        Self {
            taffy: TaffyTree::new(),
            nodes: Vec::new(),
            generation: 0,
            last_available: None,
            removed: Vec::new(),
            ids: Vec::new(),
            children: Vec::new(),
            origins: Vec::new(),
        }
    }
}

fn available(v: f32) -> AvailableSpace {
    if v.is_finite() && v >= 0.0 {
        AvailableSpace::Definite(v)
    } else if v == f32::NEG_INFINITY {
        AvailableSpace::MinContent
    } else {
        AvailableSpace::MaxContent
    }
}

impl VoidTree {
    pub fn node_count(&self) -> usize {
        self.nodes.iter().flatten().count()
    }

    /// Drains the keys removed by previous frames into `buf`, returning how many were written.
    pub fn take_removed(&mut self, buf: &mut [u64]) -> usize {
        let n = buf.len().min(self.removed.len());
        buf[..n].copy_from_slice(&self.removed[..n]);
        self.removed.drain(..n);
        n
    }

    fn validate(&self, records: &[NodeRecord], styles: &StyleRegistry, measures: &MeasureRegistry) -> Result<(), String> {
        let n = records.len();
        if n == 0 {
            return Err("frame has no records".into());
        }
        // Keys are dense, so a new key can at most extend the table by one per record. Anything larger is a bug
        // on the C# side, and would otherwise allocate a huge table.
        let max_key = self.nodes.len() + n;
        if records[0].subtree_end as usize != n {
            return Err(format!("root subtree_end is {}, expected {n}", records[0].subtree_end));
        }
        for (i, r) in records.iter().enumerate() {
            let end = r.subtree_end as usize;
            if end <= i || end > n {
                return Err(format!("record {i}: subtree_end {end} out of range"));
            }
            if r.key as usize >= max_key {
                return Err(format!("record {i}: key {} is not a dense key (table holds {})", r.key, self.nodes.len()));
            }
            if styles.get(r.style).is_none() {
                return Err(format!("record {i}: unknown style handle {}", r.style));
            }
            if !measures.contains(r.measure) {
                return Err(format!("record {i}: unknown measure handle {}", r.measure));
            }
        }
        Ok(())
    }

    #[allow(clippy::too_many_arguments)] // mirrors the C entry point one to one
    pub fn frame(
        &mut self,
        records: &[NodeRecord],
        available_width: f32,
        available_height: f32,
        origin: (f32, f32),
        styles: &StyleRegistry,
        measures: &mut MeasureRegistry,
        out: &mut [NodeOut],
    ) -> Result<FrameResult, String> {
        self.validate(records, styles, measures)?;
        if out.len() < records.len() {
            return Err(format!("output buffer holds {} entries, need {}", out.len(), records.len()));
        }

        self.generation = self.generation.wrapping_add(1);
        let generation = self.generation;
        let mut changed = false;

        // 1. Find or create the node for every record and bring its style and measure up to date.
        self.ids.clear();
        for (i, r) in records.iter().enumerate() {
            let slot = r.key as usize;
            if slot >= self.nodes.len() {
                self.nodes.resize_with(slot + 1, || None);
            }

            let entry = match &mut self.nodes[slot] {
                Some(entry) => {
                    if entry.seen == generation {
                        return Err(format!("record {i}: key {} appears twice in one frame", r.key));
                    }
                    if entry.style != r.style {
                        self.taffy.set_style(entry.id, styles.get(r.style).unwrap().clone()).map_err(|e| e.to_string())?;
                        entry.style = r.style;
                        changed = true;
                    }
                    if entry.measure != r.measure {
                        let ctx = (r.measure != 0).then_some(r.measure);
                        self.taffy.set_node_context(entry.id, ctx).map_err(|e| e.to_string())?;
                        entry.measure = r.measure;
                        changed = true;
                    }
                    entry
                }
                empty @ None => {
                    let style = styles.get(r.style).unwrap().clone();
                    let id = if r.measure != 0 {
                        self.taffy.new_leaf_with_context(style, r.measure)
                    } else {
                        self.taffy.new_leaf(style)
                    }
                    .map_err(|e| e.to_string())?;
                    changed = true;
                    empty.insert(NodeEntry {
                        id,
                        style: r.style,
                        measure: r.measure,
                        seen: generation,
                        children: Vec::new(),
                    })
                }
            };
            entry.seen = generation;
            self.ids.push(entry.id);
        }

        // 2. Attach children. Only parents whose child list differs from last frame are touched, which also
        //    covers pure reorders.
        for (i, r) in records.iter().enumerate() {
            let end = r.subtree_end as usize;
            self.children.clear();
            let mut j = i + 1;
            while j < end {
                let child_end = records[j].subtree_end as usize;
                if child_end > end {
                    return Err(format!("record {j}: subtree_end {child_end} exceeds its parent's ({end})"));
                }
                self.children.push(self.ids[j]);
                j = child_end;
            }

            let entry = self.nodes[r.key as usize].as_mut().unwrap();
            if entry.children != self.children {
                self.taffy.set_children(entry.id, &self.children).map_err(|e| e.to_string())?;
                entry.children.clone_from(&self.children);
                changed = true;
            }
        }

        // 3. Remove nodes that were not described this frame. Detach every removed node's children first, so
        //    no removal has to look at a node that is already gone.
        let first_removed = self.removed.len();
        for (key, slot) in self.nodes.iter().enumerate() {
            if matches!(slot, Some(entry) if entry.seen != generation) {
                self.removed.push(key as u64);
            }
        }
        if self.removed.len() > first_removed {
            changed = true;
            for &key in &self.removed[first_removed..] {
                let entry = self.nodes[key as usize].as_mut().unwrap();
                if !entry.children.is_empty() {
                    self.taffy.set_children(entry.id, &[]).map_err(|e| e.to_string())?;
                    entry.children.clear();
                }
            }
            for &key in &self.removed[first_removed..] {
                let entry = self.nodes[key as usize].take().unwrap();
                self.taffy.remove(entry.id).map_err(|e| e.to_string())?;
            }
        }

        // 4. Lay out, but only when something changed or the space did.
        let root = self.ids[0];
        let space = (available_width, available_height);
        let laid_out = changed || self.last_available != Some(space);
        if laid_out {
            self.taffy
                .compute_layout_with_measure(
                    root,
                    Size { width: available(available_width), height: available(available_height) },
                    |inputs, _node, context, style| {
                        compute_leaf_layout(inputs, style, |_, _| 0.0, |known, avail| match context {
                            Some(&mut handle) => measures.measure(handle, known, avail),
                            None => Size::ZERO,
                        })
                    },
                )
                .map_err(|e| e.to_string())?;
            self.last_available = Some(space);
        }

        // 5. Write absolute rects. The stack holds (subtree_end, child origin) for each open ancestor.
        self.origins.clear();
        for (i, r) in records.iter().enumerate() {
            while matches!(self.origins.last(), Some(&(end, _, _)) if end as usize <= i) {
                self.origins.pop();
            }
            let (ox, oy) = self.origins.last().map_or(origin, |&(_, x, y)| (x, y));

            let layout = self.taffy.layout(self.ids[i]).map_err(|e| e.to_string())?;
            let style = styles.get(r.style).unwrap();
            let mut flags = 0;
            if style.overflow.x == Overflow::Scroll {
                flags |= NODE_SCROLL_X;
            }
            if style.overflow.y == Overflow::Scroll {
                flags |= NODE_SCROLL_Y;
            }

            let o = NodeOut {
                x: ox + layout.location.x,
                y: oy + layout.location.y,
                width: layout.size.width,
                height: layout.size.height,
                // The overflow rect is measured from the padding box; children are positioned from the border
                // box, which is what the scroll view's content rect has to cover.
                content_width: layout.border.left + layout.scrollable_overflow_rect.right,
                content_height: layout.border.top + layout.scrollable_overflow_rect.bottom,
                scrollbar_width: layout.scrollbar_size.width,
                scrollbar_height: layout.scrollbar_size.height,
                flags,
            };
            out[i] = o;

            if r.subtree_end as usize > i + 1 {
                let child_origin = if flags != 0 { (0.0, 0.0) } else { (o.x, o.y) };
                self.origins.push((r.subtree_end, child_origin.0, child_origin.1));
            }
        }

        Ok(FrameResult {
            removed_count: self.removed.len() as u32,
            laid_out: laid_out as u32,
            root_width: out[0].width,
            root_height: out[0].height,
        })
    }
}
