//! Native core of the Void UI library: reconciliation, layout (Taffy) and measurement caching.
//!
//! The C API is deliberately small. C# registers styles and measurable content once, then hands over the
//! whole UI once per frame with [`void_frame`]. Every function returns a status code (see [`status`]);
//! details of the last failure are available through [`void_last_error`]. Panics never cross the boundary.

pub mod measure;
pub mod style;
pub mod tree;

use std::cell::RefCell;
use std::panic::{AssertUnwindSafe, catch_unwind};
use std::sync::{Mutex, MutexGuard};

use measure::{HeightCallback, MeasureDesc, MeasureRegistry};
use style::{StyleDesc, StyleRegistry, VDimension, VGridPlacement, VTrack};
use tree::{FrameResult, NodeOut, NodeRecord, VoidTree};

/// Bumped whenever a shared struct or function signature changes.
pub const ABI_VERSION: u32 = 1;

pub mod status {
    pub const OK: i32 = 0;
    pub const PANIC: i32 = 1;
    pub const NULL_POINTER: i32 = 2;
    pub const INVALID: i32 = 3;
}

#[derive(Default)]
pub struct Registries {
    pub styles: StyleRegistry,
    pub measures: MeasureRegistry,
}

// SAFETY: Taffy styles are not `Send` only because a `calc()` length can carry a raw pointer. Styles here are
// built exclusively by `style::build`, which never creates calc values, so moving them between threads is fine.
unsafe impl Send for Registries {}

static REGISTRIES: Mutex<Option<Registries>> = Mutex::new(None);

/// Locks the process-wide registries. The height callback runs while this is held, so it must never call
/// back into this library.
fn registries() -> MutexGuard<'static, Option<Registries>> {
    // A panic while holding the lock already reported itself as a failed call; the data is still consistent
    // enough to keep using, so recover from poisoning instead of failing every later call.
    let mut guard = REGISTRIES.lock().unwrap_or_else(|e| e.into_inner());
    guard.get_or_insert_with(Registries::default);
    guard
}

thread_local! {
    static LAST_ERROR: RefCell<String> = const { RefCell::new(String::new()) };
}

fn guarded(f: impl FnOnce() -> Result<(), (i32, String)>) -> i32 {
    match catch_unwind(AssertUnwindSafe(f)) {
        Ok(Ok(())) => status::OK,
        Ok(Err((code, message))) => {
            LAST_ERROR.with(|e| *e.borrow_mut() = message);
            code
        }
        Err(payload) => {
            let message = payload
                .downcast_ref::<&str>()
                .map(|s| s.to_string())
                .or_else(|| payload.downcast_ref::<String>().cloned())
                .unwrap_or_else(|| "unknown panic".into());
            LAST_ERROR.with(|e| *e.borrow_mut() = format!("panic: {message}"));
            status::PANIC
        }
    }
}

fn invalid(message: String) -> (i32, String) {
    (status::INVALID, message)
}

fn null(what: &str) -> (i32, String) {
    (status::NULL_POINTER, format!("{what} is null"))
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct AbiSizes {
    pub node_record: u32,
    pub node_out: u32,
    pub frame_result: u32,
    pub style_desc: u32,
    pub measure_desc: u32,
    pub dimension: u32,
    pub track: u32,
    pub grid_placement: u32,
}

#[unsafe(no_mangle)]
pub extern "C" fn void_abi_version() -> u32 {
    ABI_VERSION
}

/// Sizes of every shared struct, so C# can verify its declarations match at startup.
///
/// # Safety
/// `out` must be valid for writes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn void_abi_sizes(out: *mut AbiSizes) -> i32 {
    guarded(|| {
        let out = unsafe { out.as_mut() }.ok_or_else(|| null("out"))?;
        *out = AbiSizes {
            node_record: size_of::<NodeRecord>() as u32,
            node_out: size_of::<NodeOut>() as u32,
            frame_result: size_of::<FrameResult>() as u32,
            style_desc: size_of::<StyleDesc>() as u32,
            measure_desc: size_of::<MeasureDesc>() as u32,
            dimension: size_of::<VDimension>() as u32,
            track: size_of::<VTrack>() as u32,
            grid_placement: size_of::<VGridPlacement>() as u32,
        };
        Ok(())
    })
}

/// Copies the last error message of this thread as UTF-8 into `buf` and returns its full length in bytes.
///
/// # Safety
/// `buf` must be valid for `cap` bytes of writes, or null with `cap` 0.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn void_last_error(buf: *mut u8, cap: u32) -> u32 {
    LAST_ERROR.with(|e| {
        let e = e.borrow();
        let bytes = e.as_bytes();
        if !buf.is_null() {
            let n = bytes.len().min(cap as usize);
            unsafe { std::ptr::copy_nonoverlapping(bytes.as_ptr(), buf, n) };
        }
        bytes.len() as u32
    })
}

/// Sets the function that measures the height of wrapping content at a given width. Pass null to clear it.
#[unsafe(no_mangle)]
pub extern "C" fn void_set_height_callback(callback: Option<HeightCallback>) -> i32 {
    guarded(|| {
        measure::set_height_callback(callback);
        Ok(())
    })
}

/// # Safety
/// `desc` must be valid for reads, including its grid track arrays; `out_handle` must be valid for writes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn void_style_register(desc: *const StyleDesc, out_handle: *mut u32) -> i32 {
    guarded(|| {
        let desc = unsafe { desc.as_ref() }.ok_or_else(|| null("desc"))?;
        let out_handle = unsafe { out_handle.as_mut() }.ok_or_else(|| null("out_handle"))?;
        let style = unsafe { style::build(desc) }.map_err(invalid)?;
        *out_handle = registries().as_mut().unwrap().styles.register(style);
        Ok(())
    })
}

/// # Safety
/// `desc` must be valid for reads and `out_handle` for writes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn void_measure_register(desc: *const MeasureDesc, out_handle: *mut u32) -> i32 {
    guarded(|| {
        let desc = unsafe { desc.as_ref() }.ok_or_else(|| null("desc"))?;
        let out_handle = unsafe { out_handle.as_mut() }.ok_or_else(|| null("out_handle"))?;
        *out_handle = registries().as_mut().unwrap().measures.register(*desc);
        Ok(())
    })
}

/// Frees a measure handle so it can be reused. Nodes must no longer refer to it.
#[unsafe(no_mangle)]
pub extern "C" fn void_measure_release(handle: u32) -> i32 {
    guarded(|| {
        if registries().as_mut().unwrap().measures.release(handle) {
            Ok(())
        } else {
            Err(invalid(format!("measure handle {handle} is not registered")))
        }
    })
}

/// Number of times the height callback has been invoked, for profiling.
#[unsafe(no_mangle)]
pub extern "C" fn void_height_callback_count() -> u64 {
    registries().as_ref().unwrap().measures.callback_calls
}

#[unsafe(no_mangle)]
pub extern "C" fn void_tree_new() -> *mut VoidTree {
    Box::into_raw(Box::default())
}

/// # Safety
/// `tree` must come from [`void_tree_new`] and not be used afterwards. Null is ignored.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn void_tree_free(tree: *mut VoidTree) {
    if !tree.is_null() {
        drop(unsafe { Box::from_raw(tree) });
    }
}

/// Reconciles, lays out and positions one frame.
///
/// `records` is the frame's pre-order description (`count` entries); `out` receives one entry per record.
/// An available size of +infinity means "unconstrained" (max-content).
///
/// # Safety
/// `tree` must come from [`void_tree_new`]; `records` and `out` must be valid for `count` entries; `result`
/// must be valid for writes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn void_frame(
    tree: *mut VoidTree,
    records: *const NodeRecord,
    count: u32,
    available_width: f32,
    available_height: f32,
    origin_x: f32,
    origin_y: f32,
    out: *mut NodeOut,
    result: *mut FrameResult,
) -> i32 {
    guarded(|| {
        let tree = unsafe { tree.as_mut() }.ok_or_else(|| null("tree"))?;
        if records.is_null() {
            return Err(null("records"));
        }
        if out.is_null() {
            return Err(null("out"));
        }
        let result = unsafe { result.as_mut() }.ok_or_else(|| null("result"))?;
        let records = unsafe { std::slice::from_raw_parts(records, count as usize) };
        let out = unsafe { std::slice::from_raw_parts_mut(out, count as usize) };

        let mut guard = registries();
        let regs = guard.as_mut().unwrap();
        *result = tree
            .frame(
                records,
                available_width,
                available_height,
                (origin_x, origin_y),
                &regs.styles,
                &mut regs.measures,
                out,
            )
            .map_err(invalid)?;
        Ok(())
    })
}

/// Moves up to `cap` keys of removed nodes into `buf` and returns how many were written. Keys not taken
/// stay queued for the next call.
///
/// # Safety
/// `tree` must come from [`void_tree_new`]; `buf` must be valid for `cap` writes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn void_take_removed(tree: *mut VoidTree, buf: *mut u64, cap: u32) -> u32 {
    let Some(tree) = (unsafe { tree.as_mut() }) else { return 0 };
    if buf.is_null() {
        return 0;
    }
    let buf = unsafe { std::slice::from_raw_parts_mut(buf, cap as usize) };
    tree.take_removed(buf) as u32
}

#[cfg(test)]
mod tests;
