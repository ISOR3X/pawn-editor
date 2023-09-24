using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class Listing_TreeThing : Listing_Thing<ThingDef>
{
    public Listing_TreeThing(QuickSearchFilter searchFilter, List<ThingDef> things) : base(searchFilter, things)
    {
    }

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
        foreach (TreeNode_ThingCategory childCategoryNode in node.ChildCategoryNodes)
        {
            if (Visible(childCategoryNode) && !HideCategoryDueToSearch(childCategoryNode) && !HideCategoryDueToDisallowed(childCategoryNode))
                DoCategory(childCategoryNode, indentLevel, openMask, subtreeMatchedSearch);
        }

        foreach (ThingDef sortedChildThingDef in node.catDef.SortedChildThingDefs)
        {
            if (Visible(sortedChildThingDef) && !HideThingDueToSearch(sortedChildThingDef))
                DoThing(sortedChildThingDef, indentLevel);
        }


        bool HideCategoryDueToSearch(TreeNode_ThingCategory subCat) => !(!SearchFilter.Active | subtreeMatchedSearch) && !CategoryMatches(subCat) && !ThisOrDescendantsVisibleAndMatchesSearch(subCat);

        bool HideCategoryDueToDisallowed(TreeNode_ThingCategory subCat)
        {
            return subCat.catDef == ThingCategoryDefOf.Corpses || subCat.catDef.defName == "Unfinished";
        }

        bool HideThingDueToSearch(ThingDef tDef) => !(!SearchFilter.Active | subtreeMatchedSearch) && !SearchFilter.Matches(tDef);
    }

    private void DoCategory(
        TreeNode_ThingCategory node,
        int indentLevel,
        int openMask,
        bool subtreeMatchedSearch)
    {
        Color? textColor = new Color?();
        if (SearchFilter.Active)
        {
            if (CategoryMatches(node))
            {
                subtreeMatchedSearch = true;
            }
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

    protected override bool Visible(ThingDef td) => td.PlayerAcquirable && base.Visible(td);

    public override bool IsOpen(TreeNode node, int openMask) => base.IsOpen(node, openMask) || node is TreeNode_ThingCategory node1 && SearchFilter.Active && ThisOrDescendantsVisibleAndMatchesSearch(node1);

    private bool ThisOrDescendantsVisibleAndMatchesSearch(TreeNode_ThingCategory node)
    {
        if (Visible(node) && CategoryMatches(node))
            return true;
        foreach (ThingDef childThingDef in node.catDef.childThingDefs)
        {
            if (ThingDefVisibleAndMatches(childThingDef))
                return true;
        }

        foreach (ThingCategoryDef childCategory in node.catDef.childCategories)
        {
            if (ThisOrDescendantsVisibleAndMatchesSearch(childCategory.treeNode))
                return true;
        }

        return false;

        bool ThingDefVisibleAndMatches(ThingDef td) => Visible(td) && SearchFilter.Matches(td);
    }

    private bool CategoryMatches(TreeNode_ThingCategory node) => SearchFilter.Matches(node.catDef.label);

    private bool Visible(TreeNode_ThingCategory node) => node.catDef.DescendantThingDefs.Any(Visible);
}