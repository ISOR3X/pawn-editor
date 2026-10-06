use std::sync::atomic::{AtomicU32, Ordering};

use taffy::prelude::*;
use taffy::geometry::Point;
use taffy::style::Overflow;

use crate::measure::{self, MeasureDesc};
use crate::style::StyleRegistry;
use crate::tree::{FrameResult, NODE_SCROLL_Y, NodeOut, NodeRecord, VoidTree};
use crate::{Registries, measure::MeasureRegistry};

/// Test node: key, style handle, measure handle, children.
struct N(u64, u32, u32, Vec<N>);

fn flatten(node: &N, out: &mut Vec<NodeRecord>) {
    let idx = out.len();
    out.push(NodeRecord { key: node.0, subtree_end: 0, style: node.1, measure: node.2, _pad: 0 });
    for child in &node.3 {
        flatten(child, out);
    }
    out[idx].subtree_end = out.len() as u32;
}

struct Harness {
    tree: VoidTree,
    regs: Registries,
}

impl Harness {
    fn new() -> Self {
        Self { tree: VoidTree::default(), regs: Registries::default() }
    }

    fn style(&mut self, s: Style) -> u32 {
        self.regs.styles.register(s)
    }

    fn frame(&mut self, root: &N, w: f32, h: f32) -> (FrameResult, Vec<NodeOut>) {
        self.try_frame(root, w, h).unwrap()
    }

    fn try_frame(&mut self, root: &N, w: f32, h: f32) -> Result<(FrameResult, Vec<NodeOut>), String> {
        let mut records = Vec::new();
        flatten(root, &mut records);
        let mut out = vec![NodeOut::default(); records.len()];
        let r = self.tree.frame(&records, w, h, (10.0, 20.0), &self.regs.styles, &mut self.regs.measures, &mut out)?;
        Ok((r, out))
    }

    fn removed(&mut self) -> Vec<u64> {
        let mut buf = [0u64; 64];
        let n = self.tree.take_removed(&mut buf);
        let mut v = buf[..n].to_vec();
        v.sort();
        v
    }
}

fn column() -> Style {
    Style { flex_direction: FlexDirection::Column, size: Size { width: percent(1.0), height: auto() }, ..Default::default() }
}

fn fixed(w: f32, h: f32) -> Style {
    Style { size: Size { width: length(w), height: length(h) }, flex_shrink: 0.0, ..Default::default() }
}

#[test]
fn lays_out_with_absolute_positions() {
    let mut h = Harness::new();
    let col = h.style(column());
    let a = h.style(fixed(50.0, 30.0));
    let b = h.style(fixed(60.0, 40.0));

    let (r, out) = h.frame(&N(0, col, 0, vec![N(1, a, 0, vec![]), N(2, b, 0, vec![])]), 200.0, 500.0);

    assert_eq!(r.laid_out, 1);
    assert_eq!((out[0].x, out[0].y, out[0].width, out[0].height), (10.0, 20.0, 200.0, 70.0));
    assert_eq!((out[1].x, out[1].y, out[1].width, out[1].height), (10.0, 20.0, 50.0, 30.0));
    assert_eq!((out[2].x, out[2].y, out[2].width, out[2].height), (10.0, 50.0, 60.0, 40.0));
    assert_eq!(h.tree.node_count(), 3);
}

#[test]
fn unchanged_frame_skips_layout() {
    let mut h = Harness::new();
    let col = h.style(column());
    let a = h.style(fixed(50.0, 30.0));
    let tree = N(0, col, 0, vec![N(1, a, 0, vec![])]);

    let (first, out1) = h.frame(&tree, 200.0, 500.0);
    let (second, out2) = h.frame(&tree, 200.0, 500.0);
    let (resized, _) = h.frame(&tree, 300.0, 500.0);

    assert_eq!(first.laid_out, 1);
    assert_eq!(second.laid_out, 0);
    assert_eq!(out1, out2);
    assert_eq!(resized.laid_out, 1);
}

#[test]
fn style_change_relayouts() {
    let mut h = Harness::new();
    let col = h.style(column());
    let small = h.style(fixed(50.0, 30.0));
    let big = h.style(fixed(50.0, 90.0));

    h.frame(&N(0, col, 0, vec![N(1, small, 0, vec![])]), 200.0, 500.0);
    let (r, out) = h.frame(&N(0, col, 0, vec![N(1, big, 0, vec![])]), 200.0, 500.0);

    assert_eq!(r.laid_out, 1);
    assert_eq!(out[1].height, 90.0);
}

#[test]
fn removed_nodes_are_reported_including_descendants() {
    let mut h = Harness::new();
    let col = h.style(column());
    let a = h.style(fixed(50.0, 30.0));

    h.frame(&N(0, col, 0, vec![N(1, col, 0, vec![N(2, a, 0, vec![]), N(3, a, 0, vec![])]), N(4, a, 0, vec![])]), 200.0, 500.0);
    let (r, out) = h.frame(&N(0, col, 0, vec![N(4, a, 0, vec![])]), 200.0, 500.0);

    assert_eq!(r.laid_out, 1);
    assert_eq!(r.removed_count, 3);
    assert_eq!(h.removed(), vec![1, 2, 3]);
    assert_eq!(h.removed(), Vec::<u64>::new(), "taking drains the queue");
    assert_eq!(h.tree.node_count(), 2);
    assert_eq!(out[1].y, 20.0, "the remaining child moved up");
}

#[test]
fn removed_keys_can_be_reused_for_new_nodes() {
    let mut h = Harness::new();
    let col = h.style(column());
    let small = h.style(fixed(50.0, 30.0));
    let big = h.style(fixed(50.0, 90.0));

    h.frame(&N(0, col, 0, vec![N(1, small, 0, vec![])]), 200.0, 500.0);
    h.frame(&N(0, col, 0, vec![]), 200.0, 500.0);
    assert_eq!(h.removed(), vec![1]);

    // C# hands key 1 to an unrelated node; it must start fresh rather than inherit the old node.
    let (r, out) = h.frame(&N(0, col, 0, vec![N(1, big, 0, vec![])]), 200.0, 500.0);
    assert_eq!(r.laid_out, 1);
    assert_eq!(out[1].height, 90.0);
    assert_eq!(h.tree.node_count(), 2);
}

#[test]
fn rejects_keys_that_are_not_dense() {
    let mut h = Harness::new();
    let col = h.style(column());

    let err = h.try_frame(&N(0, col, 0, vec![N(1_000_000, col, 0, vec![])]), 100.0, 100.0).unwrap_err();
    assert!(err.contains("not a dense key"), "{err}");
}

#[test]
fn reorder_is_applied() {
    let mut h = Harness::new();
    let col = h.style(column());
    let a = h.style(fixed(50.0, 30.0));
    let b = h.style(fixed(50.0, 40.0));

    h.frame(&N(0, col, 0, vec![N(1, a, 0, vec![]), N(2, b, 0, vec![])]), 200.0, 500.0);
    let (r, out) = h.frame(&N(0, col, 0, vec![N(2, b, 0, vec![]), N(1, a, 0, vec![])]), 200.0, 500.0);

    assert_eq!(r.laid_out, 1);
    assert_eq!((out[1].y, out[1].height), (20.0, 40.0));
    assert_eq!((out[2].y, out[2].height), (60.0, 30.0));
}

#[test]
fn scroll_container_children_are_relative_to_its_content() {
    let mut h = Harness::new();
    let col = h.style(column());
    let scroller = h.style(Style {
        flex_direction: FlexDirection::Column,
        size: Size { width: length(100.0), height: length(50.0) },
        overflow: Point { x: Overflow::Visible, y: Overflow::Scroll },
        scrollbar_width: 16.0,
        ..Default::default()
    });
    let item = h.style(fixed(50.0, 30.0));
    let spacer = h.style(fixed(10.0, 7.0));

    let (_, out) = h.frame(
        &N(0, col, 0, vec![
            N(5, spacer, 0, vec![]),
            N(1, scroller, 0, vec![N(2, item, 0, vec![]), N(3, item, 0, vec![])]),
            N(4, item, 0, vec![]),
        ]),
        200.0,
        500.0,
    );

    assert_eq!(out[2].flags, NODE_SCROLL_Y);
    assert_eq!((out[2].x, out[2].y), (10.0, 27.0));
    assert_eq!(out[2].content_height, 60.0);
    assert_eq!(out[2].scrollbar_width, 16.0);
    assert_eq!((out[3].x, out[3].y), (0.0, 0.0), "first scroll child at content origin");
    assert_eq!((out[4].x, out[4].y), (0.0, 30.0));
    assert_eq!((out[5].x, out[5].y), (10.0, 77.0), "sibling after the scroller is absolute again");
}

#[test]
fn rejects_malformed_frames() {
    let mut h = Harness::new();
    let col = h.style(column());

    let dup = h.try_frame(&N(0, col, 0, vec![N(1, col, 0, vec![]), N(1, col, 0, vec![])]), 100.0, 100.0);
    assert!(dup.unwrap_err().contains("appears twice"));

    let unknown = h.try_frame(&N(0, 999, 0, vec![]), 100.0, 100.0);
    assert!(unknown.unwrap_err().contains("unknown style handle"));

    let records = [NodeRecord { key: 1, subtree_end: 2, style: 0, measure: 0, _pad: 0 }];
    let mut out = [NodeOut::default()];
    let bad = h.tree.frame(&records, 1.0, 1.0, (0.0, 0.0), &h.regs.styles, &mut h.regs.measures, &mut out);
    assert!(bad.unwrap_err().contains("subtree_end"));
}

static CALLS: AtomicU32 = AtomicU32::new(0);

/// Pretends every line is 10 wide and 20 tall: height = ceil(100 / width) lines.
extern "C" fn fake_height(_handle: u32, width: f32) -> f32 {
    CALLS.fetch_add(1, Ordering::SeqCst);
    (100.0 / width).ceil() * 20.0
}

#[test]
fn measurement_is_cached_by_content() {
    measure::set_height_callback(Some(fake_height));
    let mut h = Harness::new();
    let col = h.style(Style {
        flex_direction: FlexDirection::Column,
        align_items: Some(AlignItems::START),
        size: Size { width: length(50.0), height: auto() },
        ..Default::default()
    });
    let wraps = h.regs.measures.register(MeasureDesc { max_width: 100.0, max_height: 20.0, min_content_width: 10.0, wraps: 1 });
    let label = h.regs.measures.register(MeasureDesc { max_width: 30.0, max_height: 20.0, min_content_width: 30.0, wraps: 0 });

    // Three nodes share the wrapping content, one shows the non-wrapping label.
    let tree = N(0, col, 0, vec![N(1, 0, wraps, vec![]), N(2, 0, wraps, vec![]), N(3, 0, wraps, vec![]), N(4, 0, label, vec![])]);
    let (_, out) = h.frame(&tree, 200.0, 500.0);

    assert_eq!((out[1].width, out[1].height), (50.0, 40.0), "wrapped to two lines at 50 wide");
    assert_eq!((out[4].width, out[4].height), (30.0, 20.0));
    let after_first = CALLS.load(Ordering::SeqCst);
    assert!(after_first >= 1);
    assert!(after_first <= 2, "shared content measured once per distinct width, got {after_first} calls");

    // New nodes with the same content, e.g. rows scrolling into view, reuse the cached heights.
    let tree = N(0, col, 0, vec![N(5, 0, wraps, vec![]), N(6, 0, wraps, vec![]), N(4, 0, label, vec![])]);
    h.frame(&tree, 200.0, 500.0);
    assert_eq!(CALLS.load(Ordering::SeqCst), after_first);

    measure::set_height_callback(None);
}

#[test]
fn measure_handles_are_reused_after_release() {
    let mut m = MeasureRegistry::default();
    let a = m.register(MeasureDesc::default());
    let b = m.register(MeasureDesc::default());
    assert_ne!(a, 0);
    assert!(m.release(a));
    assert!(!m.release(a));
    assert!(!m.contains(a));
    assert_eq!(m.register(MeasureDesc::default()), a);
    assert!(m.contains(b));
    assert!(!m.release(0));
}

#[test]
fn default_style_handle_is_zero() {
    let reg = StyleRegistry::default();
    assert_eq!(reg.get(0), Some(&Style::default()));
}
