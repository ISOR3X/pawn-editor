//! Generates the C# declarations for this crate's C API into the Void project, the same way the Taffy fork's
//! C# bindings are generated. Hand-written additions (equality, helpers, error handling) live in partial
//! types next to the generated file.

fn main() {
    let sources = ["src/lib.rs", "src/style.rs", "src/tree.rs", "src/measure.rs"];
    for source in sources {
        println!("cargo:rerun-if-changed={source}");
    }

    let mut builder = csbindgen::Builder::default();
    for source in sources {
        builder = builder.input_extern_file(source);
    }

    builder
        // The enums only appear as raw u32 fields in the C API, so they would not be picked up otherwise.
        .always_included_types([
            "TaffyUnit",
            "TaffyDisplay",
            "TaffyFlexDirection",
            "TaffyFlexWrap",
            "TaffyAlignItems",
            "TaffyAlignContent",
            "TaffyOverflow",
            "TaffyGridAutoFlow",
        ])
        .csharp_dll_name("void_native")
        .csharp_namespace("Void.Native")
        .csharp_class_name("NativeMethods")
        // csbindgen applies this to the structs too, and the public Style API exposes the value types.
        .csharp_class_accessibility("public")
        .csharp_use_nint_types(false)
        // net472 on Mono: delegates rather than C# 9 function pointers.
        .csharp_use_function_pointer(false)
        // Only the constants that are part of the C API.
        .csharp_generate_const_filter(|name| {
            name == "ABI_VERSION" || name.starts_with("STATUS_") || name.starts_with("STYLE_SET_") || name.starts_with("NODE_")
        })
        .generate_csharp_file("../../src/Void/Native/NativeMethods.g.cs")
        .unwrap();
}
