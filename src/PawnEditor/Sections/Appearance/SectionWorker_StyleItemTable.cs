using PawnEditor.Table;
using PawnEditor.Table.ColumnWorkers;
using RimWorld;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public abstract class SectionWorker_StyleItemTable<T>(SectionDef def) : SectionWorker(def) where T : StyleItemDef
{
    private Pawn? _cachedPawn;
    private Table<T>? _cachedTable;
    protected abstract List<T> TableItems { get; }
    protected abstract string TableTitle { get; }
    protected virtual Action<Pawn, T>? OnRowClick => null;
    protected virtual Func<Pawn, bool> ShowTableForPawn => _ => true;
    protected virtual Func<Pawn, T, bool> HighlightRow => (_, _) => false;

    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<TextElement>("text").Content = TableTitle;
        layout.ComponentById<DivElement>("table").Draw = r =>
        {
            if (ShowTableForPawn(pawn))
                GetCachedTable(pawn, TableItems, OnRowClick, HighlightRow).Draw(r);
            else
                using (new TextBlock(TextAnchor.MiddleCenter))
                {
                    Verse.Widgets.Label(r, "No options available".Colorize(ColoredText.SubtleGrayColor));
                }
        };
    }

    private Table<T> GetCachedTable(Pawn pawn, List<T> defs, Action<Pawn, T>? onRowClick = null,
        Func<Pawn, T, bool>? highlightRow = null)
    {
        if (_cachedTable == null || !ReferenceEquals(_cachedPawn, pawn))
        {
            _cachedTable = ConstructStyleItemTable(pawn, defs, onRowClick, highlightRow);
            _cachedPawn = pawn;
        }

        return _cachedTable;
    }

    private static Table<T> ConstructStyleItemTable(Pawn pawn, List<T> defs, Action<Pawn, T>? onRowClick = null,
        Func<Pawn, T, bool>? highlightRow = null)
    {
        return new Table<T>(
            defs,
            [
                ColumnWorker<T>.Create<PawnContext>(
                    Void.Taffy.Px(36f),
                    (grid, def, ctx) => grid.Icon(def.Icon, ctx.Value.story.HairColor, UIUtility.ComponentSize.Large)
                ),
                ColumnWorker<T>.CreateText(
                    Void.Taffy.Fr(3), def => def.LabelCap, "Label"
                ),
                new ColumnWorker_Gender<T>(),
                ColumnWorker<T>.CreateText(Void.Taffy.Fr(), def => def.StyleItemCategory.LabelCap, "Style"),
                ColumnWorker<T>.CreateText(
                    Void.Taffy.Fr(),
                    def => def.modContentPack.Name,
                    "Source",
                    ColoredText.SubtleGrayColor
                )
            ],
            onRowClick: row =>
            {
                if (row != null) onRowClick?.Invoke(pawn, row);
            },
            onRowHover: (rowRect, styleItemDef, ctx) =>
            {
                if (ctx is not PawnContext pawnContext) return;
                UIUtility.DefIconPreview(rowRect, styleItemDef, pawnContext.Value.story.HairColor);
            },
            highlightRow: row => highlightRow?.Invoke(pawn, row) ?? false,
            context: new PawnContext(pawn),
            searchProjection: def => def.LabelCap
        );
    }
}