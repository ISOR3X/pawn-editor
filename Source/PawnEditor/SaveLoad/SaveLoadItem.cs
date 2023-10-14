using System;
using Verse;

namespace PawnEditor;

public abstract class SaveLoadItem
{
    public abstract FloatMenuOption MakeSaveOption();
    public abstract FloatMenuOption MakeLoadOption();
}

public class SaveLoadItem<T> : SaveLoadItem where T : IExposable
{
    private readonly T item;
    private readonly string label;
    private readonly SaveLoadParms<T> parms;

    public SaveLoadItem(string label, T item, SaveLoadParms<T> parms = default)
    {
        this.label = label.ToLower();
        this.item = item;
        this.parms = parms;
    }

    public override FloatMenuOption MakeSaveOption() => new(parms.SaveLabel ?? "Save".Translate() + " " + label, Save);

    public override FloatMenuOption MakeLoadOption() => new(parms.LoadLabel ?? "Load".Translate() + " " + label, Load);

    private void Save()
    {
        SaveLoadUtility.SaveItem(item, parms.OnSave, parms.ParentPawn, parms.PrepareSave, parms.TypePostfix);
    }

    private void Load()
    {
        SaveLoadUtility.LoadItem(item, parms.OnLoad, parms.ParentPawn, parms.PrepareLoad, parms.TypePostfix);
    }
}

public class SaveItem : SaveLoadItem
{
    private readonly FloatMenuOption option;

    public SaveItem(FloatMenuOption option) => this.option = option;
    public SaveItem(string label, Action action) => option = new(label, action);

    public override FloatMenuOption MakeSaveOption() => option;
    public override FloatMenuOption MakeLoadOption() => null;
}

public struct SaveLoadParms<T> where T : IExposable
{
    public Action<T> OnSave;
    public Action<T> OnLoad;
    public Action<T> PrepareSave;
    public Action<T> PrepareLoad;
    public string SaveLabel;
    public string LoadLabel;
    public Pawn ParentPawn;
    public string TypePostfix;
}
