// ============================================================================
// VOXEL.CS — Un cube de terrain (1m³)
// ============================================================================
// Le Voxel est l'unité de base du monde, comme les blocs dans Minecraft.
// Le monde est une grille 3D géante, et chaque case contient un Voxel
// qui dit : "ici c'est de la pierre" ou "ici c'est vide (air)".
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Le monde doit savoir ce qu'il y a à chaque position.
// Le Voxel est la réponse la plus simple : un numéro de matériau.
//   - MaterialId = 0 → Air (vide, traversable)
//   - MaterialId = 1 → Pierre
//   - MaterialId = 2 → Terre
//   - etc.
//
// ============================================================================
// VOXEL VS SUBCELL : QUELLE DIFFÉRENCE ?
// ============================================================================
// VOXEL = Le terrain NATUREL
//   - Pierre, terre, herbe, eau, lave...
//   - Généré par le monde (grottes, collines...)
//   - Simple : juste un type de matériau
//
// SUBCELL = Les CONSTRUCTIONS du joueur
//   - Poteaux, pentes, meubles, portes...
//   - Placé par le joueur ou les colons
//   - Complexe : forme + matériau + orientation
//
// On peut avoir les DEUX au même endroit :
//   Un poteau (SubCell) posé sur de la pierre (Voxel)
//
// ============================================================================
// CONCEPTS : POURQUOI UNE STRUCT ?
// ============================================================================
// On aura des MILLIONS de voxels (un chunk 16×16×16 = 4096 voxels).
// Struct = petit, rapide, pas d'allocation individuelle.
//
// Un Voxel fait 1 byte. Un million de voxels = 1 Mo.
// Si c'était une class, chaque voxel aurait un overhead de ~24 bytes.
// Un million de voxels = 24 Mo juste pour l'overhead !
//
// ============================================================================
// CONCEPTS : POURQUOI BYTE POUR MATERIALID ?
// ============================================================================
// byte = 0 à 255. Suffisant pour 256 types de terrain.
// Pierre, terre, herbe, sable, eau, lave, glace, neige... 256 c'est beaucoup !
//
// Si un jour on en veut plus, on passera à ushort (0 à 65535).
// Mais pour l'instant, byte = économie de mémoire.
//
// ============================================================================
// PIÈGES À ÉVITER
// ============================================================================
// - MaterialId = 0 signifie AIR, pas "non initialisé"
// - Un nouveau Voxel() a MaterialId = 0 par défaut = air
// - Ne pas confondre Voxel (terrain) et SubCell (construction)
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Chunk.cs : stocke un tableau 3D de Voxels
// - GameWorld.cs : accède aux voxels via GetVoxel/SetVoxel
// - ChunkRenderer.cs : dessine les faces des voxels non-air
// - (Futur) MaterialDefinition : définira les propriétés de chaque matériau
// ============================================================================

namespace ProjetColony2.Core.World;

public struct Voxel
{
    // ========================================================================
    // CHAMP — La seule donnée stockée dans un Voxel
    // ========================================================================
    // Le numéro qui identifie le type de terrain :
    //   0 = Air (vide, le joueur peut passer à travers)
    //   1 = Pierre
    //   2 = Terre
    //   3 = Herbe
    //   ... (définis dans un fichier JSON plus tard)
    //
    // "byte" = nombre entier de 0 à 255
    // C'est suffisant pour identifier 256 types de terrain différents
    // (pierre, terre, sable, eau, lave, etc.)
    //
    // On utilise byte plutôt que int (qui va jusqu'à 2 milliards) pour
    // économiser de la mémoire : 1 byte vs 4 bytes par voxel.
    // Avec des millions de voxels, ça compte !
    public byte MaterialId;

    // ========================================================================
    // CHAMP — Forme et orientation du voxel (encodé sur 16 bits)
    // ========================================================================
    // Ce champ contient 4 informations compactées :
    //   - BaseShape (4 bits) : cube, pente, angle... (voir ShapeData.cs)
    //   - Rotation (2 bits) : Nord/Est/Sud/Ouest
    //   - Position (2 bits) : Debout/Couché/Inversé
    //   - Height (8 bits) : hauteur en 1/25 de bloc (25 = 1 bloc)
    //
    // Pourquoi tout mettre dans un seul ushort ?
    //   - Économie mémoire : 2 bytes au lieu de 4 champs séparés
    //   - Millions de voxels = chaque byte compte !
    //
    // Pour lire/écrire ces valeurs, utilise la struct ShapeData :
    //   var data = new ShapeData(shape, rotation, position, height);
    //   voxel.Shape = data.Value;
    //
    // "ushort" = entier non-signé de 0 à 65535 (16 bits)
    public ushort Shape;

    // ========================================================================
    // PROPRIÉTÉ CALCULÉE — Un raccourci pratique
    // ========================================================================
    // "=>" c'est la syntaxe courte pour une propriété en lecture seule.
    // 
    // C'est équivalent à écrire :
    //   public bool IsAir 
    //   { 
    //       get { return MaterialId == 0; } 
    //   }
    //
    // La valeur n'est pas stockée, elle est calculée à chaque appel.
    // C'est utile car si MaterialId change, IsAir sera automatiquement à jour.
    //
    // UTILISATION :
    //   if (voxel.IsAir) { /* le joueur peut passer */ }
    //   if (!voxel.IsAir) { /* c'est solide, collision */ }
    public bool IsAir => MaterialId == 0;

    // ========================================================================
    // CONSTANTE STATIQUE — Un voxel vide prêt à l'emploi
    // ========================================================================
    // "static" = appartient au TYPE Voxel, pas à une instance particulière
    //            On y accède via Voxel.Air (pas besoin de créer un voxel d'abord)
    //
    // "readonly" = cette valeur ne peut pas être modifiée après l'initialisation
    //              C'est une sécurité : personne ne peut faire Voxel.Air = autreChose;
    //
    // UTILISATION :
    //   chunk.SetVoxel(x, y, z, Voxel.Air);  // Effacer un bloc
    //   if (voxel == Voxel.Air) { ... }       // Comparer (attention, struct !)
    //
    // POURQUOI C'EST UTILE ?
    // Au lieu d'écrire partout : new Voxel(0)
    // On écrit simplement : Voxel.Air
    // C'est plus lisible et on ne crée pas d'objet à chaque fois.
    public static readonly Voxel Air = new Voxel(0, 0);

    // ========================================================================
    // CONSTRUCTEUR — Crée un nouveau Voxel
    // ========================================================================
    // PARAMÈTRES :
    //   materialId : type de terrain (0=air, 1=pierre, 2=terre...)
    //   shape : forme encodée (par défaut = cube plein de 1 bloc)
    //
    // VALEUR PAR DÉFAUT :
    //   ShapeData.FULL_CUBE = un cube plein standard
    //   Ainsi, new Voxel(1) crée un bloc de pierre cubique.
    //
    // EXEMPLES :
    //   new Voxel(1)                        → Pierre, cube plein
    //   new Voxel(2, ShapeData.FULL_CUBE)   → Terre, cube plein
    //   new Voxel(1, myCustomShape.Value)   → Pierre, forme custom
    //
    // Le paramètre "materialId" (minuscule) est copié dans le champ 
    // "MaterialId" (majuscule). C'est une convention C# pour distinguer
    // les paramètres des champs.
    public Voxel(byte materialId, ushort shape = ShapeData.FULL_CUBE)
    {
        MaterialId = materialId;
        Shape = shape;
    }

    // ========================================================================
    // TOSTRING — Affichage texte pour le debug
    // ========================================================================
    // "override" = on REMPLACE la méthode ToString() héritée de System.Object
    // Sans override, ToString() afficherait juste "ProjetColony2.Core.World.Voxel"
    //
    // Affiche le MaterialId entre parenthèses : "(1)" pour la pierre, "(0)" pour l'air
    // Très utile quand on debug avec GD.Print() ou Console.WriteLine()
    public override string ToString()
    {
        return $"({MaterialId})";
    }
}