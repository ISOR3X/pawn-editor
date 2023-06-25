using System;
using Verse;

namespace PawnEditor;

public abstract class SaveLoadItem
{
    public abstract FloatMenuOption MakeSaveOption();
    public abstract FloatMenuOption MakeLoadOption();
}

public class SaveLoadItem<T> : SaveLoadItem where T : IExposable, new()
{
    private readonly T item;
    private readonly string label;
    private readonly SaveLoadParms<T> parms;

    public SaveLoadItem(string label, T item, SaveLoadParms<T> parms = default)
    {
        this.label = label;
        this.item = item;
        this.parms = parms;
    }

    public override FloatMenuOption MakeSaveOption() => new(parms.SaveLabel ?? "Save".Translate() + " " + label, Save);

    public override FloatMenuOption MakeLoadOption() => new(parms.LoadLabel ?? "Load".Translate() + " " + label, Load);

    private void Save()
    {
        SaveLoadUtility.SaveItem(item, parms.OnSave, parms.ParentPawn);
    }

    private void Load()
    {
        SaveLoadUtility.LoadItem(item, parms.OnLoad, parms.ParentPawn);
    }
}

public struct SaveLoadParms<T> where T : IExposable
{
    public Action<T> OnSave;
    public Action<T> OnLoad;
    public string SaveLabel;
    public string LoadLabel;
    public Pawn ParentPawn;
}
