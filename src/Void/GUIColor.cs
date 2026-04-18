using UnityEngine;

namespace Void;

/// <summary>
///     Allows setting <see cref="GUI.color" /> through the using keyword, automatically restoring the old color when
///     exiting scope. />
/// </summary>
public class GUIColor : IDisposable
{
    private readonly Color _oldColor;

    public GUIColor(Color newColor)
    {
        _oldColor = GUI.color;
        GUI.color = newColor;
    }

    public void Dispose()
    {
        GUI.color = _oldColor;
    }
}