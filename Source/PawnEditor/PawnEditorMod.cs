using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class PawnEditorMod : Mod
{
    public static Harmony Harm;
    public static PawnEditorSettings Settings;
    public static PawnEditorMod Instance;

    public PawnEditorMod(ModContentPack content) : base(content)
    {
        Harm = new("legodude17.pawneditor");
        Instance = this;

        Harm.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.PreOpen)),
            new(GetType(), nameof(Notify_ConfigurePawns)));
        Harm.Patch(AccessTools.Method(typeof(Page_SelectScenario), nameof(Page_SelectScenario.PreOpen)),
            new(typeof(StartingThingsManager), nameof(StartingThingsManager.RestoreScenario)));
        Harm.Patch(AccessTools.Method(typeof(Game), nameof(Game.InitNewGame)),
            postfix: new(typeof(StartingThingsManager), nameof(StartingThingsManager.RestoreScenario)));

        LongEventHandler.ExecuteWhenFinished(delegate
        {
            foreach (var assembly in content.assemblies.loadedAssemblies)
            foreach (var type in assembly.GetTypes())
                if (type.TryGetAttribute<ModCompatAttribute>(out var modCompat) && modCompat.ShouldActivate())
                {
                    var method = AccessTools.Method(type, "Activate", Type.EmptyTypes);
                    method?.Invoke(null, Array.Empty<object>());
                    method = AccessTools.Method(type, "Activate", new[] { typeof(Harmony) });
                    method?.Invoke(null, new object[] { Harm });
                    var field = AccessTools.Field(type, "Active");
                    field?.SetValue(null, true);
                    method = AccessTools.Method(type, "GetName");
                    var name = (string)method?.Invoke(null, Array.Empty<object>());
                    method = AccessTools.Method(type, "get_Name");
                    name ??= (string)method?.Invoke(null, Array.Empty<object>());
                    field = AccessTools.Field(type, "Name");
                    name ??= (string)field?.GetValue(null);
                    if (name != null) Log.Message($"[Pawn Editor] {name} compatibility active.");
                }

            Settings = GetSettings<PawnEditorSettings>();
            ApplySettings();
        });
    }

    public override string SettingsCategory() => "PawnEditor".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("PawnEdtior.OverrideVanilla".Translate(), ref Settings.OverrideVanilla, "PawnEditor.OverrideVanilla.Desc".Translate());
        listing.CheckboxLabeled("PawnEditor.InGameDevButton".Translate(), ref Settings.InGameDevButton, "PawnEditor.InGameDevButton.Desc".Translate());
        listing.Label("PawnEditor.PointLimit".Translate() + ": " + Settings.PointLimit.ToStringMoney());
        Settings.PointLimit = listing.Slider(Settings.PointLimit, 100, 10000000);
        listing.CheckboxLabeled("PawnEditor.UseSilver".Translate(), ref Settings.UseSilver, "PawnEditor.UseSilver.Desc".Translate());
        listing.CheckboxLabeled("PawnEditor.CountNPCs".Translate(), ref Settings.CountNPCs, "PawnEditor.CountNPCs.Desc".Translate());
        listing.CheckboxLabeled("PawnEditor.ShowEditButton".Translate(), ref Settings.ShowOpenButton, "PawnEditor.ShowEditButton.Desc".Translate());
        if (listing.ButtonTextLabeled("PawnEditor.HediffLocation".Translate(), $"PawnEditor.HediffLocation.{Settings.HediffLocationLimit}".Translate()))
            Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(PawnEditorSettings.HediffLocation))
               .Cast<PawnEditorSettings.HediffLocation>()
               .Select(loc => new FloatMenuOption($"PawnEditor.HediffLocation.{loc}".Translate(), () => Settings.HediffLocationLimit = loc))
               .ToList()));
        if (Settings.DontShowAgain.Count > 0 && listing.ButtonText("PawnEditor.ResetConfirmation".Translate())) Settings.DontShowAgain.Clear();
        listing.CheckboxLabeled("PawnEditor.EnforceHARRestrictions".Translate(), ref HARCompat.EnforceRestrictions,
            "PawnEditor.EnforceHARRestrictions.Desc".Translate());
        listing.End();
    }

    private void ApplySettings()
    {
        Harm.Unpatch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DoWindowContents)), HarmonyPatchType.Prefix,
            Harm.Id);
        Harm.Unpatch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DrawXenotypeEditorButton)),
            HarmonyPatchType.Prefix,
            Harm.Id);
        Harm.Unpatch(AccessTools.Method(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.DrawButtons)), HarmonyPatchType.Transpiler, Harm.Id);
        Harm.Unpatch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)), HarmonyPatchType.Postfix, Harm.Id);
        if (Settings.OverrideVanilla)
            Harm.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DoWindowContents)),
                new(GetType(), nameof(OverrideVanilla)));
        else
            Harm.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DrawXenotypeEditorButton)),
                new(GetType(), nameof(AddEditorButton)));

        if (Settings.ShowOpenButton)
            Harm.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)), postfix: new(GetType(), nameof(AddEditButton)));

        if (Settings.InGameDevButton)
            Harm.Patch(AccessTools.Method(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.DrawButtons)),
                transpiler: new(GetType(), nameof(AddDevButton)));
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        ApplySettings();
    }

    public static bool OverrideVanilla(Rect rect, Page_ConfigureStartingPawns __instance)
    {
        PawnEditor.Pregame = true;
        PawnEditor.DoUI(rect, __instance.DoBack, __instance.DoNext);
        return false;
    }

    public static bool AddEditorButton(Rect rect, Page_ConfigureStartingPawns __instance)
    {
        float x, y;
        if (ModsConfig.BiotechActive)
        {
            Text.Font = GameFont.Small;
            x = rect.x + rect.width / 2 + 2;
            y = rect.y + rect.height - 38f;
            if (Widgets.ButtonText(new(x, y, Page.BottomButSize.x, Page.BottomButSize.y), "XenotypeEditor".Translate()))
                Find.WindowStack.Add(new Dialog_CreateXenotype(__instance.curPawnIndex, delegate
                {
                    CharacterCardUtility.cachedCustomXenotypes = null;
                    StartingPawnUtility.RandomizePawn(__instance.curPawnIndex);
                }));
            x = rect.x + rect.width / 2 - 2 - Page.BottomButSize.x;
            y = rect.y + rect.height - 38f;
        }
        else
        {
            x = (rect.width - Page.BottomButSize.x) / 2f;
            y = rect.y + rect.height - 38f;
        }

        if (Widgets.ButtonText(new(x, y, Page.BottomButSize.x, Page.BottomButSize.y), "PawnEditor.CharacterEditor".Translate()))
            Find.WindowStack.Add(new Dialog_PawnEditor_Pregame(__instance.DoNext));

        return false;
    }

    public static IEnumerable<CodeInstruction> AddDevButton(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var info = AccessTools.Method(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.ToggleGodMode));
        var idx = codes.FindIndex(ins => ins.Calls(info));
        var label = generator.DefineLabel();
        var labels = codes[idx + 1].ExtractLabels();
        codes[idx + 1].labels.Add(label);
        codes.InsertRange(idx + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            CodeInstruction.LoadField(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.widgetRow)),
            CodeInstruction.LoadField(typeof(TexPawnEditor), nameof(TexPawnEditor.OpenPawnEditor)),
            new CodeInstruction(OpCodes.Ldstr, "PawnEditor.CharacterEditor"),
            CodeInstruction.Call(typeof(Translator), nameof(Translator.Translate), new[] { typeof(string) }),
            CodeInstruction.Call(typeof(TaggedString), "op_Implicit", new[] { typeof(TaggedString) }),
            new CodeInstruction(OpCodes.Ldloca, 0),
            new CodeInstruction(OpCodes.Initobj, typeof(Color?)),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldloca, 0),
            new CodeInstruction(OpCodes.Initobj, typeof(Color?)),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldloca, 0),
            new CodeInstruction(OpCodes.Initobj, typeof(Color?)),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ldc_R4, -1f),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(WidgetRow), nameof(WidgetRow.ButtonIcon))),
            new CodeInstruction(OpCodes.Brfalse, label),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Find), nameof(Find.WindowStack))),
            new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Dialog_PawnEditor_InGame))),
            CodeInstruction.Call(typeof(WindowStack), nameof(WindowStack.Add))
        });
        return codes;
    }

    public static IEnumerable<Gizmo> AddEditButton(IEnumerable<Gizmo> gizmos, Pawn __instance) =>
        DebugSettings.ShowDevGizmos
            ? gizmos.Append(new Command_Action
            {
                defaultLabel = "PawnEditor.Edit".Translate(),
                defaultDesc = "PawnEditor.Edit.Desc".Translate(),
                action = () =>
                {
                    Find.WindowStack.Add(new Dialog_PawnEditor_InGame());
                    PawnEditor.Select(__instance);
                }
            })
            : gizmos;

    public static void Notify_ConfigurePawns()
    {
        StartingThingsManager.ProcessScenario();
        PawnEditor.ResetPoints();
        PawnEditor.CheckChangeTabGroup();
    }
}

public class PawnEditorSettings : ModSettings
{
    public enum HediffLocation
    {
        All, RecipeDef
    }

    public bool CountNPCs;
    public HashSet<string> DontShowAgain = new();
    public HediffLocation HediffLocationLimit;
    public bool InGameDevButton = true;
    public bool OverrideVanilla = true;
    public float PointLimit = 100000;
    public bool ShowOpenButton = true;
    public bool UseSilver;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref DontShowAgain, nameof(DontShowAgain));
        Scribe_Values.Look(ref OverrideVanilla, nameof(OverrideVanilla), true);
        Scribe_Values.Look(ref InGameDevButton, nameof(InGameDevButton), true);
        Scribe_Values.Look(ref ShowOpenButton, nameof(ShowOpenButton), true);
        Scribe_Values.Look(ref PointLimit, nameof(PointLimit));
        Scribe_Values.Look(ref UseSilver, nameof(UseSilver));
        Scribe_Values.Look(ref CountNPCs, nameof(CountNPCs));
        Scribe_Values.Look(ref HediffLocationLimit, nameof(HediffLocationLimit));

        if (HARCompat.Active) Scribe_Values.Look(ref HARCompat.EnforceRestrictions, "EnforceHARRestrictions", true);

        DontShowAgain ??= new();
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class HotSwappableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class ModCompatAttribute : Attribute
{
    private readonly List<string> mods;

    public ModCompatAttribute(params string[] mods) => this.mods = mods.ToList();

    public bool ShouldActivate() => mods.Any(mod => ModLister.GetActiveModWithIdentifier(mod, true) != null);
}
