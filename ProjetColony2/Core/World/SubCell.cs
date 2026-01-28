// ============================================================================
// SUBCELL.CS — Une case de la sous-grille 3×3×3
// ============================================================================
// Chaque voxel (1m³) peut contenir une sous-grille de 3×3×3 = 27 cases.
// Chaque case (SubCell) peut contenir UN bloc construit par le joueur.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Le Voxel gère le TERRAIN (pierre, terre...).
// La SubCell gère les CONSTRUCTIONS (poteaux, pentes, meubles...).
//
// Une construction c'est plus complexe qu'un terrain :
//   - FORME : cube, pente, poteau, escalier...
//   - MATÉRIAU : pierre, bois, fer...
//   - ORIENTATION : vers le nord, tourné à 90°...
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// Une SubCell a 3 informations :
//
// 1. ShapeId (forme)
//    0 = vide (pas de bloc)
//    1 = cube plein
//    2 = pente
//    3 = poteau
//    ... (définis dans shapes.json plus tard)
//
// 2. MaterialId (matériau)
//    Même logique que Voxel : 1 = pierre, 2 = bois, etc.
//
// 3. Orientation
//    L'enum Orientation : North, East, UpNorth, DownSouth, etc.
//
// ============================================================================
// VOXEL + SUBCELL : COMMENT ÇA COHABITE ?
// ============================================================================
// Le Chunk stocke SÉPARÉMENT :
//   - Un tableau de Voxels (terrain, toujours 16×16×16)
//   - Un dictionnaire de SubCells (constructions, seulement celles qui existent)
//
// À une position donnée :
//   - Il y a TOUJOURS un Voxel (même si c'est de l'air)
//   - Il y a PARFOIS des SubCells (si le joueur a construit)
//
// ============================================================================
// CONCEPTS : POURQUOI UNE STRUCT ?
// ============================================================================
// Même logique que Voxel : petit (3 bytes), nombreux, économe.
//
// Taille d'une SubCell :
//   - ShapeId : 1 byte
//   - MaterialId : 1 byte
//   - Orientation : 1 byte (un enum stocke un int, mais ici on a peu de valeurs)
//   Total : ~3 bytes
//
// ============================================================================
// PIÈGES À ÉVITER
// ============================================================================
// - ShapeId = 0 signifie VIDE, pas "par défaut"
// - Une SubCell vide a quand même une orientation (on s'en fiche, mais elle existe)
// - Ne pas oublier de vérifier IsEmpty avant d'utiliser une SubCell
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Chunk.cs : stocke les SubCells dans un Dictionary<long, SubCell>
// - SubCellCoord.cs : la position de la SubCell dans la sous-grille
// - Orientation.cs : l'orientation du bloc
// - (Futur) ShapeDefinition : définira les propriétés de chaque forme
// - (Futur) SubBlockRenderer : affichera les SubCells avec MultiMesh
// ============================================================================

using ProjetColony2.Core.Shapes;

namespace ProjetColony2.Core.World;

public struct SubCell
{
    // ========================================================================
    // CHAMPS — Les données qui définissent le bloc
    // ========================================================================
    // ShapeId = la FORME du bloc (définie dans shapes.json plus tard)
    //   0 = vide (pas de bloc)
    //   1 = cube plein
    //   2 = pente
    //   3 = poteau
    //   ... (définis dans shapes.json)
    //
    // MaterialId = le MATÉRIAU (comme pour Voxel)
    //   0 = aucun (si ShapeId = 0)
    //   1 = pierre
    //   2 = bois
    //   ... (définis dans materials.json)
    //
    // Orientation = la DIRECTION où le bloc "regarde"
    //   Voir Orientation.cs pour les 12 valeurs possibles.
    //   Important pour les pentes, escaliers, meubles...
    public byte ShapeId;
    public byte MaterialId;
    public Orientation Orientation;

    // ========================================================================
    // PROPRIÉTÉ — Raccourci pour vérifier si la case est vide
    // ========================================================================
    // Une case est vide si elle n'a pas de forme (ShapeId == 0).
    // Dans ce cas, MaterialId et Orientation sont ignorés.
    //
    // UTILISATION :
    //   if (subCell.IsEmpty) { /* on peut placer quelque chose */ }
    //   if (!subCell.IsEmpty) { /* il y a déjà un bloc */ }
    public bool IsEmpty => ShapeId == 0;

    // ========================================================================
    // CONSTANTE — Une SubCell vide prête à l'emploi
    // ========================================================================
    // Au lieu d'écrire : new SubCell(0, 0, Orientation.North)
    // On écrit : SubCell.Empty
    //
    // L'orientation est North par défaut, mais ça n'a pas d'importance
    // pour une case vide (on ne l'affiche pas).
    public static readonly SubCell Empty = new SubCell(0, 0, Orientation.North);

    // ========================================================================
    // CONSTRUCTEUR — Crée une nouvelle SubCell
    // ========================================================================
    // Exemple : new SubCell(2, 1, Orientation.East)
    // → Une pente (forme 2) en pierre (matériau 1) orientée vers l'est
    public SubCell(byte shapeId, byte materialId, Orientation orientation)
    {
        ShapeId = shapeId;
        MaterialId = materialId;
        Orientation = orientation; 
    }

    // ========================================================================
    // TOSTRING — Affichage debug
    // ========================================================================
    // Affiche "(ShapeId, MaterialId, Orientation)"
    // Exemple : "(2, 1, East)" = pente en pierre vers l'est
    public override string ToString()
    {
        return $"({ShapeId}, {MaterialId}, {Orientation})";
    }
}