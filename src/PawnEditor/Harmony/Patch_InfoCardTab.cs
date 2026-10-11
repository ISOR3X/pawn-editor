using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using Void.Native;

namespace PawnEditor;

/// <summary>
/// Adds an "Editor" tab to <see cref="Dialog_InfoCard"/>. Currently pawns only.
/// </summary>
/// <remarks>
/// The vanilla tab enum is closed, so the tab is represented by a sentinel value. Vanilla tabs compare against
/// the card's <c>tab</c> field, so with the sentinel set they all render as unselected, and selecting any of them
/// clears ours without extra bookkeeping.
/// </remarks>
[UsedImplicitly]
[HarmonyPatch] // Required for PatchAll to discover a class whose attributes are all on its methods
public static class Patch_InfoCardTab
{
    private const Dialog_InfoCard.InfoCardTab EditorTab = (Dialog_InfoCard.InfoCardTab)255;

    private static readonly AccessTools.FieldRef<Dialog_InfoCard, Dialog_InfoCard.InfoCardTab> TabField =
        AccessTools.FieldRefAccess<Dialog_InfoCard, Dialog_InfoCard.InfoCardTab>("tab");

    private static readonly AccessTools.FieldRef<Dialog_InfoCard, Thing> ThingField =
        AccessTools.FieldRefAccess<Dialog_InfoCard, Thing>("thing");

    /// <summary>
    /// One native tree per open card, created lazily when the tab is first drawn.
    /// </summary>
    private static readonly ConditionalWeakTable<Dialog_InfoCard, UITree> Trees = new();

    private static bool AppliesTo(Dialog_InfoCard card) => ThingField(card) is Pawn;

    // Called from the transpiled DoWindowContents right before the `tabs.Count > 1` check.
    public static void AddTab(List<TabRecord> tabs, Dialog_InfoCard card)
    {
        if (!AppliesTo(card)) return;

        tabs.Add(new TabRecord("Editor", () => TabField(card) = EditorTab, TabField(card) == EditorTab));
    }

    [HarmonyPatch(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DoWindowContents))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> DoWindowContents_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var count = AccessTools.PropertyGetter(typeof(List<TabRecord>), nameof(List<TabRecord>.Count));
        var addTab = AccessTools.Method(typeof(Patch_InfoCardTab), nameof(AddTab));

        var codes = instructions.ToList();
        for (var i = 1; i < codes.Count; i++)
        {
            if (!codes[i].Calls(count) || !codes[i - 1].IsLdloc()) continue;

            // Jumps that targeted the original load must now land on our call instead.
            var original = codes[i - 1];
            var load = new CodeInstruction(original.opcode, original.operand) { labels = original.labels };
            original.labels = [];

            codes.InsertRange(i - 1,
            [
                load,
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, addTab)
            ]);
            return codes;
        }

        Log.Error("[PawnEditor] Could not find the tab count check in Dialog_InfoCard.DoWindowContents; the Editor tab will not be added.");
        return codes;
    }

    [HarmonyPatch(typeof(Dialog_InfoCard), "FillCard")]
    [HarmonyPrefix]
    private static bool FillCard_Prefix(Dialog_InfoCard __instance, Rect cardRect)
    {
        if (TabField(__instance) != EditorTab) return true;

        var tree = Trees.GetValue(__instance, _ => new UITree());
        tree.Build(b => b.Text("Editor tab"),
            style: new Style { width = Dimension.Percent(1f), height = Dimension.Percent(1f) });
        tree.Draw(cardRect);

        return false;
    }

    // The tree is native, so it must be disposed when the card goes away. Dialog_InfoCard doesn't override
    // PostClose, and WindowStack.TryRemove (used by hyperlink navigation too) always calls it.
    [HarmonyPatch(typeof(Window), nameof(Window.PostClose))]
    [HarmonyPostfix]
    private static void PostClose_Postfix(Window __instance)
    {
        if (__instance is not Dialog_InfoCard card || !Trees.TryGetValue(card, out var tree)) return;

        tree.Dispose();
        Trees.Remove(card);
    }
}
