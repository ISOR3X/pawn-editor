using System.Reflection;
using UnityEngine;
using Verse;

namespace Void;

/// <summary>
///     Resolves icon names from XML (<c>icon="plus"</c>) to <see cref="Texture2D" /> instances.
///     Supports two resolution strategies:
///     <list type="number">
///         <item>Reflection: <c>"RimWorld.TexButton.Delete"</c> - split on last dot, look up static field.</item>
///     </list>
/// </summary>
public static class IconRegistry
{
    private static readonly Dictionary<string, Texture2D?> ReflectionCache = new();

    /// <summary>
    ///     Resolves an icon name to a <see cref="Texture2D" />. Returns null if not found.
    ///     <para>Short names (e.g. <c>"plus"</c>) are looked up in the registry.</para>
    ///     <para>
    ///         Qualified names (e.g. <c>"RimWorld.TexButton.Delete"</c>) are resolved via reflection
    ///         and cached.
    ///     </para>
    /// </summary>
    public static Texture2D? Resolve(string name)
    {
        if (ReflectionCache.TryGetValue(name, out var cached))
            return cached;

        var result = ResolveViaReflection(name);
        ReflectionCache[name] = result;
        if (result == null) Log.Warning($"[{VoidMod.ModName}] Could not resolve icon '{name}'.");
        return result;
    }

    private static Texture2D? ResolveViaReflection(string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

        var lastDot = name.LastIndexOf('.');
        if (lastDot < 0) return null;

        var typeName = name[..lastDot];
        var fieldName = name[(lastDot + 1)..];

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName)
                       ?? assembly.GetType("Verse." + typeName)
                       ?? assembly.GetType("RimWorld." + typeName);
            if (type == null) continue;
            if (type.GetField(fieldName, flags)?.GetValue(null) is Texture2D t) return t;
            if (type.GetProperty(fieldName, flags)?.GetValue(null) is Texture2D pt) return pt;
        }

        return null;
    }
}