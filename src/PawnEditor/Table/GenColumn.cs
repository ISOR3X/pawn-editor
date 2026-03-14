// using System.Text;
// using RimWorld;
// using UnityEngine;
// using Verse;
// using Verse.Sound;
//
// namespace PawnEditor;
//
// [StaticConstructorOnStartup]
// public abstract class ColumnWorker
// {
//     protected const int DefaultCellHeight = 30;
//     private static readonly Texture2D SortingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Sorting");
//
//     private static readonly Texture2D
//         SortingDescendingIcon = ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending");
//
//     public required ColumnDef Def;
//
//     protected virtual TextAnchor LabelAlignment => TextAnchor.LowerCenter;
//     protected virtual Color HeaderColor => Color.white;
//     protected virtual GameFont HeaderFont => GameFont.Small;
//
//     public virtual bool VisibleCurrently => true;
//
//     public virtual void DoHeader<T>(Rect rect, Table<T> table) where T : class
//     {
//         if (!Def.label.NullOrEmpty())
//         {
//             using (new TextBlock(HeaderFont, LabelAlignment, false))
//             {
//                 var rect1 = rect;
//                 rect1.y += 3f;
//                 Verse.Widgets.Label(rect1, Def.LabelCap.Resolve().Truncate(rect.width).Colorize(HeaderColor));
//             }
//         }
//         else if (Def.HeaderIcon != null)
//         {
//             var headerIconSize = Def.HeaderIconSize;
//             var num = (int)((rect.width - (double)headerIconSize.x) / 2.0);
//             GUI.DrawTexture(
//                 new Rect(rect.x + num, rect.yMax - headerIconSize.y, headerIconSize.x, headerIconSize.y)
//                     .ContractedBy(2f), Def.HeaderIcon);
//         }
//
//         if (table.SortingBy != null && table.SortingBy.Equals(Def))
//         {
//             var image = table.SortingDescending ? SortingDescendingIcon : SortingIcon;
//             GUI.DrawTexture(
//                 new Rect((float)(rect.xMax - (double)image.width - 1.0),
//                     (float)(rect.yMax - (double)image.height - 1.0), image.width, image.height), image);
//         }
//
//         if (!Def.HeaderInteractable)
//             return;
//
//         var interactableHeaderRect = GetInteractableHeaderRect(rect, table);
//         if (Mouse.IsOver(interactableHeaderRect))
//         {
//             Verse.Widgets.DrawHighlight(interactableHeaderRect);
//             var headerTip = GetHeaderTip(table);
//             if (!headerTip.NullOrEmpty())
//                 TooltipHandler.TipRegion(interactableHeaderRect, (TipSignal)headerTip);
//         }
//
//         if (Verse.Widgets.ButtonInvisible(interactableHeaderRect)) HeaderClicked(rect, table);
//         
//         MouseoverSounds.DoRegion(rect);
//     }
//
//     public virtual int GetMinWidth<T>(Table<T> table) where T : class
//     {
//         if (!Def.label.NullOrEmpty())
//             using (new TextBlock(HeaderFont))
//                 return Mathf.CeilToInt(Text.CalcSize(Def.LabelCap).x);
//
//         return Def.HeaderIcon != null ? Mathf.CeilToInt(Def.HeaderIconSize.x) : 1;
//     }
//
//     public virtual int GetMaxWidth<T>(Table<T> table) where T : class
//     {
//         return 1000000;
//     }
//
//     public virtual int GetOptimalWidth<T>(Table<T> table) where T : class
//     {
//         return GetMinWidth(table);
//     }
//
//     public virtual int GetMinHeaderHeight<T>(Table<T> table) where T : class
//     {
//         if (!Def.label.NullOrEmpty())
//             using (new TextBlock(HeaderFont))
//                 return Mathf.CeilToInt(Text.CalcSize(Def.LabelCap).y);
//
//         return Def.HeaderIcon != null ? Mathf.CeilToInt(Def.HeaderIconSize.y) : 0;
//     }
//
//     protected virtual Rect GetInteractableHeaderRect<T>(Rect headerRect, Table<T> table) where T : class
//     {
//         var height = Mathf.Min(25f, headerRect.height);
//         return new Rect(headerRect.x, headerRect.yMax - height, headerRect.width, height);
//     }
//
//     protected virtual void HeaderClicked<T>(Rect headerRect, Table<T> table) where T : class
//     {
//         if (!Def.sortable || Event.current.shift)
//             return;
//         if (Event.current.button == 0)
//         {
//             if (table.SortingBy == null || !table.SortingBy.Equals(Def))
//             {
//                 table.SortBy(Def, true);
//                 SoundDefOf.Tick_High.PlayOneShotOnCamera();
//             }
//             else if (table.SortingDescending)
//             {
//                 table.SortBy(Def, false);
//                 SoundDefOf.Tick_High.PlayOneShotOnCamera();
//             }
//             else
//             {
//                 table.SortBy(null, false);
//                 SoundDefOf.Tick_Low.PlayOneShotOnCamera();
//             }
//         }
//         else
//         {
//             if (Event.current.button != 1)
//                 return;
//             if (table.SortingBy == null || !table.SortingBy.Equals(Def))
//             {
//                 table.SortBy(Def, false);
//                 SoundDefOf.Tick_High.PlayOneShotOnCamera();
//             }
//             else if (table.SortingDescending)
//             {
//                 table.SortBy(null, false);
//                 SoundDefOf.Tick_Low.PlayOneShotOnCamera();
//             }
//             else
//             {
//                 table.SortBy(Def, true);
//                 SoundDefOf.Tick_High.PlayOneShotOnCamera();
//             }
//         }
//     }
//
//     protected virtual string GetHeaderTip<T>(Table<T> table) where T : class
//     {
//         var stringBuilder = new StringBuilder();
//         if (!Def.headerTip.NullOrEmpty())
//             stringBuilder.Append(Def.headerTip);
//         if (Def.sortable)
//         {
//             if (stringBuilder.Length != 0)
//             {
//                 stringBuilder.AppendLine();
//                 stringBuilder.AppendLine();
//             }
//
//             stringBuilder.Append("ClickToSortByThisColumn".Translate());
//         }
//
//         return stringBuilder.ToString();
//     }
// }
//
// public abstract class ColumnWorker<T> : ColumnWorker where T : class
// {
//     public abstract void DoCell(Rect inRect, T item, Table<T> table);
//
//     public virtual bool CanGroupWith(T item, T other) => false;
//
//     public virtual int GetMinCellHeight(T item) => (int)Table<T>.DefaultRowHeight;
//
//     public virtual int Compare(T a, T b) => 0;
// }
//
// public abstract class ColumnWorker_Def : ColumnWorker<Def>
// {
// }
//
// public abstract class ColumnWorker_Thing : ColumnWorker<Thing>
// {
// }