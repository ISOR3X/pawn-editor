using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class Listing_TreeThing : Listing_Thing<ThingDef>
{
    public Listing_TreeThing(List<ThingDef> items, Func<ThingDef, string> labelGetter, Func<ThingDef, string> descGetter = null,
        List<Filter<ThingDef>> filters = null)
        : base(items, labelGetter, descGetter, filters) { }

    public Listing_TreeThing(List<ThingDef> items, Func<ThingDef, string> labelGetter, Action<ThingDef, Rect> iconDrawer,
        Func<ThingDef, string> descGetter = null, List<Filter<ThingDef>> filters = null)
        : base(items, labelGetter, iconDrawer, descGetter, filters) { }


    public void ListCategoryChildren(
        TreeNode_ThingCategory node,
        int openMask,
        Rect visibleRect)
    {
        VisibleRect = visibleRect;
        ColumnWidth = visibleRect.width - 16f;
        DoCategoryChildren(node, 0, openMask, false);
    }

    private void DoCategoryChildren(
        TreeNode_ThingCategory node,
        int indentLevel,
        int openMask,
        bool subtreeMatchedSearch)
    {
        foreach (var childCategoryNode in node.ChildCategoryNodes)
            if (Visible(childCategoryNode) && !HideCategoryDueToSearch(childCategoryNode) && !HideCategoryDueToDisallowed(childCategoryNode))
                DoCategory(childCategoryNode, indentLevel, openMask, subtreeMatchedSearch);

        var i = 0;
        foreach (var sortedChildThingDef in node.catDef.SortedChildThingDefs)
            if (Visible(sortedChildThingDef) && !HideThingDueToSearch(sortedChildThingDef))
            {
                DoThing(sortedChildThingDef, indentLevel, i);
                i++;
            }


        bool HideCategoryDueToSearch(TreeNode_ThingCategory subCat) =>
            !(!SearchFilter.filter.Active | subtreeMatchedSearch) && !CategoryMatches(subCat) && !ThisOrDescendantsVisibleAndMatchesSearch(subCat);

        bool HideCategoryDueToDisallowed(TreeNode_ThingCategory subCat) => subCat.catDef == ThingCategoryDefOf.Corpses || subCat.catDef.defName == "Unfinished";

        bool HideThingDueToSearch(ThingDef tDef) => !(!SearchFilter.filter.Active | subtreeMatchedSearch) && !SearchFilter.filter.Matches(tDef);
    }

    private void DoCategory(
        TreeNode_ThingCategory node,
        int indentLevel,
        int openMask,
        bool subtreeMatchedSearch)
    {
        var textColor = new Color?();
        if (SearchFilter.filter.Active)
        {
            if (CategoryMatches(node))
                subtreeMatchedSearch = true;
            else
                textColor = Listing_TreeThingFilter.NoMatchColor;
        }

        if (CurrentRowVisibleOnScreen())
        {
            OpenCloseWidget(node, indentLevel, openMask);
            LabelLeft(node.LabelCap, node.catDef.description, indentLevel, textColor: textColor);
        }

        EndLine();
        if (!IsOpen(node, openMask))
            return;
        DoCategoryChildren(node, indentLevel + 1, openMask, subtreeMatchedSearch);
    }

    public override bool IsOpen(TreeNode node, int openMask) =>
        base.IsOpen(node, openMask) || (node is TreeNode_ThingCategory node1 && SearchFilter.filter.Active && ThisOrDescendantsVisibleAndMatchesSearch(node1));

    private bool ThisOrDescendantsVisibleAndMatchesSearch(TreeNode_ThingCategory node)
    {
        if (Visible(node) && CategoryMatches(node))
            return true;
        foreach (var childThingDef in node.catDef.childThingDefs)
            if (ThingDefVisibleAndMatches(childThingDef))
                return true;

        foreach (var childCategory in node.catDef.childCategories)
            if (ThisOrDescendantsVisibleAndMatchesSearch(childCategory.treeNode))
                return true;

        return false;

        bool ThingDefVisibleAndMatches(ThingDef td) => Visible(td) && SearchFilter.filter.Matches(td);
    }

    private bool CategoryMatches(TreeNode_ThingCategory node) => SearchFilter.filter.Matches(node.catDef.label);

    private bool Visible(TreeNode_ThingCategory node) => node.catDef.DescendantThingDefs.Any(Visible);
}
