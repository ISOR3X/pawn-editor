using UnityEngine;
using Verse;

namespace Void.Extensions;

public static class Rect_Extension
{
    extension(ref Rect rect)
    {
        public Rect TakeTopPart(float pixels)
        {
            var ret = rect.TopPartPixels(pixels);
            rect.yMin += pixels;
            return ret;
        }

        public Rect TakeBottomPart(float pixels)
        {
            var ret = rect.BottomPartPixels(pixels);
            rect.yMax -= pixels;
            return ret;
        }

        public Rect TakeRightPart(float pixels)
        {
            var ret = rect.RightPartPixels(pixels);
            rect.xMax -= pixels;
            return ret;
        }

        public Rect TakeLeftPart(float pixels)
        {
            var ret = rect.LeftPartPixels(pixels);
            rect.xMin += pixels;
            return ret;
        }


        public void Indent(float width = 4f)
        {
            rect.xMin += width;
        }

        public void Gap(float height = 4f)
        {
            rect.yMin += height;
        }
    }

    extension(Rect rect)
    {
        public void LabelH2(string label)
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Verse.Widgets.Label(rect, label.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
            }
        }

        public Rect CenteredVertically(float height)
        {
            var remove = (rect.height - height) / 2;
            rect.yMax -= remove;
            rect.yMin += remove;
            return rect;
        }

        public Rect CenteredHorizontally(float width)
        {
            var remove = (rect.width - width) / 2;
            rect.xMax -= remove;
            rect.xMin += remove;
            return rect;
        }

        public void SplitHorizontallyEqual(out Rect top, out Rect bottom, float padding = 0)
        {
            var half = rect.height / 2;
            top = rect.TopPartPixels(half - padding);
            bottom = rect.BottomPartPixels(half - padding);
        }
    }
}