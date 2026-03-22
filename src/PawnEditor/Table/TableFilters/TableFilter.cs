using HotSwap;
using PawnEditor.Extensions;
using Verse;

namespace PawnEditor.Table;

[HotSwappable]
public abstract class TableFilter
{
    protected abstract string Label { get; }
    
    public virtual void DrawFilter(Listing_Standard listing)
    {
        listing.LabelH2(Label);
        DrawFilterWidget(listing);
    }

    protected abstract void DrawFilterWidget(Listing_Standard listing);

    public abstract bool Matches(Def thing);
}