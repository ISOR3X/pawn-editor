using UnityEngine;

namespace Void;

public class GUIColor : IDisposable
{
    private readonly Color oldColor;

    public GUIColor(Color newColor)
    {
        oldColor = GUI.color;
        GUI.color = newColor;
    }

    public void Dispose()
    {
        GUI.color = oldColor;
    }
}