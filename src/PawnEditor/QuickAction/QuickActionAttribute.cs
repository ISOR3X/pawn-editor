using System;

namespace PawnEditor;

/// <summary>
/// Used to define a Pawn action that are added to the quick action button for each tab.
/// </summary>
public class QuickActionAttribute : Attribute
{
    public string name;
    public string tabDefName;
    public bool requiresRoyalty;
    public bool requiresIdeology;
    public bool requiresBiotech;
    public bool requiresAnomaly;
    public int displayPriority;
    public bool hideAtEntryProgramState;
    public Action action;

    public QuickActionAttribute(
        string tabDefName,
        string name = null,
        bool requiresRoyalty = false,
        bool requiresIdeology = false,
        bool requiresBiotech = false,
        bool requiresAnomaly = false,
        bool hideAtEntryProgramState = false,
        int displayPriority = 0)
    {
        this.name = name;
        this.tabDefName = tabDefName;
        this.requiresRoyalty = requiresRoyalty;
        this.requiresIdeology = requiresIdeology;
        this.requiresBiotech = requiresBiotech;
        this.requiresAnomaly = requiresAnomaly;
        this.hideAtEntryProgramState = hideAtEntryProgramState;
        this.displayPriority = displayPriority;
    }
}