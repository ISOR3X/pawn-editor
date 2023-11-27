using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class EditUtility
{
    private static readonly List<Type> editTypes;

    static EditUtility()
    {
        editTypes = typeof(Dialog_EditItem).AllSubclassesNonAbstract().Where(type => !type.IsGenericTypeDefinition).ToList();
    }

    public static Dialog_EditItem CurrentWindow { get; private set; }

    private static Type WindowForType(Type type) =>
        editTypes.FirstOrDefault(editType => typeof(Dialog_EditItem<>).MakeGenericType(type).IsAssignableFrom(editType));

    public static void Edit<T>(T item, Pawn pawn = null, UITable<Pawn> table = null)
    {
        var type = WindowForType(typeof(T));
        if (CurrentWindow != null && type.IsInstanceOfType(CurrentWindow))
        {
            var window = (Dialog_EditItem<T>)CurrentWindow;
            if (window.IsSelected(item))
            {
                window.Close();
                CurrentWindow = null;
            }
            else
            {
                window.Select(item);
                if (!Find.WindowStack.IsOpen(window)) Find.WindowStack.Add(window);
            }
        }
        else
        {
            CurrentWindow?.Close(false);
            CurrentWindow = (Dialog_EditItem)Activator.CreateInstance(type, item, pawn, table);
            Find.WindowStack.Add(CurrentWindow);
        }
    }
}
