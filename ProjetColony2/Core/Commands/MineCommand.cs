// ============================================================================
// MINECOMMAND.CS — Commande pour casser un bloc
// ============================================================================
// Quand le joueur clique gauche sur un bloc, on crée une MineCommand.
// La commande stocke les coordonnées du bloc à casser.
//
// ============================================================================
// POURQUOI STOCKER LES COORDONNÉES ?
// ============================================================================
// En lockstep, la commande est créée au tick N, exécutée au tick N+1.
// Entre-temps, le joueur a peut-être bougé ou regardé ailleurs.
// La commande "se souvient" de quel bloc était visé.
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - RaycastSystem.cs : trouve les coordonnées du bloc visé
// - PlayerController.cs : crée la commande au clic gauche
// - IntentComponent.cs : reçoit WantsToMine et les coordonnées cible
// - GameManager.cs : applique le minage (supprime le voxel)
// ============================================================================

using ProjetColony2.Core.Entities;

namespace ProjetColony2.Core.Commands;

public class MineCommand : ICommand
{
    // Identifiant de l'entité qui veut miner
    // Coordonnées du bloc ciblé (en blocs, pas en fixed point)
    public readonly int EntityId;
    public readonly int BlockX;
    public readonly int BlockY;
    public readonly int BlockZ;

    public MineCommand(int entityId, int blockX, int blockY, int blockZ)
    {
        EntityId = entityId;
        BlockX = blockX;
        BlockY = blockY;
        BlockZ = blockZ;
    }

    // Active l'intention de miner et transmet les coordonnées cibles
    public void Execute(Entity entity)
    {
        entity.Intent.WantsToMine = true;
        entity.Intent.TargetBlockX = BlockX;
        entity.Intent.TargetBlockY = BlockY;
        entity.Intent.TargetBlockZ = BlockZ;
    }
}