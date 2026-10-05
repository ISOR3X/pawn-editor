using System.Runtime.CompilerServices;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;

public static partial class VoidComponents
{
    public record struct PawnInfo(string Name, int Age, string Role);

    public static List<PawnInfo> pawns = [];

    public record struct TableColumn
    {
        public string? Header = null;
        public Texture2D? Icon = null;
        public Style Style;

        public TableColumn(string? header = null, Texture2D? icon = null, Style? style = null, Action<UIBranch, PawnInfo>? drawCell = null, object? sort = null)
        {
            Header = header;
            Icon = icon;
            Style = (style ?? new Style()).Merge(new Style
            {
                width = Dimension.Auto()
            });
        }

        public void DrawHeader(UIBranch b)
        {
            if (Header != null)
            {
                b.Text(Header);
            }
            if (Icon != null)
            {
                b.Icon(Icon);
            }
        }

        public void DrawCell(UIBranch b) { }
    }

    private sealed class TableState
    {
        public bool descending;
        public string? key;
    }

    public static void Draw(UIBranch b)
    {
        List<TableColumn> cols = [
            new TableColumn( "Name" ),
            new TableColumn( "Age" ),
            new TableColumn( icon: Verse.Widgets.AltTexture ),
        ];

        var state = b.UpsertState("table-key", () => new TableState { });

        b.Div(b2 =>
        {
            b2.Div(b3 =>
            {
                foreach (var c in cols)
                {
                    b3.Div(b4 =>
                    {
                        c.DrawHeader(b4);
                    }, draw: r =>
                    {
                        if (Verse.Widgets.ButtonInvisible(r))
                        {
                            var colKey = c.Header ?? c.Icon?.name ?? "";
                            if (state.key != colKey) state.key = colKey;
                            else
                            {
                                if (state.descending) state.descending = !state.descending;
                                else
                                {
                                    state.key = null;
                                }
                            }

                            pawns.SortBy(p => p.Name);
                        }
                    }, style: c.Style);
                }
            });
            b2.Div(b3 =>
            {
                foreach (var p in pawns)
                {
                    b3.Div(b4 =>
                    {
                        foreach (var c in cols)
                        {
                            b4.Div(b4 =>
                            {
                                c.DrawCell(b);
                            }, style: c.Style);
                        }
                    });
                }
            }, style: new Style { overflowY = TaffyOverflow.Scroll });
        });
    }

}
