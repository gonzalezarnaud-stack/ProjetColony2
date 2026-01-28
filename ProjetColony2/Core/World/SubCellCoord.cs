// ============================================================================
// SUBCELLCOORD.CS — Coordonnées dans la sous-grille 3×3×3
// ============================================================================
// Chaque voxel (cube de 1m) peut être divisé en une sous-grille de 3×3×3.
// Ça donne 27 petites cases où on peut placer des blocs fins.
// Cette struct représente une POSITION dans cette sous-grille.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Dans Minecraft, un bloc = un bloc. Pas de finesse.
// Dans ProjetColony, on veut pouvoir placer :
//   - Un poteau dans un coin du voxel
//   - Une pente qui ne prend qu'un tiers de la hauteur
//   - Plusieurs petits blocs dans le même voxel
//
// Pour ça, on divise chaque voxel en 3×3×3 = 27 sous-cases.
// SubCellCoord dit : "je suis dans la case (1, 0, 2) de ce voxel".
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// Chaque coordonnée (X, Y, Z) va de 0 à 2 :
//
//   VISUALISATION (vue de dessus, Y ignoré) :
//
//        Z=0    Z=1    Z=2
//       ┌────┬────┬────┐
//  X=0  │0,0 │0,1 │0,2 │
//       ├────┼────┼────┤
//  X=1  │1,0 │1,1 │1,2 │  ← (1,1) = centre
//       ├────┼────┼────┤
//  X=2  │2,0 │2,1 │2,2 │
//       └────┴────┴────┘
//
// En 3D avec l'axe Y :
//   (0,0,0) = coin bas-gauche-arrière du voxel
//   (1,1,1) = centre du voxel (SubCellCoord.Center)
//   (2,2,2) = coin haut-droite-avant du voxel
//
// ============================================================================
// CONCEPTS : POURQUOI UNE STRUCT ?
// ============================================================================
// En C#, il y a "class" et "struct". La différence :
//
// CLASS (référence) :
//   - Stocké sur le "tas" (heap)
//   - La variable contient une ADRESSE vers les données
//   - Passer à une méthode = passer l'adresse (même objet)
//   - Peut être null
//
// STRUCT (valeur) :
//   - Stocké directement là où c'est déclaré
//   - La variable contient LES DONNÉES elles-mêmes
//   - Passer à une méthode = COPIER toutes les données
//   - Ne peut pas être null
//
// SubCellCoord est une STRUCT car :
//   - Très petit (3 bytes seulement)
//   - On en manipule beaucoup (placement, collision...)
//   - Copier 3 bytes c'est plus rapide qu'allouer sur le tas
//
// ============================================================================
// CONCEPTS : POURQUOI BYTE ?
// ============================================================================
// "byte" = entier de 0 à 255 (1 octet de mémoire).
// On n'utilise que 0, 1, 2, donc byte suffit largement.
// C'est plus économe que "int" (4 octets).
//
// Avec des millions de SubCells, chaque octet compte !
//
// ============================================================================
// PIÈGES À ÉVITER
// ============================================================================
// - Les valeurs valides sont 0, 1, 2 SEULEMENT
// - Pas de vérification automatique : new SubCellCoord(5, 0, 0) compile !
// - C'est à toi de t'assurer que les valeurs sont correctes
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Chunk.cs : utilise SubCellCoord comme clé pour le dictionnaire de SubCells
// - SubCell.cs : un bloc fin à une certaine SubCellCoord dans un voxel
// - (Futur) Placement : calculer où placer un bloc fin
// ============================================================================
namespace ProjetColony2.Core.World;

public struct SubCellCoord
{
    // ========================================================================
    // CHAMPS — Les coordonnées dans la sous-grille
    // ========================================================================
    // Chaque champ peut valoir 0, 1, ou 2 :
    //   0 = côté négatif de l'axe (gauche, bas, arrière)
    //   1 = centre
    //   2 = côté positif de l'axe (droite, haut, avant)
    //
    // "public" = accessible depuis l'extérieur
    // "byte" = entier de 0 à 255 (1 octet de mémoire)
    public byte X;
    public byte Y;
    public byte Z;

    // ========================================================================
    // CONSTANTE — La position centrale, très souvent utilisée
    // ========================================================================
    // (1, 1, 1) = pile au milieu du voxel
    //
    // "static" = appartient au TYPE SubCellCoord, pas à une instance
    //            On écrit : SubCellCoord.Center (pas besoin de créer un objet)
    //
    // "readonly" = ne peut pas être modifié après initialisation
    //              Personne ne peut faire SubCellCoord.Center = autreChose;
    //
    // UTILISATION :
    //   var position = SubCellCoord.Center;  // Récupère (1,1,1)
    //   if (maCoord == SubCellCoord.Center)  // Vérifie si c'est le centre
    //
    // POURQUOI C'EST UTILE ?
    //   - Position par défaut pour placer un bloc
    //   - Comparaison : if (coord == SubCellCoord.Center)
    //   - Évite de créer new SubCellCoord(1,1,1) partout
    public static readonly SubCellCoord Center = new SubCellCoord(1, 1, 1);
    
    // ========================================================================
    // CONSTRUCTEUR — Crée une nouvelle coordonnée
    // ========================================================================
    // Appelé quand on écrit : new SubCellCoord(0, 2, 1)
    //
    // Les paramètres (minuscules) sont copiés dans les champs (majuscules).
    // Cette convention permet de distinguer les deux dans le code.
    //
    // EXEMPLE :
    //   var coinBasGauche = new SubCellCoord(0, 0, 0);
    //   var centre = new SubCellCoord(1, 1, 1);
    //   var coinHautDroit = new SubCellCoord(2, 2, 2);
    //
    // ATTENTION : Pas de validation !
    //   new SubCellCoord(99, 0, 0) ne lève pas d'erreur.
    //   C'est au code appelant de s'assurer que les valeurs sont 0, 1 ou 2.
    public SubCellCoord(byte x, byte y, byte z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    // ========================================================================
    // TOSTRING — Représentation texte pour le debug
    // ========================================================================
    // "override" = on remplace la méthode héritée de System.Object
    //
    // Sans notre override, ToString() afficherait :
    //   "ProjetColony2.Core.World.SubCellCoord" (pas utile !)
    //
    // Avec notre override, on voit les valeurs :
    //   "(1, 2, 0)" — beaucoup plus pratique pour débugger !
    //
    // Le "$" devant la string active l'INTERPOLATION :
    //   $"({X}, {Y}, {Z})" avec X=1, Y=2, Z=0 devient "(1, 2, 0)"
    //   Les {variables} sont remplacées par leurs valeurs.
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}