using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

    public static void EditButton<T>(Rect rect, T item, Pawn pawn = null, UIElement element = null)
    {
        if (!Widgets.ButtonText(rect, "PawnEditor.Edit".Translate() + "...")) return;
        var type = WindowForType(typeof(T));
        if (CurrentWindow != null && type.IsInstanceOfType(CurrentWindow))
        {
            var window = (Dialog_EditItem<T>)CurrentWindow;
            if (window.IsSelected(item))
            {
                if (Find.WindowStack.IsOpen(window))
                {
                    window.Close();
                    CurrentWindow = null;
                }
                else
                    Find.WindowStack.Add(window);
            }
            else
            {
                window.TableRect = rect;
                if (element != null) window.TableRect.x = element.Position.x;
                window.Select(item);
                Find.WindowStack.RemoveWindowsOfType(type);
                Find.WindowStack.Add(window);
            }
        }
        else // Used when switching tabs/ using different dialogs.
        {
            CurrentWindow?.Close(false);
            CurrentWindow = (Dialog_EditItem)Activator.CreateInstance(type, item, pawn, element);
            CurrentWindow.TableRect = rect;
            if (element != null) CurrentWindow.TableRect.x = element.Position.x;
            Find.WindowStack.Add(CurrentWindow);
        }
    }
}
