using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioFake(SectionDef def) : SectionWorker(def)
{
    private readonly Color _c = new Color(Random.value, Random.value, Random.value);

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var r = listing.GetRect(64f);
        Widgets.DrawRectFast(r, _c);
        Widgets.Label(r, $"{r.width} x {r.height}");
    }
}