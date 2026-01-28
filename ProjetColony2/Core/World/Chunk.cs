// ============================================================================
// CHUNK.CS — Un morceau du monde (16×16×16 blocs)
// ============================================================================
// Le monde est trop grand pour tout stocker d'un coup. On le découpe en
// "chunks" (morceaux) de 16×16×16 = 4096 blocs chacun.
//
// POURQUOI 16 ?
//   - Assez petit pour charger/décharger rapidement
//   - Assez grand pour ne pas avoir trop de chunks à gérer
//   - 16 = 2^4, pratique pour les calculs binaires
//
// UN CHUNK CONTIENT DEUX CHOSES :
//
// 1. LES VOXELS (terrain) — Tableau 3D fixe
//    Chaque case du tableau contient UN voxel (pierre, terre, air...)
//    Accès : _voxels[x, y, z]
//    Même les cases vides (air) existent dans le tableau.
//
// 2. LES SUBCELLS (constructions) — Dictionnaire
//    Les blocs fins (poteaux, pentes, meubles) placés par le joueur.
//    Seulement les cases NON VIDES sont stockées (économie de mémoire).
//    Accès : _subCells[clé] où la clé encode la position.
//
// POURQUOI CLASS ET PAS STRUCT ?
//   Un Chunk contient beaucoup de données (4096 voxels + dictionnaire).
//   Si c'était une struct, tout serait COPIÉ à chaque fois qu'on passe
//   le chunk à une méthode. Avec une class, on passe juste une référence
//   (une "adresse" vers les données).
// ============================================================================

using System.Collections.Generic;
using ProjetColony2.Core.Shapes;

namespace ProjetColony2.Core.World;

public class Chunk
{
    // ========================================================================
    // CONSTANTE ET CHAMPS
    // ========================================================================
    // Size = 16 : un chunk fait 16 blocs de côté (16×16×16 = 4096 blocs)
    //
    // _voxels : tableau 3D pour le terrain
    //   - Le underscore "_" indique un champ privé (convention C#)
    //   - [,,] signifie tableau à 3 dimensions
    //   - Accès : _voxels[x, y, z] où x, y, z vont de 0 à 15
    //
    // _subCells : dictionnaire pour les constructions
    //   - Dictionary<clé, valeur> = structure qui associe une clé à une valeur
    //   - La clé (long) encode la position complète
    //   - La valeur (SubCell) est le bloc construit
    //   - Avantage : on ne stocke que les cases où il y a quelque chose
    public const int Size = 16;
    private Voxel[,,] _voxels;
    private Dictionary<long, SubCell> _subCells;

    // ========================================================================
    // CONSTRUCTEUR — Appelé quand on écrit "new Chunk()"
    // ========================================================================
    // Crée un chunk VIDE :
    //   - Le tableau de voxels est créé avec des Voxel par défaut (MaterialId = 0 = air)
    //   - Le dictionnaire est vide (aucune construction)
    public Chunk()
    {
        _voxels = new Voxel[Size, Size, Size];
        _subCells = new Dictionary<long, SubCell>();
    }

    // ========================================================================
    // GETVOXEL — Récupère le voxel à une position locale
    // ========================================================================
    // x, y, z = coordonnées DANS le chunk (0 à 15)
    // Retourne : le Voxel à cette position
    //
    // ATTENTION : pas de vérification des bornes !
    // Si x=20, ça plante. C'est volontaire pour la performance.
    // La vérification se fait dans World.cs avant d'appeler cette méthode.
    public Voxel GetVoxel(int x, int y, int z)
    {
        return _voxels[x, y, z];
    }

    // ========================================================================
    // SETVOXEL — Place un voxel à une position locale
    // ========================================================================
    // x, y, z = coordonnées DANS le chunk (0 à 15)
    // voxel = le nouveau voxel à placer
    public void SetVoxel(int x, int y, int z, Voxel voxel)
    {
        _voxels[x, y, z] = voxel;
    }

    // ========================================================================
    // GETSUBCELL — Récupère une construction à une position précise
    // ========================================================================
    // vx, vy, vz = position du voxel dans le chunk (0 à 15)
    // coord = position dans la sous-grille 3×3×3 du voxel (0 à 2)
    //
    // Retourne : la SubCell si elle existe, sinon SubCell.Empty
    //
    // TryGetValue expliqué :
    //   if (_subCells.TryGetValue(key, out var subCell))
    //   
    //   Cette méthode fait deux choses en une :
    //   1. Vérifie si la clé existe dans le dictionnaire
    //   2. Si oui, met la valeur dans "subCell" et retourne true
    //   3. Si non, retourne false (et subCell est vide)
    //
    //   C'est plus sûr que _subCells[key] qui PLANTE si la clé n'existe pas.
    public SubCell GetSubCell(int vx, int vy, int vz, SubCellCoord coord)
    {
        long key = GetSubCellKey(vx, vy, vz, coord);
        if(_subCells.TryGetValue(key, out var subCell))
        {
            return subCell;
        }
        return SubCell.Empty;
    }

    // ========================================================================
    // SETSUBCELL — Place une construction à une position précise
    // ========================================================================
    // vx, vy, vz = position du voxel dans le chunk (0 à 15)
    // coord = position dans la sous-grille 3×3×3 (0 à 2)
    // subCell = le bloc à placer
    //
    // NOTE : _subCells[key] = valeur
    //   - Si la clé existe → remplace la valeur
    //   - Si la clé n'existe pas → crée l'entrée
    public void SetSubCell(int vx, int vy, int vz, SubCellCoord coord, SubCell subCell)
    {
        long key = GetSubCellKey(vx, vy, vz, coord);
        _subCells[key] = subCell;
    }

    // ========================================================================
    // GETSUBCELLKEY — Crée une clé unique pour le dictionnaire
    // ========================================================================
    // On doit combiner 6 nombres (vx, vy, vz, coord.X, coord.Y, coord.Z)
    // en UN SEUL nombre (long = 64 bits) pour servir de clé.
    //
    // TECHNIQUE : BIT SHIFTING (décalage de bits)
    //
    // Un "long" c'est 64 bits. On découpe ces 64 bits en tranches :
    //
    //   Bits 0-7   (8 bits)  → vx       (0 à 255, on utilise 0-15)
    //   Bits 8-15  (8 bits)  → vy       (0 à 255, on utilise 0-15)
    //   Bits 16-23 (8 bits)  → vz       (0 à 255, on utilise 0-15)
    //   Bits 24-31 (8 bits)  → coord.X  (0 à 255, on utilise 0-2)
    //   Bits 32-39 (8 bits)  → coord.Y  (0 à 255, on utilise 0-2)
    //   Bits 40-47 (8 bits)  → coord.Z  (0 à 255, on utilise 0-2)
    //
    // OPÉRATEURS :
    //   << (shift left) = décale les bits vers la gauche
    //   |  (or) = combine les bits
    //
    // EXEMPLE avec des petits nombres :
    //   vx = 5, vy = 3
    //   
    //   vx      = 00000101 (5 en binaire)
    //   vy << 8 = 00000011 00000000 (3 décalé de 8 bits)
    //   
    //   vx | (vy << 8) = 00000011 00000101 = 773 en décimal
    //
    // Résultat : chaque position donne une clé UNIQUE.
    //   Position (5, 3, 0, 1, 1, 1) → une clé
    //   Position (5, 3, 0, 1, 1, 2) → une clé différente
    private long GetSubCellKey(int vx, int vy, int vz, SubCellCoord coord)
    {
        long key = vx | (vy << 8) | (vz << 16) | (coord.X << 24) | (coord.Y << 32) | (coord.Z << 40);
        return key;
    }
}
