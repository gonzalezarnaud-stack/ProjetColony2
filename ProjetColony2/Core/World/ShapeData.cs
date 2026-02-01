// ============================================================================
// SHAPEDATA.CS — Encode la forme d'un voxel sur 16 bits
// ============================================================================
// Un voxel peut avoir différentes formes : cube, pente, angle...
// Plus une rotation, une position (debout/couché), et une hauteur.
//
// Ça fait 4 informations à stocker. On pourrait faire 4 champs séparés :
//   byte Shape;      // 1 byte
//   byte Rotation;   // 1 byte
//   byte Position;   // 1 byte
//   byte Height;     // 1 byte
//   Total : 4 bytes par voxel
//
// Mais avec des millions de voxels, on veut économiser la mémoire.
// Alors on "compacte" tout dans un seul ushort (2 bytes) :
//
//   ┌─────────────────┬───────┬───────┬─────────┐
//   │    Height       │  Pos  │  Rot  │  Shape  │
//   │    8 bits       │2 bits │2 bits │ 4 bits  │
//   └─────────────────┴───────┴───────┴─────────┘
//   Bits: 15--------8   7---6   5---4   3-----0
//
// ============================================================================
// C'EST QUOI LE "BIT SHIFTING" ?
// ============================================================================
// Imagine des cases numérotées de 0 à 15 (les 16 bits d'un ushort).
// Chaque case contient un 0 ou un 1.
//
// EXEMPLE : le nombre 25 en binaire
//   25 = 0000 0000 0001 1001
//         ↑              ↑
//        bit 15         bit 0
//
// "<<" (shift left) = décale tous les bits vers la gauche
//   25 << 8 = 0001 1001 0000 0000 = 6400
//   Les bits de 25 sont maintenant dans les positions 8-15 !
//
// ">>" (shift right) = décale vers la droite (inverse)
//   6400 >> 8 = 0000 0000 0001 1001 = 25
//   On récupère la valeur originale.
//
// "&" (AND binaire) = garde seulement certains bits (masque)
//   0011 0101 & 0000 1111 = 0000 0101
//   Le masque 0xF (= 0000 1111) garde les 4 bits de droite.
//
// "|" (OR binaire) = combine des valeurs sans les écraser
//   0000 0001 | 0011 0000 = 0011 0001
//   Chaque 1 de l'une OU l'autre source donne un 1.
//
// ============================================================================
// EXEMPLE CONCRET : ENCODER UNE PENTE
// ============================================================================
// On veut : Shape=PENTE(2), Rotation=EST(1), Position=DEBOUT(0), Height=25
//
// Étape 1 : Shape (bits 0-3)
//   2 = 0000 0000 0000 0010
//
// Étape 2 : Rotation décalée de 4 (bits 4-5)
//   1 << 4 = 0000 0000 0001 0000
//
// Étape 3 : Position décalée de 6 (bits 6-7)
//   0 << 6 = 0000 0000 0000 0000
//
// Étape 4 : Height décalée de 8 (bits 8-15)
//   25 << 8 = 0001 1001 0000 0000
//
// Étape 5 : Combiner avec OR (|)
//   0000 0000 0000 0010  (shape)
// | 0000 0000 0001 0000  (rotation)
// | 0000 0000 0000 0000  (position)
// | 0001 1001 0000 0000  (height)
// = 0001 1001 0001 0010  = 6418 en décimal
//
// Un seul nombre (6418) contient les 4 informations !
//
// ============================================================================
// EXEMPLE CONCRET : DÉCODER LA VALEUR
// ============================================================================
// On a Value = 6418. On veut extraire Rotation.
//
// Étape 1 : Décaler pour amener Rotation à droite
//   6418 >> 4 = 0000 0001 1001 0001 = 401
//
// Étape 2 : Masquer pour garder seulement 2 bits
//   401 & 0x3 = 401 & 0000 0011 = 0000 0001 = 1
//
// Rotation = 1 = EST. Correct !
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Voxel.cs : stocke un ShapeData.Value dans son champ Shape
// - ChunkRenderer.cs : lit Shape/Rotation/Height pour générer le mesh
// - WorldGenerator.cs : crée des ShapeData pour le terrain (pentes, etc.)
// ============================================================================

namespace ProjetColony2.Core.World;

public struct ShapeData
{
    // ========================================================================
    // CONSTANTES — Formes de base (BaseShape)
    // ========================================================================
    public const byte SHAPE_AIR = 0;
    public const byte SHAPE_CUBE = 1;
    public const byte SHAPE_SLOPE = 2;
    public const byte SHAPE_CORNER_OUT = 3;
    public const byte SHAPE_CORNER_IN = 4;

    // ========================================================================
    // CONSTANTES — Rotations
    // ========================================================================
    public const byte ROT_NORTH = 0;
    public const byte ROT_EAST = 1;
    public const byte ROT_SOUTH = 2;
    public const byte ROT_WEST = 3;

    // ========================================================================
    // CONSTANTES — Positions
    // ========================================================================
    public const byte POS_UPRIGHT = 0;    // Debout
    public const byte POS_SIDEWAYS = 1;   // Couché
    public const byte POS_INVERTED = 2;   // Inversé

    // ========================================================================
    // CONSTANTES — Hauteurs courantes (en 1/25 de bloc)
    // ========================================================================
    public const byte HEIGHT_FULL = 25;       // 1 bloc complet
    public const byte HEIGHT_HALF = 12;       // ~1/2 bloc
    public const byte HEIGHT_SLAB = 5;        // 1/5 bloc (dalle fine)

    // ========================================================================
    // CONSTANTE — Cube plein standard (valeur encodée)
    // ========================================================================
    // Un cube plein : Shape=1, Rotation=0, Position=0, Height=25
    // Calcul : 1 | (0 << 4) | (0 << 6) | (25 << 8) = 1 + 6400 = 6401
    public const ushort FULL_CUBE = (SHAPE_CUBE) | (HEIGHT_FULL << 8);

    // ========================================================================
    // CHAMP — La valeur encodée sur 16 bits
    // ========================================================================
    public ushort Value;

    // ========================================================================
    // CONSTRUCTEUR — Crée un ShapeData à partir des composants
    // ========================================================================
    // On "emballe" les 4 valeurs dans un seul ushort avec du bit shifting.
    //
    // COMMENT ÇA MARCHE ?
    // Imagine des cases : [Height 8 bits][Pos 2 bits][Rot 2 bits][Shape 4 bits]
    //
    // "<<" = décalage à gauche (shift left)
    //   - shape reste en place (bits 0-3)
    //   - rotation décalée de 4 positions (bits 4-5)
    //   - position décalée de 6 positions (bits 6-7)
    //   - height décalée de 8 positions (bits 8-15)
    //
    // "|" = OU binaire, combine les valeurs sans les écraser
    //
    public ShapeData(byte shape, byte rotation, byte position, byte height)
    {
        Value = (ushort)(
            (shape & 0xF) |           // 4 bits pour shape (0xF = 1111 en binaire)
            ((rotation & 0x3) << 4) | // 2 bits pour rotation, décalés de 4
            ((position & 0x3) << 6) | // 2 bits pour position, décalés de 6
            (height << 8)             // 8 bits pour height, décalés de 8
        );
    }

    // ========================================================================
    // PROPRIÉTÉS — Extraire les valeurs du ushort encodé
    // ========================================================================
    // C'est l'inverse du constructeur : on "déballe" les valeurs.
    //
    // ">>" = décalage à droite (shift right)
    // "&" = ET binaire, garde seulement les bits qui nous intéressent (masque)
    //
    // EXEMPLE pour extraire Rotation :
    //   Value = 0110 0101 0011 0010  (exemple)
    //   Value >> 4 = 0000 0110 0101 0011  (décalé de 4 vers la droite)
    //   & 0x3 = garde seulement les 2 derniers bits = 11 = 3
    //
    public byte Shape => (byte)(Value & 0xF);
    public byte Rotation => (byte)((Value >> 4) & 0x3);
    public byte Position => (byte)((Value >> 6) & 0x3);
    public byte Height => (byte)(Value >> 8);

    // ========================================================================
    // PROPRIÉTÉ — Raccourci pour vérifier si c'est de l'air
    // ========================================================================
    public bool IsAir => Shape == SHAPE_AIR;
}