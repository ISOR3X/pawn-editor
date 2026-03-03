using UnityEngine;
using Verse;

namespace PawnEditor;

public class Listing_Advanced : Listing_Standard
{
    private float heightAfterPush;
    private float heightAtPush;
    private float totalHeight;

    public override void Begin(Rect inRect)
    {
        base.Begin(inRect);
        // if (totalHeight > 0) this.curY = inRect.yMax - totalHeight;
    }

    public override void End()
    {
        if (totalHeight == 0)
        {
            totalHeight = curY;
            heightAfterPush = totalHeight - heightAtPush;
        }

        base.End();
    }

    public Rect PushDownFromHere()
    {
        if (heightAtPush == 0) heightAtPush = curY;
        else curY = listingRect.yMax - heightAfterPush;
        return new Rect(listingRect.x, heightAtPush, listingRect.width,
            listingRect.height - (heightAfterPush + heightAtPush));
    }
}