// ============================================================================
// MOVECOMMAND.CS — Commande de déplacement
// ============================================================================
// Quand le joueur appuie sur Z/Q/S/D, on crée une MoveCommand.
// Cette commande STOCKE la direction, puis l'APPLIQUE à l'Intent au bon moment.
//
// POURQUOI STOCKER LA DIRECTION ?
//   En lockstep, la commande est créée au tick N, mais exécutée au tick N+2.
//   Entre-temps, elle est envoyée aux autres joueurs pour synchronisation.
//   Elle doit "se souvenir" de ce que le joueur voulait faire.
//
// POURQUOI ENTITYID ?
//   En multijoueur, plusieurs joueurs envoient des commandes.
//   Joueur 1 appuie sur Z → MoveCommand(entityId=1, ...)
//   Joueur 2 appuie sur D → MoveCommand(entityId=2, ...)
//   La simulation sait ainsi QUI veut bouger.
//
//   Même en solo, on garde EntityId pour :
//   - Préparer le multijoueur
//   - Permettre au joueur de contrôler plusieurs entités (futur)
//
// READONLY :
//   Les champs sont "readonly" car une commande ne change jamais après création.
//   C'est une photo instantanée de ce que le joueur voulait à ce moment.
//
// VALEURS DE DIRECTION (fixed point ×1000) :
//   DirectionX = -1000 : gauche
//   DirectionX = +1000 : droite
//   DirectionZ = -1000 : avancer (vers -Z dans Godot)
//   DirectionZ = +1000 : reculer (vers +Z)
// ============================================================================

using ProjetColony2.Core.Entities;

namespace ProjetColony2.Core.Commands;

public class MoveCommand : ICommand
{
// ========================================================================
    // QUI EST CONCERNÉ ?
    // ========================================================================
    // L'identifiant de l'entité qui veut bouger.
    // En multijoueur, chaque joueur contrôle une entité différente.
    // La simulation utilise cet Id pour savoir à qui appliquer la commande.
    public readonly int EntityId;    
    
    // ========================================================================
    // DIRECTION — Où le joueur veut aller
    // ========================================================================
    // Stocké en fixed point (×1000).
    //   -1000 = direction négative, vitesse max
    //   0 = immobile sur cet axe
    //   +1000 = direction positive, vitesse max
    //
    // "readonly" = ne peut pas être modifié après le constructeur.
    public readonly int DirectionX;
    public readonly int DirectionZ;

    // ========================================================================
    // CONSTRUCTEUR — Crée une commande de déplacement
    // ========================================================================
    // Appelé quand on écrit : new MoveCommand(1, -1000, 0)
    // → L'entité 1 veut aller à gauche
    public MoveCommand(int entityId, int directionX, int directionZ)
    {
        EntityId = entityId;
        DirectionX = directionX;
        DirectionZ = directionZ;
    }

    // ========================================================================
    // EXECUTE — Applique la commande à l'entité
    // ========================================================================
    // Transfère la direction stockée vers l'Intent de l'entité.
    // La simulation lira ensuite l'Intent pour déplacer l'entité.
    //
    // NOTE : Pour l'instant, on ignore EntityId car on passe l'entité
    // directement en paramètre. En multijoueur, on cherchera l'entité
    // dans le World via son Id.
    public void Execute(Entity entity)
    {
        entity.Intent.MoveX = DirectionX;
        entity.Intent.MoveZ = DirectionZ;
    }
}