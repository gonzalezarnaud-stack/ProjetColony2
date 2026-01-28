// ============================================================================
// INTENTCOMPONENT.CS — Ce qu'une entité VEUT faire ce tick
// ============================================================================
// Une intention = un SOUHAIT, pas une action exécutée.
// L'entité dit "je veux aller à gauche" ou "je veux miner".
// C'est la simulation qui décide si c'est possible et l'exécute.
//
// POURQUOI SÉPARER INTENTION ET ACTION ?
//   - Un colon VEUT miner, mais il y a un mur → il ne peut pas
//   - Le joueur VEUT avancer, mais il y a un trou → il tombe
//   L'intention est le désir. La simulation applique les règles.
//
// QUI REMPLIT LES INTENTIONS ?
//   - Joueur : une Command (venant des inputs) remplit l'Intent
//   - Colon : le JobSystem ou le Brain remplit l'Intent
//   Dans les deux cas, la simulation traite l'Intent pareil.
//
// CYCLE DE VIE (chaque tick) :
//   1. Command ou Brain remplit l'Intent
//   2. La simulation lit l'Intent et agit
//   3. Clear() remet tout à zéro pour le prochain tick
//
// FIXED POINT (×1000) :
//   MoveX/Y/Z utilisent des entiers au lieu de floats.
//   -1000 = direction négative, vitesse max
//   0 = immobile
//   +1000 = direction positive, vitesse max
//   Ça permet des valeurs intermédiaires (joystick, sprint...).
// ============================================================================

namespace ProjetColony2.Core.Entities;

public class IntentComponent
{
    // ========================================================================
    // MOUVEMENT — Direction et vitesse souhaitées
    // ========================================================================
    // Valeurs de -1000 à +1000 (fixed point)
    //   MoveX : -1000 = gauche, +1000 = droite
    //   MoveY : -1000 = bas, +1000 = haut
    //   MoveZ : -1000 = arrière (-Z dans Godot), +1000 = avant (+Z)
    //
    // Exemples :
    //   Avancer tout droit : MoveZ = -1000 (vers -Z)
    //   Diagonale gauche-avant : MoveX = -1000, MoveZ = -1000
    //   Demi-vitesse droite : MoveX = 500
    public int MoveX;
    public int MoveY;
    public int MoveZ;

    // ========================================================================
    // ACTIONS — Ce que l'entité veut faire
    // ========================================================================
    // WantsToMine : veut casser un bloc (clic gauche)
    // WantsToPlace : veut poser un bloc (clic droit)
    //
    // Plus tard on ajoutera : WantsToAttack, WantsToInteract, etc.
    public bool WantsToMine;
    public bool WantsToPlace;
    public bool WantsToJump;

    // ========================================================================
    // BLOC CIBLÉ — Coordonnées du bloc à miner ou à côté duquel poser
    // ========================================================================
    // Rempli par MineCommand ou PlaceCommand.
    // En coordonnées bloc (int), pas en fixed point.
    public int TargetBlockX;
    public int TargetBlockY;
    public int TargetBlockZ;

    // Matériau à poser (0 = aucun, 1 = pierre, 2 = terre...)
    public byte PlaceMaterialId;

    // ========================================================================
    // CLEAR — Remet toutes les intentions à zéro
    // ========================================================================
    // Appelée à la FIN de chaque tick, après que la simulation ait traité.
    //
    // POURQUOI ?
    // Les intentions sont valables pour UN SEUL tick.
    // Si le joueur appuie sur Z, on reçoit une Command à chaque tick.
    // Si le joueur relâche Z, on ne reçoit plus de Command.
    // Sans Clear(), le personnage continuerait à avancer tout seul !
    //
    // "void" = cette méthode ne retourne rien, elle fait juste son travail.
    public void Clear()
    {
        MoveX = 0;
        MoveY = 0;
        MoveZ = 0;
        WantsToMine = false;
        WantsToPlace = false;
        WantsToJump = false;
        TargetBlockX = 0;
        TargetBlockY = 0;
        TargetBlockZ = 0;
        PlaceMaterialId = 0;
    }
}