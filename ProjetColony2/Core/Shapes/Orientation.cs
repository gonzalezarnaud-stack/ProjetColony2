// ============================================================================
// ORIENTATION.CS — Les 12 orientations possibles d'un bloc
// ============================================================================
// Quand on place un bloc (escalier, pente, poteau...), il faut savoir dans
// quelle direction il "regarde". C'est l'orientation.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Dans l'ancien ProjetColony, on avait RotationY (0-3) et RotationX (0-3).
// Ça donnait 16 combinaisons, mais :
//   - Certaines étaient redondantes
//   - Les calculs étaient compliqués
//   - On se trompait souvent
//
// Le nouveau système : 12 orientations avec des NOMS CLAIRS.
// Plus de calculs, plus de confusion.
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// Un bloc peut être posé de 3 façons verticales :
//   - Horizontal : posé normalement (comme un escalier)
//   - Up : basculé vers le haut (comme un poteau couché)
//   - Down : basculé vers le bas (accroché au plafond)
//
// Et 4 façons horizontales (comme une boussole) :
//   - North : face vers -Z
//   - East : face vers +X
//   - South : face vers +Z
//   - West : face vers -X
//
// Total : 3 × 4 = 12 combinaisons, toutes utiles et uniques.
//
// ============================================================================
// CONCEPTS : QU'EST-CE QU'UN ENUM ?
// ============================================================================
// Un "enum" (énumération) c'est une liste FERMÉE de choix possibles.
// 
// Au lieu de manipuler des nombres qu'on oublie vite :
//   int rotation = 2;  // C'est quoi 2 ? Est ? Sud ? On sait plus...
//
// On utilise des noms clairs :
//   Orientation orientation = Orientation.East;  // Évident !
//
// AVANTAGES :
//   - Le compilateur vérifie qu'on utilise des valeurs valides
//   - Si on écrit Orientation.Nrth (typo), ça ne compile pas
//   - Le code est lisible : if (bloc.Orientation == Orientation.North)
//
// EN COULISSE :
//   C# stocke chaque valeur comme un entier (0, 1, 2, 3...).
//   North = 0, East = 1, South = 2, etc.
//   Mais on n'a jamais besoin de connaître ces chiffres.
//
// ============================================================================
// PIÈGES À ÉVITER
// ============================================================================
// - Ne jamais utiliser des int à la place de l'enum (même si c'est possible)
// - Ne pas confondre North (face vers -Z) et Up (basculé vers le haut)
// - Se souvenir que Godot utilise -Z pour "devant" (convention OpenGL)
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - SubCell.cs : chaque SubCell a une Orientation
// - (Futur) ShapeDefinition.cs : définira comment orienter chaque forme
// - (Futur) Rendu : l'orientation détermine la rotation du mesh
// ============================================================================

namespace ProjetColony2.Core.Shapes;

public enum Orientation
{
    // ========================================================================
    // ORIENTATIONS HORIZONTALES — Bloc posé normalement sur le sol
    // ========================================================================
    // La "face avant" du bloc pointe vers cette direction.
    // Imagine une chaise : où est le dossier ? Vers le Nord ? L'Est ?
    //
    // RAPPEL DES AXES (convention Godot) :
    //   -Z = Nord (devant au démarrage de la caméra)
    //   +X = Est (droite)
    //   +Z = Sud (derrière)
    //   -X = Ouest (gauche)
    //   +Y = Haut
    //   -Y = Bas
    //
    // EXEMPLES D'UTILISATION :
    //   - Escalier qui monte vers le Nord : Orientation.North
    //   - Porte qui s'ouvre vers l'Est : Orientation.East
    //   - Coffre dont l'ouverture est vers le Sud : Orientation.South
    
    North,      // Face vers -Z (vers le "nord" de la carte)
    East,       // Face vers +X (vers l'est)
    South,      // Face vers +Z (vers le sud)
    West,       // Face vers -X (vers l'ouest)
    
// ========================================================================
    // ORIENTATIONS VERS LE HAUT — Bloc basculé à l'horizontale
    // ========================================================================
    // Le "sommet" du bloc (qui était vers +Y) pointe maintenant vers
    // une direction horizontale.
    //
    // VISUALISATION :
    //   Imagine un poteau vertical. Tu le couches.
    //   Son sommet peut pointer vers North, East, South ou West.
    //
    // EXEMPLES D'UTILISATION :
    //   - Poutre horizontale le long de l'axe Z : UpNorth ou UpSouth
    //   - Tronc d'arbre couché : UpEast
    //   - Tuyau horizontal : UpWest
    
    UpNorth,    // Sommet vers -Z
    UpEast,     // Sommet vers +X
    UpSouth,    // Sommet vers +Z
    UpWest,     // Sommet vers -X
    
    // ========================================================================
    // ORIENTATIONS VERS LE BAS — Bloc basculé et inversé
    // ========================================================================
    // Comme les orientations "Up", mais à l'envers.
    // Le sommet du bloc pointe horizontalement, et la BASE est en haut.
    //
    // QUAND L'UTILISER ?
    //   C'est plus rare, mais nécessaire pour :
    //   - Stalactites (pointe vers le bas)
    //   - Blocs accrochés au plafond
    //   - Escaliers inversés (descendre depuis un plafond)
    //   - Symétrie architecturale
    
    DownNorth,  // Sommet vers -Z, base vers +Z
    DownEast,   // Sommet vers +X, base vers -X
    DownSouth,  // Sommet vers +Z, base vers -Z
    DownWest    // Sommet vers -X, base vers +X
}