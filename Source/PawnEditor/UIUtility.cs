using UnityEngine;
using Verse;

namespace PawnEditor
{
    public static class UIUtility
    {
        public static Rect TakeTopPart(ref this Rect rect, float pixels)
        {
            var ret = rect.TopPartPixels(pixels);
            rect.yMin += pixels;
            return ret;
        }

        public static Rect TakeBottomPart(ref this Rect rect, float pixels)
        {
            var ret = rect.BottomPartPixels(pixels);
            rect.yMax -= pixels;
            return ret;
        }

        public static Rect TakeRightPart(ref this Rect rect, float pixels)
        {
            var ret = rect.RightPartPixels(pixels);
            rect.xMax -= pixels;
            return ret;
        }

        public static Rect TakeLeftPart(ref this Rect rect, float pixels)
        {
            var ret = rect.LeftPartPixels(pixels);
            rect.xMin += pixels;
            return ret;
        }
    }
}
