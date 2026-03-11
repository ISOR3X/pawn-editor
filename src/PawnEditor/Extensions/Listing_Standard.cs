using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor.Extensions;

[HotSwappable]
public static class Listing_Standard_Extension
{
    extension(Listing_Standard listing)
    {
        public Rect LabelH2(string label)
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                return listing.Label(label.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
            }
        }

        public bool ButtonText_TruncateWithTooltip(string label, float padding = 16f)
        {
            var width = listing.ColumnWidth;
            if (!(Text.CalcSize(label).x > width - padding)) return listing.ButtonText(label.Truncate(width - padding));
            TooltipHandler.TipRegion(listing.GetRect(Text.LineHeight), label);
            // listing.curY -= Text.LineHeight; // Remove the height of the tooltip rect.

            return listing.ButtonText(label.Truncate(width - padding));
        }

        public bool ButtonText_Fit(string label, float padding = 16f)
        {
            var w = Text.CalcSize(label).x + padding * 2;
            var r = listing.GetRect(UIUtility.ButtonHeight);
            return Verse.Widgets.ButtonText(r.TakeLeftPart(w), label);
        }

        public Rect RectLabeled(string label, float height = UIUtility.ButtonHeight, float? labelWidth = null)
        {
            var rect = listing.GetRect(height);
            return UIUtility.RectLabeled(rect, label, labelWidth);
        }
    }
}