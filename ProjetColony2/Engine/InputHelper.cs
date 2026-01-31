// ============================================================================
// INPUTHELPER.CS — Utilitaire pour détecter les clics souris "JustPressed"
// ============================================================================
// Ce fichier est dans ENGINE car il utilise Godot (Input).
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Godot 4 a Input.IsActionJustPressed() pour le clavier, mais PAS pour
// les boutons souris individuels (IsMouseButtonJustPressed n'existe pas).
//
// Ce helper comble le manque en trackant l'état précédent des boutons.
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// Chaque frame, on appelle Update() qui :
//   1. Copie l'état courant → état précédent
//   2. Lit les boutons actuellement pressés → état courant
//
// Ensuite, IsMouseButtonJustPressed compare :
//   - Bouton pressé MAINTENANT ? (état courant)
//   - Bouton pressé AVANT ? (état précédent)
//   - Si pressé maintenant ET PAS avant → JustPressed !
//
// ============================================================================
// UTILISATION
// ============================================================================
// Au début de _Process :
//   InputHelper.Update();
//
// Pour vérifier un clic unique :
//   if (InputHelper.IsMouseButtonJustPressed(MouseButton.Right))
//
// Pour vérifier un maintien :
//   if (InputHelper.IsMouseButtonPressed(MouseButton.Left))
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - PlayerController.cs : appelle Update() et utilise les méthodes
// ============================================================================

using System.Collections.Generic;
using Godot;

namespace ProjetColony2.Engine;

public static class InputHelper
{
    // État des boutons à la frame PRÉCÉDENTE
    private static HashSet<MouseButton> _previousMouseButtons = new();

    // État des boutons à la frame COURANTE
    private static HashSet<MouseButton> _currentMouseButtons = new();

    // ========================================================================
    // UPDATE — Met à jour l'état des boutons (appeler une fois par frame)
    // ========================================================================
    // IMPORTANT : Doit être appelé AU DÉBUT de _Process, AVANT de lire
    // les boutons. Sinon l'état sera décalé d'une frame.
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

    // ========================================================================
    // ISMOUSEBUTTONJUSTPRESSED — Vrai la première frame du clic uniquement
    // ========================================================================
    // Utile pour : placement de bloc, ouverture de menu, interaction unique
    public static bool IsMouseButtonJustPressed(MouseButton button)
    {
        return _currentMouseButtons.Contains(button) && !_previousMouseButtons.Contains(button);
    }

    // ========================================================================
    // ISMOUSEBUTTONPRESSED — Vrai tant que le bouton est maintenu
    // ========================================================================
    // Utile pour : minage, tir automatique, drag & drop
    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return _currentMouseButtons.Contains(button);
    }
}