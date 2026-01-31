// ============================================================================
// MININGSYSTEM.CS — Gère le minage progressif des blocs
// ============================================================================
// Ce fichier est dans CORE car il ne dépend pas de Godot.
// C'est de la pure logique de simulation.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Avant : clic = bloc cassé instantanément
// Maintenant : maintenir clic = progression → bloc cassé
//
// C'est plus réaliste et ça permet :
//   - Des matériaux plus ou moins durs
//   - Des outils plus ou moins efficaces
//   - Le même système pour joueur ET colons
//
// ============================================================================
// SYSTÈME UNIFIÉ JOUEUR/COLONS
// ============================================================================
// Le joueur maintient le clic gauche → IsMining = true chaque frame
// Un colon reçoit un ordre de travail → IsMining = true chaque tick
//
// Dans les deux cas, MiningSystem accumule la progression et casse
// le bloc quand c'est terminé. UN SEUL CODE pour tout le monde.
//
// ============================================================================
// CALCUL DU TEMPS DE MINAGE
// ============================================================================
// Formule : tempsRequis = Hardness * 1000 / MiningSpeed
//
// Exemples avec MiningSpeed = 1000 (100%) :
//   Stone (Hardness 1500) → 1500 * 1000 / 1000 = 1500ms = 1.5s
//   Dirt (Hardness 500) → 500 * 1000 / 1000 = 500ms = 0.5s
//
// Avec une pioche (MiningSpeed = 2000 = 200%) :
//   Stone → 1500 * 1000 / 2000 = 750ms = 0.75s
//
// ============================================================================
// FIXED POINT ET DÉTERMINISME
// ============================================================================
// Tout est en int (millisecondes) pour le lockstep multijoueur.
// Pas de float = pas de désync entre machines.
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Entity.cs : MiningSpeed, MiningProgress
// - IntentComponent.cs : IsMining, TargetBlockX/Y/Z
// - MaterialDefinition.cs : Hardness
// - GameManager.cs : appelle ApplyMining dans _PhysicsProcess
// ============================================================================

using System.Collections.Generic;
using ProjetColony2.Core.Data;
using ProjetColony2.Core.Entities;
using ProjetColony2.Core.World;

namespace ProjetColony2.Core.Systems;

public static class MiningSystem
{
    // ========================================================================
    // TICKDURATIONMS — Durée d'un tick en millisecondes
    // ========================================================================
    // Godot tourne à 60 FPS par défaut → 1000ms / 60 ≈ 17ms par tick.
    // C'est une approximation, mais suffisante pour le gameplay.
    public const int TickDurationMs = 17;

    // ========================================================================
    // APPLYMINING — Gère la progression du minage
    // ========================================================================
    // PARAMÈTRES :
    //   entity : l'entité qui mine
    //   world : le monde (pour vérifier le bloc)
    //   materials : liste des matériaux (pour la Hardness)
    //   out blockX/Y/Z : coordonnées du bloc cassé (si retourne true)
    //
    // RETOUR :
    //   true = un bloc a été cassé (utiliser blockX/Y/Z)
    //   false = pas encore cassé (ou pas en train de miner)
    //
    // ALGORITHME :
    //   1. Si pas en train de miner → reset et return false
    //   2. Récupérer le bloc visé
    //   3. Si c'est de l'air → reset et return false
    //   4. Calculer le temps requis selon Hardness et MiningSpeed
    //   5. Accumuler la progression
    //   6. Si progression >= temps requis → cassé ! return true
    public static bool ApplyMining(
        Entity entity, 
        GameWorld world, 
        List<MaterialDefinition> materials,
        out int blockX, out int blockY, out int blockZ)
    {
        // Valeurs par défaut pour les paramètres out
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

        // Accumuler la progression (un tick = ~17ms)
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