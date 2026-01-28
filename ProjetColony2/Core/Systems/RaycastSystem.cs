// ============================================================================
// RAYCASTSYSTEM.CS — Lance un rayon pour trouver quel bloc on vise
// ============================================================================
// Ce fichier est dans CORE car il ne dépend pas de Godot.
// C'est de la pure logique de simulation.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Le joueur regarde un bloc et clique. Mais QUEL bloc ?
// Le raycast répond à cette question en simulant un rayon invisible
// depuis les yeux du joueur dans la direction du regard.
//
// UTILISATIONS :
//   - Casser un bloc (clic gauche)
//   - Poser un bloc (clic droit)
//   - Interagir avec un objet (futur)
//   - Attaquer une entité (futur)
//
// ============================================================================
// L'ALGORITHME DDA (Digital Differential Analyzer)
// ============================================================================
// Le rayon traverse le monde bloc par bloc, en suivant les FRONTIÈRES.
//
// VISUALISATION (vue de côté) :
//
//     │   │   │   │
//   ──┼───┼───┼───┼──
//     │ ● → → │ → │ ■     ● = départ (yeux)
//   ──┼───┼───┼───┼──     ■ = bloc solide trouvé
//     │   │   │   │       → = trajet du rayon
//
// POURQUOI PAS UN PAS FIXE ?
//   Un pas fixe (ex: 0.1 bloc) pourrait "sauter" par-dessus un bloc fin.
//   DDA suit CHAQUE frontière, jamais de saut, 100% précis.
//
// PRINCIPE :
//   1. On calcule la distance jusqu'à la prochaine frontière X, Y, Z
//   2. On avance jusqu'à la plus proche
//   3. On teste le bloc qu'on vient d'entrer
//   4. Si solide → trouvé ! Sinon → on continue
//
// ============================================================================
// CONCEPTS : LES VARIABLES DU DDA
// ============================================================================
// stepX/Y/Z :
//   Direction du pas sur chaque axe (+1 ou -1).
//   Si on regarde vers +X, stepX = +1.
//   Si on regarde vers -X, stepX = -1.
//
// tDeltaX/Y/Z :
//   Distance (en "t") pour traverser un bloc ENTIER sur cet axe.
//   Si dirX = 0.5, il faut t = 2 pour avancer de 1 bloc en X.
//   Formule : tDelta = |1 / direction|
//
// tMaxX/Y/Z :
//   Distance (en "t") jusqu'à la PROCHAINE frontière sur cet axe.
//   On compare tMaxX, tMaxY, tMaxZ pour savoir quelle frontière est la plus proche.
//   Après chaque pas, on ajoute tDelta au tMax correspondant.
//
// ============================================================================
// POURQUOI RETOURNER "previous" ?
// ============================================================================
// Pour POSER un bloc, on le place dans l'air AVANT le bloc touché.
//
//     Air → Air → Air → Pierre
//                  ↑       ↑
//             previous    hit
//
//   Casser = détruire "hit"
//   Poser = placer dans "previous"
//
// ============================================================================
// POURQUOI RETOURNER "hitPoint" ?
// ============================================================================
// Pour la sous-grille 5×5×5 (MVP-B), on aura besoin du point EXACT d'impact.
//
//   hitPoint = (2.3, 1.0, 5.7)
//   localX = 2.3 - 2 = 0.3 (30% dans le bloc)
//   subCellX = (int)(0.3 * 5) = 1
//
// On prépare l'architecture pour le futur.
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - GameWorld.cs : on interroge GetVoxel pour tester chaque bloc
// - PlayerController.cs : appelle le raycast pour savoir quoi casser/poser
// - (Futur) CrosshairRenderer : affichera quel bloc est visé
// ============================================================================

using System;
using ProjetColony2.Core.World;

namespace ProjetColony2.Core.Systems;

public static class RaycastSystem
{
    // Portée maximale du raycast en blocs (5 blocs = portée Minecraft)
    public const float MaxDistance = 5f;
    
    // ========================================================================
    // TRYGETTARGETBLOCK — Lance un rayon et trouve le premier bloc solide
    // ========================================================================
    // PARAMÈTRES :
    //   world : le monde à interroger
    //   startX/Y/Z : position de départ du rayon (yeux du joueur)
    //   dirX/Y/Z : direction du regard (normalisée)
    //
    // SORTIES (out) :
    //   hitBlockX/Y/Z : coordonnées du bloc touché
    //   previousX/Y/Z : coordonnées du bloc juste avant (pour poser)
    //   hitPointX/Y/Z : point exact d'impact (pour sous-grille futur)
    //
    // RETOUR :
    //   true = un bloc solide a été trouvé
    //   false = rien dans la portée (on vise le vide)
    //
    // CONCEPT "out" EN C# :
    //   "out" permet à une méthode de retourner PLUSIEURS valeurs.
    //   L'appelant doit fournir des variables, la méthode les remplit.
    //   Exemple : TryGetTargetBlock(..., out int x, out int y, out int z)
    public static bool TryGetTargetBlock(
        GameWorld world,
        float startX, float startY, float startZ,
        float dirX, float dirY, float dirZ,
        out int hitBlockX, out int hitBlockY, out int hitBlockZ,
        out int previousX, out int previousY, out int previousZ,
        out float hitPointX, out float hitPointY, out float hitPointZ)
    {
        // ====================================================================
        // INITIALISATION DES SORTIES
        // ====================================================================
        // En C#, les paramètres "out" DOIVENT être assignés avant de quitter.
        // On les initialise à 0 pour éviter les erreurs de compilation.
        hitBlockX = hitBlockY = hitBlockZ = 0;
        previousX = previousY = previousZ = 0;
        hitPointX = hitPointY = hitPointZ = 0f;
        
        // ====================================================================
        // BLOC DE DÉPART
        // ====================================================================
        // Math.Floor arrondit vers le bas (vers -∞).
        //   2.7 → 2
        //   -0.3 → -1 (pas 0 !)
        //
        // C'est important pour les coordonnées négatives.
        int blockX = (int)Math.Floor(startX);
        int blockY = (int)Math.Floor(startY);
        int blockZ = (int)Math.Floor(startZ);
        
        // ====================================================================
        // DIRECTION DU PAS
        // ====================================================================
        // Sur chaque axe, on avance de +1 ou -1 selon la direction du rayon.
        //
        // Si dirX > 0 (on regarde vers +X) → stepX = +1
        // Si dirX < 0 (on regarde vers -X) → stepX = -1
        // Si dirX = 0 (parallèle à l'axe) → stepX = +1 (peu importe, on n'avancera jamais)
        int stepX = dirX >= 0 ? 1 : -1;
        int stepY = dirY >= 0 ? 1 : -1;
        int stepZ = dirZ >= 0 ? 1 : -1;
        
        // ====================================================================
        // DISTANCE POUR TRAVERSER UN BLOC (tDelta)
        // ====================================================================
        // "t" est une mesure de distance le long du rayon.
        // tDelta = combien de "t" pour traverser UN bloc entier sur cet axe.
        //
        // FORMULE : tDelta = |1 / direction|
        //
        // EXEMPLE :
        //   dirX = 0.5 (rayon incliné à 45°)
        //   tDeltaX = |1 / 0.5| = 2
        //   → Il faut t = 2 pour avancer de 1 bloc en X
        //
        //   dirX = 1.0 (rayon droit vers X)
        //   tDeltaX = |1 / 1| = 1
        //   → Il faut t = 1 pour avancer de 1 bloc en X
        //
        // Si direction = 0, on met MaxValue (infini).
        // Le rayon est parallèle à cet axe, il ne croisera jamais de frontière.
        float tDeltaX = dirX != 0 ? Math.Abs(1f / dirX) : float.MaxValue;
        float tDeltaY = dirY != 0 ? Math.Abs(1f / dirY) : float.MaxValue;
        float tDeltaZ = dirZ != 0 ? Math.Abs(1f / dirZ) : float.MaxValue;
        
        // ====================================================================
        // DISTANCE JUSQU'À LA PROCHAINE FRONTIÈRE (tMax)
        // ====================================================================
        // tMax = combien de "t" pour atteindre la PROCHAINE frontière de bloc.
        //
        // VISUALISATION :
        //        frontière
        //            ↓
        //   ─────────┼─────────
        //        ●───→
        //        ↑
        //      start
        //
        //   tMax = distance de ● jusqu'à la frontière
        //
        // CALCUL (si stepX > 0, on va vers la droite) :
        //   Prochaine frontière = blockX + 1
        //   Distance horizontale = (blockX + 1) - startX
        //   tMaxX = distance horizontale / |dirX|
        //
        // CALCUL (si stepX < 0, on va vers la gauche) :
        //   Prochaine frontière = blockX (le bord gauche du bloc actuel)
        //   Distance horizontale = startX - blockX
        //   tMaxX = distance horizontale / |dirX|
        float tMaxX = dirX != 0 ? ((stepX > 0 ? blockX + 1 - startX : startX - blockX) / Math.Abs(dirX)) : float.MaxValue;
        float tMaxY = dirY != 0 ? ((stepY > 0 ? blockY + 1 - startY : startY - blockY) / Math.Abs(dirY)) : float.MaxValue;
        float tMaxZ = dirZ != 0 ? ((stepZ > 0 ? blockZ + 1 - startZ : startZ - blockZ) / Math.Abs(dirZ)) : float.MaxValue;
        
        // Distance parcourue le long du rayon
        float distance = 0f;
        
        // ====================================================================
        // BOUCLE PRINCIPALE — Avancer de bloc en bloc
        // ====================================================================
        while (distance < MaxDistance)
        {
            // ================================================================
            // MÉMORISER LE BLOC PRÉCÉDENT
            // ================================================================
            // Avant d'avancer, on garde la position actuelle.
            // Si le prochain bloc est solide, "previous" sera le bloc d'AIR
            // juste avant — c'est là qu'on posera un nouveau bloc.
            previousX = blockX;
            previousY = blockY;
            previousZ = blockZ;
            
            // ================================================================
            // AVANCER VERS LA FRONTIÈRE LA PLUS PROCHE
            // ================================================================
            // On compare tMaxX, tMaxY, tMaxZ.
            // La plus petite valeur = la frontière la plus proche.
            //
            // EXEMPLE :
            //   tMaxX = 0.3, tMaxY = 0.8, tMaxZ = 0.5
            //   → La frontière X est la plus proche
            //   → On avance en X (blockX += stepX)
            //   → On met à jour tMaxX pour la PROCHAINE frontière X
            //
            // Après chaque pas, on ajoute tDelta au tMax correspondant.
            // C'est comme dire "OK, j'ai passé cette frontière, la prochaine
            // est à tDelta de plus".
            if (tMaxX < tMaxY && tMaxX < tMaxZ)
            {
                distance = tMaxX;
                tMaxX += tDeltaX;
                blockX += stepX;
            }
            else if (tMaxY < tMaxZ)
            {
                distance = tMaxY;
                tMaxY += tDeltaY;
                blockY += stepY;
            }
            else
            {
                distance = tMaxZ;
                tMaxZ += tDeltaZ;
                blockZ += stepZ;
            }
            
            // ================================================================
            // TESTER LE BLOC
            // ================================================================
            // On vient d'entrer dans un nouveau bloc. Est-il solide ?
            Voxel voxel = world.GetVoxel(blockX, blockY, blockZ);
            if (!voxel.IsAir)
            {
                hitBlockX = blockX;
                hitBlockY = blockY;
                hitBlockZ = blockZ;
                
                // ============================================================
                // POINT EXACT D'IMPACT
                // ============================================================
                // On calcule où exactement le rayon touche le bloc.
                // Formule : point = départ + direction × distance
                //
                // Utile pour la sous-grille 5×5×5 (MVP-B) :
                //   On saura précisément où dans le bloc l'impact a eu lieu.
                hitPointX = startX + dirX * distance;
                hitPointY = startY + dirY * distance;
                hitPointZ = startZ + dirZ * distance;
                
                return true;
            }
        }
        
        // Aucun bloc solide trouvé dans la portée
        return false;
    }
}