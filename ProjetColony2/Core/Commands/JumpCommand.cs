// ============================================================================
// JUMPCOMMAND.CS — Commande de saut
// ============================================================================
// Quand le joueur appuie sur Espace, on crée une JumpCommand.
// Cette commande indique que l'entité VEUT sauter.
//
// ============================================================================
// POURQUOI UN FICHIER DÉDIÉ ?
// ============================================================================
// 1. Convention C# : une classe = un fichier
// 2. Cohérence : MoveCommand a son fichier, JumpCommand aussi
// 3. Extensibilité : on pourra ajouter JumpForce, DoubleJump, etc.
// 4. Lockstep : chaque commande sera sérialisée pour le multijoueur
//
// ============================================================================
// DIFFÉRENCE AVEC MOVECOMMAND
// ============================================================================
// MoveCommand stocke une DIRECTION (où aller).
// JumpCommand ne stocke RIEN — c'est juste "je veux sauter".
//
// La force du saut est dans MovementSystem, pas dans la commande.
// Toutes les entités sautent de la même façon (pour l'instant).
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - ICommand.cs : l'interface qu'on implémente
// - IntentComponent.cs : on active WantsToJump
// - PlayerController.cs : crée la commande quand Espace est pressé
// - MovementSystem.cs : lit WantsToJump et applique le saut
// ============================================================================

using ProjetColony2.Core.Entities;

namespace ProjetColony2.Core.Commands;

public class JumpCommand : ICommand
{
    // Identifiant de l'entité qui veut sauter
    // Même principe que MoveCommand : savoir QUI a envoyé la commande.
    public readonly int EntityId;

    // Crée une commande de saut pour l'entité spécifiée
    public JumpCommand(int entityId)
    {
        EntityId = entityId;
    }

    // ========================================================================
    // EXECUTE — Active l'intention de sauter
    // ========================================================================
    // On ne fait que lever un FLAG (WantsToJump = true).
    // C'est MovementSystem qui fera le vrai saut.
    //
    // POURQUOI NE PAS SAUTER ICI ?
    //   La commande ne connaît pas le monde (pas de collision).
    //   Elle dit juste "je veux", le système décide "je peux".
    public void Execute(Entity entity)
    {
        entity.Intent.WantsToJump = true;
    }
}