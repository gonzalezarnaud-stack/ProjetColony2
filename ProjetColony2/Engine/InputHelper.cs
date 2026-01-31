using System.Collections.Generic;
using Godot;

namespace ProjetColony2.Engine;

public static class InputHelper
{
    private static HashSet<MouseButton> _previousMouseButtons = new();
    private static HashSet<MouseButton> _currentMouseButtons = new();

    /// <summary>
    /// Appelé une fois par frame pour mettre à jour l'état.
    /// </summary>
    public static void Update()
    {
        // L'état courant devient l'état précédent
        _previousMouseButtons = new HashSet<MouseButton>(_currentMouseButtons);
        _currentMouseButtons.Clear();

        // Enregistrer les boutons actuellement pressés
        if (Input.IsMouseButtonPressed(MouseButton.Left))
            _currentMouseButtons.Add(MouseButton.Left);
        if (Input.IsMouseButtonPressed(MouseButton.Right))
            _currentMouseButtons.Add(MouseButton.Right);
        if (Input.IsMouseButtonPressed(MouseButton.Middle))
            _currentMouseButtons.Add(MouseButton.Middle);
    }

    /// <summary>
    /// True seulement la première frame où le bouton est pressé.
    /// </summary>
    public static bool IsMouseButtonJustPressed(MouseButton button)
    {
        return _currentMouseButtons.Contains(button) && !_previousMouseButtons.Contains(button);
    }

    /// <summary>
    /// True tant que le bouton est maintenu.
    /// </summary>
    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return _currentMouseButtons.Contains(button);
    }
}