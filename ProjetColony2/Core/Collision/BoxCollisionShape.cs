// ============================================================================
// BOXCOLLISIONSHAPE.CS — Collision pour les formes rectangulaires
// ============================================================================
// Une "boîte" est la forme de collision la plus simple : un rectangle 3D.
// Elle couvre la majorité des blocs : cube plein, demi-bloc, poteau, dalle...
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// On a besoin de calculer :
//   - La hauteur de la surface (pour que le joueur marche dessus)
//   - Si un point est dans la forme (pour les collisions)
//
// Pour une boîte, c'est simple :
//   - Hauteur = SizeY partout (surface plate)
//   - ContainsPoint = vérifier si x, y, z sont dans les limites
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// On définit la taille de la boîte en unités fixed point (0 à 1000) :
//
//   Cube plein : (1000, 1000, 1000)
//   Demi-bloc : (1000, 500, 1000)
//   Poteau : (333, 1000, 333)
//   Dalle fine : (1000, 100, 1000)
//
// GetHeightAt() retourne toujours SizeY car la surface est PLATE.
// ContainsPoint() vérifie si (x, y, z) est dans le rectangle.
//
// ============================================================================
// CONCEPTS : IMPLÉMENTER UNE INTERFACE
// ============================================================================
// "class BoxCollisionShape : ICollisionShape" signifie :
//   "BoxCollisionShape s'engage à respecter le contrat ICollisionShape"
//
// On DOIT fournir :
//   - GetHeightAt(int localX, int localZ)
//   - ContainsPoint(int localX, int localY, int localZ)
//
// Si on oublie une méthode, le compilateur refuse.
//
// ============================================================================
// PIÈGES À ÉVITER
// ============================================================================
// - Les dimensions sont en millièmes, pas en blocs
// - SizeX = 1000 signifie "un bloc entier", pas "1000 blocs"
// - ContainsPoint utilise < (strictement inférieur), pas <=
//   Car 1000 c'est déjà le bloc SUIVANT
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - ICollisionShape.cs : l'interface qu'on implémente
// - (Futur) ShapeDefinition : créera la bonne CollisionShape selon la forme
// - (Futur) Player : utilisera GetHeightAt pour marcher
// ============================================================================

namespace ProjetColony2.Core.Collision;

public class BoxCollisionShape : ICollisionShape
{
    // ========================================================================
    // DIMENSIONS — Taille de la boîte en unités fixed point (0 à 1000)
    // ========================================================================
    // SizeX, SizeY, SizeZ définissent le rectangle 3D.
    //
    // EXEMPLES :
    //   Cube plein : (1000, 1000, 1000) — remplit tout le voxel
    //   Demi-bloc : (1000, 500, 1000) — moitié de la hauteur
    //   Poteau : (333, 1000, 333) — un tiers de largeur et profondeur
    public int SizeX;
    public int SizeY;
    public int SizeZ;

    // Crée une boîte avec les dimensions spécifiées (en unités, pas en blocs)
    public BoxCollisionShape(int sizeX, int sizeY, int sizeZ)
    {
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
    }

    // ========================================================================
    // GETHEIGHTAT — Hauteur de la surface (toujours plate pour une boîte)
    // ========================================================================
    // Pour une boîte, la surface est PLATE. La hauteur est la même partout.
    // On ignore localX et localZ car ils ne changent rien.
    //
    // EXEMPLES :
    //   Cube plein (SizeY = 1000) → retourne 1000 partout
    //   Demi-bloc (SizeY = 500) → retourne 500 partout
    public int GetHeightAt(int localX, int localZ)
    {
        return SizeY;
    }

    // ========================================================================
    // CONTAINSPOINT — Le point est-il dans la boîte ?
    // ========================================================================
    // Un point est DANS la boîte si ses 3 coordonnées sont dans les limites :
    //   - 0 <= localX < SizeX
    //   - 0 <= localY < SizeY
    //   - 0 <= localZ < SizeZ
    //
    // POURQUOI < ET PAS <= ?
    //   Les coordonnées vont de 0 à 999 (pour un bloc plein de taille 1000).
    //   1000 c'est déjà le bloc SUIVANT, pas celui-ci.
    //   C'est comme les indices de tableau : 0 à 15 pour un tableau de 16.
    public bool ContainsPoint(int localX, int localY, int localZ)
    {
        return localX >= 0 && localX < SizeX
            && localY >= 0 && localY < SizeY
            && localZ >= 0 && localZ < SizeZ;
    }
}