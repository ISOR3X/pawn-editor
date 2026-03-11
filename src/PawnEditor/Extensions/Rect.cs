using UnityEngine;
using Verse;

namespace PawnEditor.Extensions;

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
    }
}