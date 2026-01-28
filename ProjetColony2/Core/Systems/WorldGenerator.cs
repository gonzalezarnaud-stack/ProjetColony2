using System;
using ProjetColony2.Core.World;

namespace ProjetColony2.Core.Systems;

// ============================================================================
// WORLDGENERATOR.CS — Génère le terrain procéduralement
// ============================================================================
// Ce fichier crée des collines, vallées, et (futur) des grottes.
// On utilise le bruit Simplex pour avoir un terrain naturel.
//
// ============================================================================
// QU'EST-CE QUE LE BRUIT SIMPLEX ?
// ============================================================================
// C'est une fonction mathématique qui retourne une valeur "aléatoire"
// mais COHÉRENTE : deux points proches ont des valeurs proches.
//
// EXEMPLE :
//   Point (0, 0) → bruit = 0.5
//   Point (1, 0) → bruit = 0.52 (proche !)
//   Point (100, 0) → bruit = 0.8 (différent)
//
// Ça crée des "vagues" naturelles, parfait pour des collines.
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// 1. Pour chaque colonne (x, z) du chunk
// 2. On calcule la position MONDIALE (pas locale au chunk)
// 3. On demande au bruit : "quelle hauteur ici ?"
// 4. On remplit de pierre du sol jusqu'à cette hauteur
//
// ============================================================================
// SEED ET DÉTERMINISME
// ============================================================================
// La "seed" est un nombre qui initialise le générateur.
// Même seed = même monde, toujours.
// C'est crucial pour :
//   - Sauvegardes (on stocke juste la seed)
//   - Multijoueur (tous les joueurs voient le même monde)
//
// ============================================================================
public static class WorldGenerator
{
    // ========================================================================
    // CONSTANTES — Paramètres de génération
    // ========================================================================
    
    // Seed du monde (futur : passée en paramètre ou config)
    public const int Seed = 12345;
    
    // Hauteur minimum du terrain (jamais en-dessous)
    public const int MinHeight = 2;
    
    // Hauteur maximum du terrain (jamais au-dessus)
    public const int MaxHeight = 14;
    
    // Échelle du bruit (plus petit = collines plus larges)
    // 0.02 = collines d'environ 50 blocs de large
    // 0.05 = collines d'environ 20 blocs de large
    public const float NoiseScale = 0.03f;

    // ========================================================================
    // GENERATECHUNK — Remplit un chunk avec du terrain
    // ========================================================================
    // PARAMÈTRES :
    //   chunk : le chunk à remplir
    //   chunkX, chunkY, chunkZ : position du chunk dans le monde
    //
    // EXEMPLE :
    //   Chunk (0, 0, 0) contient les blocs de (0,0,0) à (15,15,15)
    //   Chunk (1, 0, 0) contient les blocs de (16,0,0) à (31,15,15)
    //
    public static void GenerateChunk(Chunk chunk, int chunkX, int chunkY, int chunkZ)
    {
        // Initialiser la seed (toujours la même = monde identique)
        SimplexNoise.Noise.Seed = Seed;
        
        // Parcourir chaque colonne (x, z) du chunk
        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int z = 0; z < Chunk.Size; z++)
            {
                // ============================================================
                // POSITION MONDIALE
                // ============================================================
                // Le chunk (1, 0, 2) avec x=5 donne worldX = 1*16 + 5 = 21
                // C'est important car le bruit doit être cohérent entre chunks.
                int worldX = chunkX * Chunk.Size + x;
                int worldZ = chunkZ * Chunk.Size + z;
                
                // ============================================================
                // OBTENIR LA HAUTEUR
                // ============================================================
                int height = GetHeight(worldX, worldZ);
                
                // ============================================================
                // REMPLIR LA COLONNE
                // ============================================================
                // De y=0 jusqu'à height : mettre de la pierre
                // Au-dessus : laisser l'air (c'est le défaut)
                for (int y = 0; y < Chunk.Size; y++)
                {
                    // Position mondiale en Y
                    int worldY = chunkY * Chunk.Size + y;
                    
                    if (worldY <= height)
                    {
                        // Sous ou au niveau du sol = pierre
                        chunk.SetVoxel(x, y, z, new Voxel(1));
                    }
                    // Sinon = air (déjà le défaut, on ne fait rien)
                }
            }
        }
    }

    // ========================================================================
    // GETHEIGHT — Calcule la hauteur du terrain à une position
    // ========================================================================
    // PARAMÈTRES :
    //   worldX, worldZ : position mondiale (pas locale au chunk)
    //
    // RETOUR :
    //   Hauteur du terrain (entre MinHeight et MaxHeight)
    //
    // COMMENT ÇA MARCHE :
    //   1. On demande une valeur de bruit (0 à 255)
    //   2. On normalise en 0.0 à 1.0
    //   3. On convertit en hauteur (MinHeight à MaxHeight)
    //
    private static int GetHeight(int worldX, int worldZ)
    {
        // Obtenir le bruit Simplex (retourne 0.0 à 255.0)
        float noise = SimplexNoise.Noise.CalcPixel2D(worldX, worldZ, NoiseScale);
        
        // Normaliser entre 0.0 et 1.0
        float normalized = noise / 255f;
        
        // Convertir en hauteur
        // normalized = 0.0 → MinHeight
        // normalized = 1.0 → MaxHeight
        int height = MinHeight + (int)(normalized * (MaxHeight - MinHeight));
        
        return height;
    }
}