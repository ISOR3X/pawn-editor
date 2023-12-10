using Verse;

namespace PawnEditor;

public static partial class PawnEditor
{
    private static Pawn clipboard;

    public static bool CanPaste => clipboard != null;

    public static void Copy(Pawn pawn)
    {
        clipboard = pawn;
    }

    public static void ClearClipboard()
    {
        clipboard = null;
    }

    public static void Paste(Pawn into)
    {
        if (into.apparel != null && clipboard?.apparel != null)
        {
            into.apparel.DestroyAll();
            foreach (var apparel in clipboard.apparel.WornApparel) into.apparel.Wear(apparel.Clone(), false, clipboard.apparel.IsLocked(apparel));
        }

        if (into.equipment != null && clipboard?.equipment != null)
        {
            into.equipment.DestroyAllEquipment();
            foreach (var eq in clipboard.equipment.AllEquipmentListForReading) into.equipment.AddEquipment(eq.Clone());
        }

        if (into.inventory != null && clipboard?.inventory != null)
        {
            into.inventory.DestroyAll();
            foreach (var thing in into.inventory.innerContainer) into.inventory.innerContainer.TryAdd(thing.Clone(), false);
        }
    }

    private static T Clone<T>(this T thing) where T : Thing
    {
        var clone = (T)ThingMaker.MakeThing(thing.def, thing.Stuff);
        clone.HitPoints = thing.HitPoints;
        if (thing is ThingWithComps twc)
            foreach (var comp in twc.AllComps)
                comp.PostSplitOff(clone);

        return clone;
    }
}
