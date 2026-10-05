//! Style interning: C# registers each distinct layout style once and refers to it by handle afterwards.
//!
//! The value types mirror the managed `Taffy.dll` structs (`TaffyDimension`, `TaffyTrackSizingFunction`,
//! `TaffyGridPlacement`, `TaffyAxes`, `TaffyEdges`) field for field, so C# can copy them straight into a
//! [`StyleDesc`] without converting. Enum values follow the same managed enums.

use taffy::prelude::*;
use taffy::style::{MaxTrackSizingFunction, MinTrackSizingFunction, Overflow};

#[repr(C)]
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct VDimension {
    pub value: f32,
    pub unit: u32,
}

pub mod unit {
    pub const NONE: u32 = 0;
    pub const LENGTH: u32 = 1;
    pub const PERCENT: u32 = 2;
    pub const MIN_CONTENT: u32 = 3;
    pub const MAX_CONTENT: u32 = 4;
    pub const FIT_CONTENT_PX: u32 = 5;
    pub const FIT_CONTENT_PERCENT: u32 = 6;
    pub const AUTO: u32 = 7;
    pub const FR: u32 = 8;
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct VTrack {
    pub min: VDimension,
    pub max: VDimension,
}

/// `start`/`end` are grid lines (0 = auto). `span` is only used when `start` is 0.
#[repr(C)]
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct VGridPlacement {
    pub start: i16,
    pub end: i16,
    pub span: u16,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct VAxes {
    pub width: VDimension,
    pub height: VDimension,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct VEdges {
    pub top: VDimension,
    pub right: VDimension,
    pub bottom: VDimension,
    pub left: VDimension,
}

/// Bits of [`StyleDesc::set_mask`]. A field whose bit is clear keeps Taffy's default.
pub mod set {
    pub const DISPLAY: u64 = 1 << 0;
    pub const FLEX_DIRECTION: u64 = 1 << 1;
    pub const FLEX_WRAP: u64 = 1 << 2;
    pub const ALIGN_ITEMS: u64 = 1 << 3;
    pub const ALIGN_SELF: u64 = 1 << 4;
    pub const ALIGN_CONTENT: u64 = 1 << 5;
    pub const JUSTIFY_CONTENT: u64 = 1 << 6;
    pub const JUSTIFY_ITEMS: u64 = 1 << 7;
    pub const JUSTIFY_SELF: u64 = 1 << 8;
    pub const OVERFLOW_X: u64 = 1 << 9;
    pub const OVERFLOW_Y: u64 = 1 << 10;
    pub const GRID_AUTO_FLOW: u64 = 1 << 11;
    pub const FLEX_GROW: u64 = 1 << 12;
    pub const FLEX_SHRINK: u64 = 1 << 13;
    pub const SCROLLBAR_WIDTH: u64 = 1 << 14;
    pub const FLEX_BASIS: u64 = 1 << 15;
    pub const WIDTH: u64 = 1 << 16;
    pub const HEIGHT: u64 = 1 << 17;
    pub const MIN_WIDTH: u64 = 1 << 18;
    pub const MIN_HEIGHT: u64 = 1 << 19;
    pub const MAX_WIDTH: u64 = 1 << 20;
    pub const MAX_HEIGHT: u64 = 1 << 21;
    pub const GAP: u64 = 1 << 22;
    pub const PADDING: u64 = 1 << 23;
    pub const MARGIN: u64 = 1 << 24;
    pub const GRID_COLUMN: u64 = 1 << 25;
    pub const GRID_ROW: u64 = 1 << 26;
    pub const GRID_TEMPLATE_COLUMNS: u64 = 1 << 27;
    pub const GRID_TEMPLATE_ROWS: u64 = 1 << 28;
    pub const GRID_AUTO_COLUMNS: u64 = 1 << 29;
    pub const GRID_AUTO_ROWS: u64 = 1 << 30;
}

/// Flat description of a layout style. Pointers are only read during `void_style_register`.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct StyleDesc {
    pub set_mask: u64,

    pub grid_template_columns: *const VTrack,
    pub grid_template_rows: *const VTrack,
    pub grid_auto_columns: *const VTrack,
    pub grid_auto_rows: *const VTrack,
    pub grid_template_columns_len: u32,
    pub grid_template_rows_len: u32,
    pub grid_auto_columns_len: u32,
    pub grid_auto_rows_len: u32,

    pub display: u32,
    pub flex_direction: u32,
    pub flex_wrap: u32,
    pub align_items: u32,
    pub align_self: u32,
    pub align_content: u32,
    pub justify_content: u32,
    pub justify_items: u32,
    pub justify_self: u32,
    pub overflow_x: u32,
    pub overflow_y: u32,
    pub grid_auto_flow: u32,

    pub flex_grow: f32,
    pub flex_shrink: f32,
    pub scrollbar_width: f32,

    pub flex_basis: VDimension,
    pub width: VDimension,
    pub height: VDimension,
    pub min_width: VDimension,
    pub min_height: VDimension,
    pub max_width: VDimension,
    pub max_height: VDimension,

    pub gap: VAxes,
    pub padding: VEdges,
    pub margin: VEdges,

    pub grid_column: VGridPlacement,
    pub grid_row: VGridPlacement,
}

type R<T> = Result<T, String>;

fn bad(field: &str, d: VDimension) -> String {
    format!("{field}: unit {} is not valid here", d.unit)
}

fn dimension(field: &str, d: VDimension) -> R<Dimension> {
    match d.unit {
        unit::LENGTH => Ok(Dimension::length(d.value)),
        unit::PERCENT => Ok(Dimension::percent(d.value)),
        unit::AUTO => Ok(Dimension::auto()),
        _ => Err(bad(field, d)),
    }
}

fn length_percentage(field: &str, d: VDimension) -> R<LengthPercentage> {
    match d.unit {
        unit::LENGTH => Ok(LengthPercentage::length(d.value)),
        unit::PERCENT => Ok(LengthPercentage::percent(d.value)),
        _ => Err(bad(field, d)),
    }
}

fn length_percentage_auto(field: &str, d: VDimension) -> R<LengthPercentageAuto> {
    match d.unit {
        unit::LENGTH => Ok(LengthPercentageAuto::length(d.value)),
        unit::PERCENT => Ok(LengthPercentageAuto::percent(d.value)),
        unit::AUTO => Ok(LengthPercentageAuto::auto()),
        _ => Err(bad(field, d)),
    }
}

fn min_track(field: &str, d: VDimension) -> R<MinTrackSizingFunction> {
    match d.unit {
        unit::LENGTH => Ok(MinTrackSizingFunction::length(d.value)),
        unit::PERCENT => Ok(MinTrackSizingFunction::percent(d.value)),
        unit::AUTO => Ok(MinTrackSizingFunction::auto()),
        unit::MIN_CONTENT => Ok(MinTrackSizingFunction::min_content()),
        unit::MAX_CONTENT => Ok(MinTrackSizingFunction::max_content()),
        _ => Err(bad(field, d)),
    }
}

fn max_track(field: &str, d: VDimension) -> R<MaxTrackSizingFunction> {
    match d.unit {
        unit::LENGTH => Ok(MaxTrackSizingFunction::length(d.value)),
        unit::PERCENT => Ok(MaxTrackSizingFunction::percent(d.value)),
        unit::AUTO => Ok(MaxTrackSizingFunction::auto()),
        unit::MIN_CONTENT => Ok(MaxTrackSizingFunction::min_content()),
        unit::MAX_CONTENT => Ok(MaxTrackSizingFunction::max_content()),
        unit::FIT_CONTENT_PX => Ok(MaxTrackSizingFunction::fit_content_px(d.value)),
        unit::FIT_CONTENT_PERCENT => Ok(MaxTrackSizingFunction::fit_content_percent(d.value)),
        unit::FR => Ok(MaxTrackSizingFunction::fr(d.value)),
        _ => Err(bad(field, d)),
    }
}

fn track(field: &str, t: VTrack) -> R<TrackSizingFunction> {
    Ok(TrackSizingFunction { min: min_track(field, t.min)?, max: max_track(field, t.max)? })
}

/// # Safety
/// `ptr` must point to `len` readable tracks (or `len` must be 0).
unsafe fn tracks<'a>(field: &str, ptr: *const VTrack, len: u32) -> R<&'a [VTrack]> {
    if len == 0 {
        return Ok(&[]);
    }
    if ptr.is_null() {
        return Err(format!("{field}: null pointer with length {len}"));
    }
    Ok(unsafe { std::slice::from_raw_parts(ptr, len as usize) })
}

/// The C# enums have a `Normal` value at 0 that Taffy expresses as `None`.
fn align_items(field: &str, v: u32) -> R<Option<AlignItems>> {
    Ok(match v {
        0 => None,
        1 => Some(AlignItems::START),
        2 => Some(AlignItems::END),
        3 => Some(AlignItems::FLEX_START),
        4 => Some(AlignItems::FLEX_END),
        5 => Some(AlignItems::CENTER),
        6 => Some(AlignItems::BASELINE),
        7 => Some(AlignItems::STRETCH),
        _ => return Err(format!("{field}: unknown value {v}")),
    })
}

fn align_content(field: &str, v: u32) -> R<Option<AlignContent>> {
    Ok(match v {
        0 => None,
        1 => Some(AlignContent::START),
        2 => Some(AlignContent::END),
        3 => Some(AlignContent::FLEX_START),
        4 => Some(AlignContent::FLEX_END),
        5 => Some(AlignContent::CENTER),
        6 => Some(AlignContent::STRETCH),
        7 => Some(AlignContent::SPACE_BETWEEN),
        8 => Some(AlignContent::SPACE_EVENLY),
        9 => Some(AlignContent::SPACE_AROUND),
        _ => return Err(format!("{field}: unknown value {v}")),
    })
}

fn overflow(field: &str, v: u32) -> R<Overflow> {
    Ok(match v {
        0 => Overflow::Visible,
        1 => Overflow::Clip,
        2 => Overflow::Hidden,
        3 => Overflow::Scroll,
        _ => return Err(format!("{field}: unknown value {v}")),
    })
}

fn placement(p: VGridPlacement) -> Line<GridPlacement> {
    let start = match (p.start, p.span) {
        (0, 0) => GridPlacement::Auto,
        (0, s) => GridPlacement::Span(s),
        (l, _) => GridPlacement::Line(l.into()),
    };
    let end = if p.end != 0 { GridPlacement::Line(p.end.into()) } else { GridPlacement::Auto };
    Line { start, end }
}

/// Converts a description into a Taffy style. Unset fields keep `Style::default()`, as do the individual
/// gap/padding/margin components whose unit is `None`.
///
/// # Safety
/// The grid track pointers in `d` must be valid for their lengths.
pub unsafe fn build(d: &StyleDesc) -> R<Style> {
    let has = |bit: u64| d.set_mask & bit != 0;
    let mut s = Style::default();

    if has(set::DISPLAY) {
        s.display = match d.display {
            0 => Display::Block,
            1 => Display::Flex,
            2 => Display::Grid,
            3 => Display::None,
            v => return Err(format!("display: unknown value {v}")),
        };
    }
    if has(set::FLEX_DIRECTION) {
        s.flex_direction = match d.flex_direction {
            0 => FlexDirection::Row,
            1 => FlexDirection::Column,
            2 => FlexDirection::RowReverse,
            3 => FlexDirection::ColumnReverse,
            v => return Err(format!("flexDirection: unknown value {v}")),
        };
    }
    if has(set::FLEX_WRAP) {
        s.flex_wrap = match d.flex_wrap {
            0 => FlexWrap::NoWrap,
            1 => FlexWrap::Wrap,
            2 => FlexWrap::WrapReverse,
            v => return Err(format!("flexWrap: unknown value {v}")),
        };
    }
    if has(set::ALIGN_ITEMS) {
        s.align_items = align_items("alignItems", d.align_items)?;
    }
    if has(set::ALIGN_SELF) {
        s.align_self = align_items("alignSelf", d.align_self)?;
    }
    if has(set::JUSTIFY_ITEMS) {
        s.justify_items = align_items("justifyItems", d.justify_items)?;
    }
    if has(set::JUSTIFY_SELF) {
        s.justify_self = align_items("justifySelf", d.justify_self)?;
    }
    if has(set::ALIGN_CONTENT) {
        s.align_content = align_content("alignContent", d.align_content)?;
    }
    if has(set::JUSTIFY_CONTENT) {
        s.justify_content = align_content("justifyContent", d.justify_content)?;
    }
    if has(set::OVERFLOW_X) {
        s.overflow.x = overflow("overflowX", d.overflow_x)?;
    }
    if has(set::OVERFLOW_Y) {
        s.overflow.y = overflow("overflowY", d.overflow_y)?;
    }
    if has(set::GRID_AUTO_FLOW) {
        s.grid_auto_flow = match d.grid_auto_flow {
            0 => GridAutoFlow::Row,
            1 => GridAutoFlow::Column,
            2 => GridAutoFlow::RowDense,
            3 => GridAutoFlow::ColumnDense,
            v => return Err(format!("gridAutoFlow: unknown value {v}")),
        };
    }
    if has(set::FLEX_GROW) {
        s.flex_grow = d.flex_grow;
    }
    if has(set::FLEX_SHRINK) {
        s.flex_shrink = d.flex_shrink;
    }
    if has(set::SCROLLBAR_WIDTH) {
        s.scrollbar_width = d.scrollbar_width;
    }
    if has(set::FLEX_BASIS) {
        s.flex_basis = dimension("flexBasis", d.flex_basis)?;
    }
    if has(set::WIDTH) {
        s.size.width = dimension("width", d.width)?;
    }
    if has(set::HEIGHT) {
        s.size.height = dimension("height", d.height)?;
    }
    if has(set::MIN_WIDTH) {
        s.min_size.width = length_percentage_auto("minWidth", d.min_width)?;
    }
    if has(set::MIN_HEIGHT) {
        s.min_size.height = length_percentage_auto("minHeight", d.min_height)?;
    }
    if has(set::MAX_WIDTH) {
        s.max_size.width = length_percentage_auto("maxWidth", d.max_width)?;
    }
    if has(set::MAX_HEIGHT) {
        s.max_size.height = length_percentage_auto("maxHeight", d.max_height)?;
    }
    if has(set::GAP) {
        // Width is the gap between columns, height the gap between rows.
        if d.gap.width.unit != unit::NONE {
            s.gap.width = length_percentage("gap.width", d.gap.width)?;
        }
        if d.gap.height.unit != unit::NONE {
            s.gap.height = length_percentage("gap.height", d.gap.height)?;
        }
    }
    if has(set::PADDING) {
        let p = d.padding;
        if p.top.unit != unit::NONE {
            s.padding.top = length_percentage("padding.top", p.top)?;
        }
        if p.right.unit != unit::NONE {
            s.padding.right = length_percentage("padding.right", p.right)?;
        }
        if p.bottom.unit != unit::NONE {
            s.padding.bottom = length_percentage("padding.bottom", p.bottom)?;
        }
        if p.left.unit != unit::NONE {
            s.padding.left = length_percentage("padding.left", p.left)?;
        }
    }
    if has(set::MARGIN) {
        let m = d.margin;
        if m.top.unit != unit::NONE {
            s.margin.top = length_percentage_auto("margin.top", m.top)?;
        }
        if m.right.unit != unit::NONE {
            s.margin.right = length_percentage_auto("margin.right", m.right)?;
        }
        if m.bottom.unit != unit::NONE {
            s.margin.bottom = length_percentage_auto("margin.bottom", m.bottom)?;
        }
        if m.left.unit != unit::NONE {
            s.margin.left = length_percentage_auto("margin.left", m.left)?;
        }
    }
    if has(set::GRID_COLUMN) {
        s.grid_column = placement(d.grid_column);
    }
    if has(set::GRID_ROW) {
        s.grid_row = placement(d.grid_row);
    }
    if has(set::GRID_TEMPLATE_COLUMNS) {
        let ts = unsafe { tracks("gridTemplateColumns", d.grid_template_columns, d.grid_template_columns_len)? };
        s.grid_template_columns =
            ts.iter().map(|t| track("gridTemplateColumns", *t).map(GridTemplateComponent::Single)).collect::<R<_>>()?;
    }
    if has(set::GRID_TEMPLATE_ROWS) {
        let ts = unsafe { tracks("gridTemplateRows", d.grid_template_rows, d.grid_template_rows_len)? };
        s.grid_template_rows =
            ts.iter().map(|t| track("gridTemplateRows", *t).map(GridTemplateComponent::Single)).collect::<R<_>>()?;
    }
    if has(set::GRID_AUTO_COLUMNS) {
        let ts = unsafe { tracks("gridAutoColumns", d.grid_auto_columns, d.grid_auto_columns_len)? };
        s.grid_auto_columns = ts.iter().map(|t| track("gridAutoColumns", *t)).collect::<R<_>>()?;
    }
    if has(set::GRID_AUTO_ROWS) {
        let ts = unsafe { tracks("gridAutoRows", d.grid_auto_rows, d.grid_auto_rows_len)? };
        s.grid_auto_rows = ts.iter().map(|t| track("gridAutoRows", *t)).collect::<R<_>>()?;
    }

    Ok(s)
}

/// Process-wide table of interned styles. Handle 0 is always `Style::default()`, used for nodes without a style.
pub struct StyleRegistry {
    styles: Vec<Style>,
}

impl Default for StyleRegistry {
    fn default() -> Self {
        Self { styles: vec![Style::default()] }
    }
}

impl StyleRegistry {
    pub fn register(&mut self, style: Style) -> u32 {
        self.styles.push(style);
        (self.styles.len() - 1) as u32
    }

    pub fn get(&self, handle: u32) -> Option<&Style> {
        self.styles.get(handle as usize)
    }
}
