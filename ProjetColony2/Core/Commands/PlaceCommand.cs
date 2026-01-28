// ============================================================================
// PLACECOMMAND.CS — Commande pour poser un bloc
// ============================================================================
// Quand le joueur clique droit, on crée une PlaceCommand.
// La commande stocke les coordonnées OÙ poser et QUEL matériau.
//
// ============================================================================
// POURQUOI MATERIALID ?
// ============================================================================
// Le joueur a un inventaire (futur) avec différents matériaux.
// La commande dit "pose de la pierre" ou "pose du bois".
// Pour l'instant, on utilisera toujours 1 (pierre).
//
// ============================================================================
// OÙ POSER ?
// ============================================================================
// Le raycast retourne "hit" (bloc touché) et "previous" (bloc avant).
// On pose dans "previous" — c'est le bloc d'AIR juste avant le bloc solide.
//
//     Air → Air → [previous] → [hit]
//                     ↑
//               On pose ici
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - RaycastSystem.cs : trouve "previous" (où poser)
// - PlayerController.cs : crée la commande au clic droit
// - IntentComponent.cs : reçoit WantsToPlace, coordonnées et matériau
// - GameManager.cs : applique le placement (crée le voxel)
// ============================================================================

using ProjetColony2.Core.Entities;

namespace ProjetColony2.Core.Commands;

public class PlaceCommand : ICommand
{
    // Identifiant de l'entité qui veut poser
    // Coordonnées où poser le bloc (en blocs)
    // Matériau à placer (1 = pierre, 2 = terre, etc.)
    public readonly int EntityId;
    public readonly int BlockX;
    public readonly int BlockY;
    public readonly int BlockZ;
    public readonly byte MaterialId;

    public PlaceCommand(int entityId, int blockX, int blockY, int blockZ, byte materialId)
    {
        EntityId = entityId;
        BlockX = blockX;
        BlockY = blockY;
        BlockZ = blockZ;
        MaterialId = materialId;
    }

    // Active l'intention de poser et transmet les coordonnées et le matériau
    public void Execute(Entity entity)
    {
        entity.Intent.WantsToPlace = true;
        entity.Intent.TargetBlockX = BlockX;
        entity.Intent.TargetBlockY = BlockY;
        entity.Intent.TargetBlockZ = BlockZ;
        entity.Intent.PlaceMaterialId = MaterialId;
    }
}