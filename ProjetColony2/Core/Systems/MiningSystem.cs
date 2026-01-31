using System.Collections.Generic;
using ProjetColony2.Core.Data;
using ProjetColony2.Core.Entities;
using ProjetColony2.Core.World;

namespace ProjetColony2.Core.Systems;

public static class MiningSystem
{
    // Durée d'un tick en millisecondes (~60 FPS)
    public const int TickDurationMs = 17;

    /// <summary>
    /// Gère la progression du minage.
    /// Retourne true si un bloc a été cassé.
    /// </summary>
    public static bool ApplyMining(
        Entity entity, 
        GameWorld world, 
        List<MaterialDefinition> materials,
        out int blockX, out int blockY, out int blockZ)
    {
        blockX = 0;
        blockY = 0;
        blockZ = 0;

        // Pas en train de miner → reset progression
        if (!entity.Intent.IsMining)
        {
            entity.MiningProgress = 0;
            return false;
        }

        // Récupérer le bloc visé
        int x = entity.Intent.TargetBlockX;
        int y = entity.Intent.TargetBlockY;
        int z = entity.Intent.TargetBlockZ;

        // Vérifier que c'est un bloc solide
        Voxel voxel = world.GetVoxel(x, y, z);
        if (voxel.IsAir)
        {
            entity.MiningProgress = 0;
            return false;
        }

        // Récupérer la dureté
        int hardness = materials[voxel.MaterialId].Hardness;

        // Temps requis = Hardness * 1000 / MiningSpeed
        // Exemple : 1500 * 1000 / 1000 = 1500ms
        int timeRequired = hardness * 1000 / entity.MiningSpeed;

        // Accumuler la progression
        entity.MiningProgress += TickDurationMs;

        // Bloc cassé ?
        if (entity.MiningProgress >= timeRequired)
        {
            entity.MiningProgress = 0;
            blockX = x;
            blockY = y;
            blockZ = z;
            return true;
        }

        return false;
    }
}