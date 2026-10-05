//! Measure interning: leaf content (text, font, wrap) is registered once and measured by handle.
//!
//! Everything that does not depend on the available width is computed by C# at registration and answered
//! here directly. Only wrapping text at a width not seen before calls back into C# for its height, and that
//! answer is cached on the entry, so every node showing the same content shares it.

use std::sync::atomic::{AtomicUsize, Ordering};
use taffy::prelude::*;

#[repr(C)]
#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct MeasureDesc {
    /// Unwrapped size of the content.
    pub max_width: f32,
    pub max_height: f32,
    /// Width of the widest unbreakable part (the longest word).
    pub min_content_width: f32,
    /// 0 when the content never wraps, so its size does not depend on the width.
    pub wraps: u32,
}

/// Returns the height of the content behind `handle` when laid out at `width`.
pub type HeightCallback = extern "C" fn(handle: u32, width: f32) -> f32;

static HEIGHT_CALLBACK: AtomicUsize = AtomicUsize::new(0);

pub fn set_height_callback(cb: Option<HeightCallback>) {
    HEIGHT_CALLBACK.store(cb.map_or(0, |f| f as usize), Ordering::Release);
}

fn height_callback() -> Option<HeightCallback> {
    match HEIGHT_CALLBACK.load(Ordering::Acquire) {
        0 => None,
        // SAFETY: only ever stored from a `HeightCallback` in `set_height_callback`.
        p => Some(unsafe { std::mem::transmute::<usize, HeightCallback>(p) }),
    }
}

const HEIGHT_CACHE: usize = 8;

struct Entry {
    desc: MeasureDesc,
    /// (width bits, height), replaced round-robin once full. Resizing a window produces a new width every
    /// frame, so this stays small on purpose.
    heights: [(u32, f32); HEIGHT_CACHE],
    len: usize,
    next: usize,
}

impl Entry {
    fn new(desc: MeasureDesc) -> Self {
        Self { desc, heights: [(0, 0.0); HEIGHT_CACHE], len: 0, next: 0 }
    }
}

#[derive(Default)]
pub struct MeasureRegistry {
    /// Index = handle. Handle 0 is never handed out and means "not measured".
    entries: Vec<Option<Entry>>,
    free: Vec<u32>,
    pub callback_calls: u64,
}

impl MeasureRegistry {
    pub fn register(&mut self, desc: MeasureDesc) -> u32 {
        if self.entries.is_empty() {
            self.entries.push(None);
        }
        if let Some(h) = self.free.pop() {
            self.entries[h as usize] = Some(Entry::new(desc));
            return h;
        }
        self.entries.push(Some(Entry::new(desc)));
        (self.entries.len() - 1) as u32
    }

    pub fn release(&mut self, handle: u32) -> bool {
        match self.entries.get_mut(handle as usize) {
            Some(slot @ Some(_)) if handle != 0 => {
                *slot = None;
                self.free.push(handle);
                true
            }
            _ => false,
        }
    }

    pub fn contains(&self, handle: u32) -> bool {
        handle == 0 || matches!(self.entries.get(handle as usize), Some(Some(_)))
    }

    fn height_at(&mut self, handle: u32, width: f32) -> f32 {
        let Some(Some(e)) = self.entries.get_mut(handle as usize) else { return 0.0 };
        let bits = width.to_bits();
        if let Some(&(_, h)) = e.heights[..e.len].iter().find(|(b, _)| *b == bits) {
            return h;
        }

        let h = match height_callback() {
            Some(cb) => {
                self.callback_calls += 1;
                cb(handle, width)
            }
            None => e.desc.max_height,
        };

        let Some(Some(e)) = self.entries.get_mut(handle as usize) else { return h };
        e.heights[e.next] = (bits, h);
        e.next = (e.next + 1) % HEIGHT_CACHE;
        e.len = (e.len + 1).min(HEIGHT_CACHE);
        h
    }

    /// Same rules as the C# `DefaultMeasure` this replaces. The measure mode is derived from Taffy's inputs the
    /// same way the previous C bindings did: a known width is exact, a definite available width is fit-content.
    pub fn measure(&mut self, handle: u32, known: Size<Option<f32>>, available: Size<AvailableSpace>) -> Size<f32> {
        let Some(Some(e)) = self.entries.get(handle as usize) else { return Size::ZERO };
        let d = e.desc;

        enum Mode {
            Exact(f32),
            FitContent(f32),
            MinContent,
            MaxContent,
        }

        let mode = match (known.width, available.width) {
            (Some(w), _) => Mode::Exact(w),
            (None, AvailableSpace::Definite(w)) => Mode::FitContent(w),
            (None, AvailableSpace::MinContent) => Mode::MinContent,
            (None, AvailableSpace::MaxContent) => Mode::MaxContent,
        };

        if d.wraps == 0 {
            let width = match mode {
                Mode::Exact(w) | Mode::FitContent(w) => w.min(d.max_width),
                Mode::MinContent | Mode::MaxContent => d.max_width,
            };
            return Size { width, height: d.max_height };
        }

        match mode {
            Mode::Exact(w) => Size { width: w, height: self.height_at(handle, w) },
            Mode::FitContent(w) => {
                let w = w.min(d.max_width);
                Size { width: w, height: self.height_at(handle, w) }
            }
            Mode::MinContent => {
                let w = d.min_content_width;
                Size { width: w, height: self.height_at(handle, w) }
            }
            Mode::MaxContent => Size { width: d.max_width, height: d.max_height },
        }
    }
}
