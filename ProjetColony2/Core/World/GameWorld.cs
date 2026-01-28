// ============================================================================
// GAMEWORLD.CS — Le monde entier, découpé en chunks
// ============================================================================
// Le monde est INFINI (ou presque). On ne peut pas stocker une grille infinie
// en mémoire. Solution : découper le monde en CHUNKS (morceaux) de 16×16×16.
//
// ANALOGIE : Une bibliothèque
//   MONDE = La bibliothèque entière (infinie)
//   CHUNK = Une étagère (16×16×16 livres)
//   VOXEL = Un livre
//
// Quand tu demandes "le bloc à la position (35, 10, -5)", World fait :
//   1. Calcule le chunk : (35÷16, 10÷16, -5÷16) = chunk (2, 0, -1)
//   2. Calcule la position locale : 35 - 2×16 = 3, etc. = locale (3, 10, 11)
//   3. Demande au chunk (2, 0, -1) le bloc à (3, 10, 11)
//
// POURQUOI UN DICTIONNAIRE ?
//   On ne crée que les chunks dont on a besoin.
//   Si le joueur n'a jamais visité la zone (100, 0, 200), pas de chunk là-bas.
//   C'est plus économe qu'un tableau géant plein de vide.
// ============================================================================

using System;
using System.Collections.Generic;

namespace ProjetColony2.Core.World;

public class GameWorld
{
    // ========================================================================
    // LE STOCKAGE DES CHUNKS
    // ========================================================================
    // Dictionary<clé, valeur> = structure qui associe une clé à une valeur
    //
    // Clé : (int, int, int) = un TUPLE, trois entiers groupés ensemble
    //       Représente les coordonnées du chunk : (chunkX, chunkY, chunkZ)
    //       Exemple : (0, 0, 0) = chunk à l'origine
    //                 (2, 0, -1) = chunk à droite et derrière l'origine
    //
    // Valeur : Chunk = le morceau de monde avec ses 4096 voxels
    Dictionary<(int, int, int), Chunk> _chunks;

    // Crée un monde vide (aucun chunk pour l'instant)
    public GameWorld()
    {
        _chunks = new Dictionary<(int, int, int), Chunk>();
    }

    // ========================================================================
    // GETCHUNKCOORD — Trouve dans quel chunk se trouve une position mondiale
    // ========================================================================
    // Entrée : position dans le monde (peut être n'importe quel entier)
    // Sortie : coordonnées du chunk qui contient cette position
    //
    // CALCUL : on divise par 16 (la taille du chunk)
    //   Position 35 → 35 ÷ 16 = chunk 2 (car 35 est entre 32 et 47)
    //   Position 0  → 0 ÷ 16 = chunk 0
    //   Position -1 → -1 ÷ 16 = chunk -1 (ATTENTION, pas 0 !)
    //
    // Le piège des négatifs :
    //   En C#, -1 / 16 = 0 (division entière tronque vers zéro)
    //   Mais on veut -1 (car -1 est dans le chunk -1, pas le chunk 0)
    //   C'est pour ça qu'on utilise FloorDiv au lieu de /
    public (int, int, int) GetChunkCoord(int worldX, int worldY, int worldZ)
    {
        int chunkX = FloorDiv(worldX, Chunk.Size);
        int chunkY = FloorDiv(worldY, Chunk.Size);
        int chunkZ = FloorDiv(worldZ, Chunk.Size);

        return (chunkX, chunkY, chunkZ);
    }

    // ========================================================================
    // GETLOCALCOORD — Convertit une position mondiale en position locale
    // ========================================================================
    // Entrée : position dans le monde
    // Sortie : position DANS le chunk (0 à 15 sur chaque axe)
    //
    // CALCUL : position - (chunk × 16)
    //   Monde 35, chunk 2 → 35 - (2 × 16) = 35 - 32 = 3
    //   Monde -1, chunk -1 → -1 - (-1 × 16) = -1 + 16 = 15
    //
    // SYNTAXE var (a, b, c) = méthode() :
    //   C'est le "destructuring" — on récupère les 3 valeurs du tuple
    //   en une seule ligne au lieu de faire :
    //     var result = GetChunkCoord(...);
    //     int chunkX = result.Item1;
    //     int chunkY = result.Item2;
    //     int chunkZ = result.Item3;
    public (int, int, int) GetLocalCoord(int worldX, int worldY, int worldZ)
    {
        var (chunkX, chunkY, chunkZ) = GetChunkCoord(worldX, worldY, worldZ);

        int localX = worldX - chunkX * Chunk.Size;
        int localY = worldY - chunkY * Chunk.Size;
        int localZ = worldZ - chunkZ * Chunk.Size;

        return (localX, localY, localZ);
    }

    // ========================================================================
    // GETORCREATECHUNK — Récupère un chunk, le crée s'il n'existe pas
    // ========================================================================
    // Si le joueur explore une nouvelle zone, le chunk n'existe pas encore.
    // Cette méthode le crée automatiquement.
    //
    // LOGIQUE :
    //   1. Crée la clé (tuple des coordonnées)
    //   2. Cherche dans le dictionnaire
    //   3. Si trouvé → retourne le chunk existant
    //   4. Sinon → crée un nouveau chunk vide, le stocke, le retourne
    public Chunk GetOrCreateChunk(int chunkX, int chunkY, int chunkZ)
    {
        var key = (chunkX, chunkY, chunkZ);

        if(_chunks.TryGetValue(key, out var chunk))
        {
            return chunk;
        }

        var newChunk = new Chunk();
        _chunks[key] = newChunk;

        return newChunk;
    }

    // ========================================================================
    // GETVOXEL — Récupère un voxel à une position mondiale
    // ========================================================================
    // C'est la méthode principale ! Le reste du code l'utilise.
    //
    // ÉTAPES :
    //   1. Trouve le chunk (GetChunkCoord)
    //   2. Convertit en position locale (GetLocalCoord)
    //   3. Récupère ou crée le chunk (GetOrCreateChunk)
    //   4. Demande au chunk le voxel (chunk.GetVoxel)
    public Voxel GetVoxel(int worldX, int worldY, int worldZ)
    {
        var (chunkX, chunkY, chunkZ) = GetChunkCoord(worldX, worldY, worldZ);
        var (localX, localY, localZ) = GetLocalCoord(worldX, worldY, worldZ);
        var chunk = GetOrCreateChunk(chunkX, chunkY, chunkZ);

        return chunk.GetVoxel(localX, localY, localZ);
    }

    // ========================================================================
    // SETVOXEL — Place un voxel à une position mondiale
    // ========================================================================
    // Même logique que GetVoxel, mais on ÉCRIT au lieu de LIRE.
    public void SetVoxel(int worldX, int worldY, int worldZ, Voxel voxel)
    {
        var (chunkX, chunkY, chunkZ) = GetChunkCoord(worldX, worldY, worldZ);
        var (localX, localY, localZ) = GetLocalCoord(worldX, worldY, worldZ);
        var chunk = GetOrCreateChunk(chunkX, chunkY, chunkZ);
        chunk.SetVoxel(localX, localY, localZ, voxel);
    }

    // ========================================================================
    // FLOORDIV — Division entière qui arrondit vers le BAS (pas vers zéro)
    // ========================================================================
    // En C#, la division entière arrondit vers ZÉRO :
    //   7 / 16 = 0  ✓ (correct)
    //   -1 / 16 = 0  ✗ (on voudrait -1)
    //   -17 / 16 = -1  ✗ (on voudrait -2)
    //
    // On veut arrondir vers le BAS (comme Math.Floor mais en entier) :
    //   7 / 16 = 0  ✓
    //   -1 / 16 = -1  ✓
    //   -17 / 16 = -2  ✓
    //
    // ASTUCE POUR LES NÉGATIFS :
    //   (numerator - denominator + 1) / denominator
    //   (-1 - 16 + 1) / 16 = -16 / 16 = -1  ✓
    //   (-17 - 16 + 1) / 16 = -32 / 16 = -2  ✓
    private int FloorDiv(int numerator, int denominator)
    {
        if(numerator >= 0)
        {
            return numerator / denominator;
        }
        else
        {
            return (numerator - denominator + 1) / denominator;
        }
    }
}