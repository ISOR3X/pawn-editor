//! Measurement caching. C# registers each distinct piece of content (text, font, wrapping) once and gets a
//! handle; leaves refer to that handle. Sizes are measured by a C# callback and cached per handle, so every node
//! showing the same content shares the answers, including nodes created later (e.g. rows scrolling into view).

use std::sync::atomic::{AtomicUsize, Ordering};
use taffy::prelude::*;

/// How the available width constrains a measurement. The width passed alongside is only meaningful for
/// `Exact` and `FitContent`.
#[repr(u32)]
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum MeasureMode {
    /// The width is fixed.
    Exact = 0,
    /// The width is the most that is available.
    FitContent = 1,
    /// As narrow as the content can be.
    MinContent = 2,
    /// As wide as the content wants to be.
    MaxContent = 3,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct MeasureSize {
    pub width: f32,
    pub height: f32,
}

/// Measures the content behind `handle` and writes its size to `out`.
pub type MeasureCallback = extern "C" fn(handle: u32, mode: MeasureMode, width: f32, out: *mut MeasureSize);

static CALLBACK: AtomicUsize = AtomicUsize::new(0);

pub fn set_callback(callback: Option<MeasureCallback>) {
    CALLBACK.store(callback.map_or(0, |f| f as usize), Ordering::Release);
}

fn callback() -> Option<MeasureCallback> {
    match CALLBACK.load(Ordering::Acquire) {
        0 => None,
        // SAFETY: only ever stored from a `MeasureCallback` in `set_callback`.
        p => Some(unsafe { std::mem::transmute::<usize, MeasureCallback>(p) }),
    }
}

/// How many answers each piece of content remembers.
/// Taffy requires answers for several measure modes in a layout pass (min-content, max-content, etc.) in order to find the ideal layout.
const ANSWERS_PER_CONTENT: usize = 8;

#[derive(Default)]
struct Answers {
    /// Cache entries with (mode, width) as key, returning size. Replaces oldest entry first once full.
    entries: Vec<(MeasureMode, f32, Size<f32>)>,
    next: usize,
}

#[derive(Default)]
pub struct MeasureRegistry {
    /// Index = handle. Handle 0 is never handed out and means "not measured".
    content: Vec<Option<Answers>>,
    free: Vec<u32>,
    pub callback_calls: u64,
}

impl MeasureRegistry {
    pub fn register(&mut self) -> u32 {
        // First handle is always None for "not measured" nodes.
        if self.content.is_empty() {
            self.content.push(None);
        }
        // Prioritize previously released handles over new ones.
        if let Some(handle) = self.free.pop() {
            self.content[handle as usize] = Some(Answers::default());
            return handle;
        }
        // Register a new handle.
        self.content.push(Some(Answers::default()));
        (self.content.len() - 1) as u32
    }

    pub fn release(&mut self, handle: u32) -> bool {
        // Set the Answer to None for a handle, and add the handle to the free list of handles.
        match self.content.get_mut(handle as usize) {
            Some(slot @ Some(_)) if handle != 0 => {
                *slot = None;
                self.free.push(handle);
                true
            }
            _ => false,
        }
    }

    pub fn contains(&self, handle: u32) -> bool {
        handle == 0 || matches!(self.content.get(handle as usize), Some(Some(_)))
    }

    /// Answers Taffy's measure request for a leaf, from the cache or by asking C#.
    pub fn measure(&mut self, handle: u32, known: Size<Option<f32>>, available: Size<AvailableSpace>) -> Size<f32> {
        // Set measure mode and available width for measuring.
        let (mode, width) = match (known.width, available.width) {
            (Some(w), _) => (MeasureMode::Exact, w),
            (None, AvailableSpace::Definite(w)) => (MeasureMode::FitContent, w),
            (None, AvailableSpace::MinContent) => (MeasureMode::MinContent, f32::INFINITY),
            (None, AvailableSpace::MaxContent) => (MeasureMode::MaxContent, f32::INFINITY),
        };

        // Check if the cache already contains answers for the handle. If so, return it.
        let Some(Some(answers)) = self.content.get_mut(handle as usize) else { return Size::ZERO };
        if let Some(&(_, _, size)) = answers.entries.iter().find(|(m, b, _)| *m == mode && *b == width) {
            return size;
        }

        // If the cache misses, try to get the callback.
        // The callback calls into C# to return actual sizing based on the context (context is stored on the C# side.)
        // Once a new size is returned, update the answers cache.
        let Some(callback) = callback() else { return Size::ZERO };
        let mut out = MeasureSize::default();
        callback(handle, mode, width, &mut out);
        self.callback_calls += 1;
        let size = Size { width: out.width, height: out.height };

        if answers.entries.len() < ANSWERS_PER_CONTENT {
            answers.entries.push((mode, width, size));
        } else {
            answers.entries[answers.next] = (mode, width, size);
            answers.next = (answers.next + 1) % ANSWERS_PER_CONTENT;
        }
        size
    }
}
