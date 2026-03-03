using System;
using UnityEngine;

namespace PawnEditor;

public class GUIColor : IDisposable
{
    private Color oldColor;

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