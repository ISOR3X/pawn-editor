using System;
using Verse;

namespace PawnEditor;

public enum PawnCategory
{
    Humans,
    Animals,
    Mechs,
    All
}

public static class PawnCategoryExtensions
{
    public static string Label(this PawnCategory category) => $"PawnEditor.PawnCategory.{category}".Translate();
    public static string LabelCap(this PawnCategory category) => category.Label().CapitalizeFirst();
    public static string LabelPlural(this PawnCategory category) => Find.ActiveLanguageWorker.Pluralize(Label(category));
    public static string LabelCapPlural(this PawnCategory category) => category.LabelPlural().CapitalizeFirst();

    public static bool Includes(this PawnCategory category, Pawn pawn)
    {
        return category switch
        {
            PawnCategory.Humans => pawn.RaceProps.Humanlike,
            PawnCategory.Animals => pawn.RaceProps.Animal,
            PawnCategory.Mechs => pawn.RaceProps.IsMechanoid,
            PawnCategory.All => true,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }
}
