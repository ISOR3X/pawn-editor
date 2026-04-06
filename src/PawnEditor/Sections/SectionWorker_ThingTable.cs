using HotSwap;
using PawnEditor.Extensions;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker_ThingTable(SectionDef def) : SectionWorker(def)
{
    private const int RowCount = 11;
    private const float RowHeight = 30f;

    private Pawn? _lastPawn;

    protected abstract ThingTableDef TableDef { get; }
    protected abstract string Label { get; }
    protected abstract IEnumerable<Thing> GetThings();

    private ThingTableWorker Table => field ??= (ThingTableWorker)Activator.CreateInstance(
        TableDef.workerClass, TableDef, (Func<IEnumerable<Thing>>)GetThings, null);

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        if (_lastPawn != pawn)
        {
            _lastPawn = pawn;
            Table.SetDirty();
        }

        var rowCount = Math.Clamp(Table.ThingListForReading.Count, 1, RowCount);
        var tableHeight = Table.HeaderHeight + rowCount * RowHeight + UIUtility.ButtonHeight + 4f;

        builder.Item(height: Text.LineHeight, draw: r => r.LabelH2(Label));
        builder.Item(height: tableHeight, draw: r => Table.TableOnGUI(r));
    }
}
