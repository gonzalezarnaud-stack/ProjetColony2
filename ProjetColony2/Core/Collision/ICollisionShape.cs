// ============================================================================
// ICOLLISIONSHAPE.CS — Interface pour les formes de collision
// ============================================================================
// Une INTERFACE définit un CONTRAT : "toute classe qui m'implémente
// DOIT avoir ces méthodes". C'est comme un cahier des charges.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Le joueur marche sur des blocs. Mais les blocs ont des formes différentes :
//   - Cube plein : surface plate en haut
//   - Pente : surface inclinée
//   - Demi-bloc : surface plate à mi-hauteur
//
// Chaque forme calcule sa collision différemment.
// L'interface permet de les traiter de façon UNIFORME :
//
//   ICollisionShape shape = GetShapeAt(position);
//   int height = shape.GetHeightAt(x, z);  // Marche pour toutes les formes !
//
// Sans interface, on aurait :
//   if (shape is BoxShape) { ... }
//   else if (shape is SlopeShape) { ... }  // Moche et fragile !
//
// ============================================================================
// CONCEPTS : QU'EST-CE QU'UNE INTERFACE ?
// ============================================================================
// Une interface c'est comme un CONTRAT ou une PROMESSE.
// Elle dit : "je promets d'avoir ces méthodes".
// Elle ne dit PAS comment les méthodes fonctionnent.
//
// EXEMPLE CONCRET :
//   Interface "IVehicule" promet : Démarrer(), Freiner(), Accélérer()
//   Classe "Voiture" implémente IVehicule : ses propres versions
//   Classe "Moto" implémente IVehicule : ses propres versions
//
//   Le code qui utilise IVehicule peut faire :
//     vehicule.Démarrer();  // Marche pour voiture ET moto
//
// SYNTAXE :
//   - "interface" au lieu de "class"
//   - Les méthodes n'ont PAS de corps (pas de { })
//   - Tout est implicitement public
//
// ============================================================================
// CONCEPTS : FIXED POINT (×1000)
// ============================================================================
// On utilise des ENTIERS au lieu de floats pour être 100% déterministe.
//
//   1 bloc = 1000 unités
//   0.5 bloc = 500 unités
//   1/3 bloc = 333 unités
//
// POURQUOI ?
//   En multijoueur lockstep, tous les clients doivent calculer EXACTEMENT
//   la même chose. Les floats ont des erreurs d'arrondi qui varient selon
//   le CPU. Les entiers sont toujours exacts.
//
// ============================================================================
// PIÈGES À ÉVITER
// ============================================================================
// - On ne peut PAS faire : new ICollisionShape() — c'est une interface
// - Toute classe qui implémente DOIT fournir les deux méthodes
// - Les valeurs sont en millièmes (0 à 1000), pas en mètres
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - BoxCollisionShape.cs : implémente pour les cubes/demi-blocs
// - SlopeCollisionShape.cs : implémente pour les pentes
// - (Futur) Player movement : utilisera GetHeightAt pour marcher
// - (Futur) Physique : utilisera ContainsPoint pour les collisions
// ============================================================================

namespace ProjetColony2.Core.Collision;

public interface ICollisionShape
{
    // ========================================================================
    // CONSTANTE — Taille d'un bloc en unités fixed point
    // ========================================================================
    // 1 bloc = 1000 unités
    // Milieu du bloc = 500
    // Un tiers = 333, deux tiers = 666
    //
    // POURQUOI 1000 ?
    // - Assez précis (résolution de 1mm pour un bloc d'1m)
    // - Facile à lire et débugger (500 = moitié, c'est évident)
    // - Entier = calculs déterministes (pas d'erreurs de floats)
    // ACCESSIBLE PARTOUT :
    //   int taille = ICollisionShape.Unit;  // 1000
    public const int Unit = 1000;

    // ========================================================================
    // GETHEIGHTAT — Hauteur de la surface à une position locale
    // ========================================================================
    // "À quelle hauteur est le sol si je me tiens en (x, z) ?"
    //
    // PARAMÈTRES :
    //   localX, localZ : position dans le bloc (0 à 1000)
    //
    // RETOUR :
    //   Hauteur de la surface (0 à 1000)
    //
    // EXEMPLES PAR FORME :
    //   Cube plein : retourne toujours 1000 (surface plate en haut)
    //   Demi-bloc : retourne toujours 500 (surface plate à mi-hauteur)
    //   Pente : retourne 0 à 1000 selon localZ (surface inclinée)
    //
    // UTILISATION :
    //   Calculer où le joueur pose ses pieds quand il marche.
    int GetHeightAt(int localX, int localZ);

    // ========================================================================
    // CONTAINSPOINT — Le point est-il à l'intérieur de la forme ?
    // ========================================================================
    // "Est-ce que ce point (x, y, z) est DANS le bloc solide ?"
    //
    // PARAMÈTRES :
    //   localX, localY, localZ : position dans le bloc (0 à 1000)
    //
    // RETOUR :
    //   true = le point est dans la partie solide
    //   false = le point est dans le vide
    //
    // UTILISATION :
    //   - Collision joueur : est-ce que je rentre dans un mur ?
    //   - Placement bloc : est-ce qu'il y a déjà quelque chose ici ?
    //   - Raycast : est-ce que le rayon touche ce bloc ?
    bool ContainsPoint(int localX, int localY, int localZ);
}