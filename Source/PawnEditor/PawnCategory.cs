using Verse;

namespace PawnEditor;

public enum PawnCategory
{
    Colonists,
    Humans,
    Animals,
    Mechs
}

public static class PawnCategoryExtensions
{
    public static string Label(this PawnCategory category) => $"PawnEditor.PawnCategory.{category}".Translate();
    public static string LabelCap(this PawnCategory category) => category.Label().CapitalizeFirst();
    public static string LabelPlural(this PawnCategory category) => Find.ActiveLanguageWorker.Pluralize(Label(category));
    public static string LabelCapPlural(this PawnCategory category) => category.LabelPlural().CapitalizeFirst();
}
