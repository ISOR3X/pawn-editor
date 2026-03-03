using System.IO;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_GearBasic : SectionWorker
{
    public SectionWorker_GearBasic(SectionDef def) : base(def)
    {
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
       
    }
}