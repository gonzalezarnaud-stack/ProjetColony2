// ============================================================================
// MININGCOMMAND.CS — Commande "je suis en train de miner ce bloc"
// ============================================================================
// Ce fichier est dans CORE car il ne dépend pas de Godot.
//
// ============================================================================
// DIFFÉRENCE AVEC MINECOMMAND
// ============================================================================
// MineCommand (ancien) : "casse ce bloc maintenant" → instantané
// MiningCommand (nouveau) : "je mine ce bloc" → progressif
//
// MiningCommand est envoyée CHAQUE FRAME tant que le joueur maintient
// le clic. Elle met IsMining = true et enregistre la cible.
//
// Quand le joueur relâche, plus de MiningCommand → Intent.Clear() →
// IsMining = false → MiningSystem reset la progression.
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - PlayerController.cs : crée MiningCommand quand clic gauche maintenu
// - MiningSystem.cs : lit IsMining et TargetBlock pour progresser
// - IntentComponent.cs : stocke IsMining et TargetBlockX/Y/Z
// ============================================================================

using ProjetColony2.Core.Entities;

namespace ProjetColony2.Core.Commands;

public class MiningCommand : ICommand
{
    // Identifiant de l'entité qui mine
    public int EntityId { get; }

    // Coordonnées du bloc à miner
    public int TargetX { get; }
    public int TargetY { get; }
    public int TargetZ { get; }

    // ========================================================================
    // CONSTRUCTEUR — Crée une commande de minage
    // ========================================================================
    public MiningCommand(int entityId, int targetX, int targetY, int targetZ)
    {
        EntityId = entityId;
        TargetX = targetX;
        TargetY = targetY;
        TargetZ = targetZ;
    }

    // ========================================================================
    // EXECUTE — Remplit l'Intent de l'entité
    // ========================================================================
    // Met IsMining = true et enregistre les coordonnées cibles.
    // MiningSystem utilisera ces infos pour faire progresser le minage.
    public void Execute(Entity entity)
    {
        entity.Intent.IsMining = true;
        entity.Intent.TargetBlockX = TargetX;
        entity.Intent.TargetBlockY = TargetY;
        entity.Intent.TargetBlockZ = TargetZ;
    }
}