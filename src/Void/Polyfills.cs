// Polyfills for C# 9+ language features when targeting net472.
// IsExternalInit enables init-only setters and record types (C# 9+).
// Required for net472 targets where this type isn't part of the runtime.

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
