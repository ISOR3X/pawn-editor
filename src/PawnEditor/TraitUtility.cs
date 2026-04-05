using RimWorld;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class TraitUtility
{
    public static readonly IReadOnlyCollection<TraitRecord> AllTraits;

    static TraitUtility()
    {
        AllTraits = DefDatabase<TraitDef>.AllDefs.SelectMany(traitDef =>
            traitDef.degreeDatas.Select(degree => new TraitRecord(traitDef, degree))).ToList();
    }

    /// <summary>
    /// TraitDefs hold multiple trait definitions 
    /// </summary>
    public readonly record struct TraitRecord(TraitDef TraitDef, TraitDegreeData Degree)
    {
        public Trait Trait => new(TraitDef, Degree.degree);
        public TraitDegreeData TraitDegreeData => Degree;
    }
}