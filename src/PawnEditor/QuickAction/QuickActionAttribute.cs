namespace PawnEditor;

/// <summary>
///     Used to define a Pawn action added to the quick action button for each tab.
/// </summary>
public class QuickActionAttribute(
    string tabDefName,
    string name,
    bool requiresRoyalty = false,
    bool requiresIdeology = false,
    bool requiresBiotech = false,
    bool requiresAnomaly = false,
    bool hideAtEntryProgramState = false,
    int displayPriority = 0)
    : Attribute
{
    public Action? action;
    public int displayPriority = displayPriority;
    public bool hideAtEntryProgramState = hideAtEntryProgramState;
    public string name = name;
    public bool requiresAnomaly = requiresAnomaly;
    public bool requiresBiotech = requiresBiotech;
    public bool requiresIdeology = requiresIdeology;
    public bool requiresRoyalty = requiresRoyalty;
    public string tabDefName = tabDefName;
}