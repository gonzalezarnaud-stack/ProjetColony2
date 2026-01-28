// ============================================================================
// ICOMMAND.CS — Interface pour toutes les commandes
// ============================================================================
// Une COMMANDE = une action demandée par le joueur.
// "Je veux bouger", "Je veux miner", "Je veux poser un bloc".
//
// POURQUOI UNE INTERFACE ?
//   Il y aura plusieurs types de commandes : MoveCommand, MineCommand, etc.
//   Toutes doivent pouvoir être "exécutées". L'interface garantit ça.
//
//   Sans interface, on aurait :
//     if (command is MoveCommand) { ... }
//     else if (command is MineCommand) { ... }  // Moche !
//
//   Avec interface :
//     command.Execute(entity);  // Marche pour toutes
//
// FLUX COMPLET :
//   1. Joueur appuie sur une touche
//   2. PlayerController crée une Command (ex: MoveCommand)
//   3. Command est ajoutée au buffer
//   4. Au tick suivant, simulation appelle command.Execute(entity)
//   5. Execute() remplit entity.Intent
//   6. La simulation traite l'Intent
//
// POURQUOI PAS MODIFIER L'INTENT DIRECTEMENT ?
//   Pour le LOCKSTEP multijoueur. Les Commands sont synchronisées entre
//   tous les joueurs AVANT d'être exécutées. Tout le monde exécute les
//   mêmes Commands au même tick = même résultat = pas de désynchronisation.
// ============================================================================

using ProjetColony2.Core.Entities;

namespace ProjetColony2.Core.Commands;

public interface ICommand
{
    // ========================================================================
    // EXECUTE — Applique la commande sur une entité
    // ========================================================================
    // Appelée par la simulation quand c'est le moment d'exécuter.
    // Remplit l'Intent de l'entité avec ce que la commande demande.
    //
    // Exemple pour MoveCommand :
    //   entity.Intent.MoveX = this.DirectionX;
    //   entity.Intent.MoveZ = this.DirectionZ;
    void Execute(Entity entity);
}