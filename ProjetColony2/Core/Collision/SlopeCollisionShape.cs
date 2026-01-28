// ============================================================================
// SLOPECOLLISIONSHAPE.CS — Collision pour les pentes
// ============================================================================
// Une pente a une surface INCLINÉE. La hauteur varie selon la position.
// C'est plus complexe qu'une boîte, mais permet des escaliers fluides.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Une boîte a une surface plate. Mais pour les pentes, la hauteur change :
//   - Au début de la pente (Z = 0) : hauteur basse
//   - À la fin de la pente (Z = 1000) : hauteur haute
//
// Le joueur doit pouvoir MARCHER sur la pente, pas sauter de plateau en plateau.
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// On définit deux hauteurs :
//   - MinHeight : hauteur au début de la pente (localZ = 0)
//   - MaxHeight : hauteur à la fin de la pente (localZ = 1000)
//
// GetHeightAt() fait une INTERPOLATION LINÉAIRE entre les deux :
//   hauteur = MinHeight + (MaxHeight - MinHeight) * localZ / 1000
//
// EXEMPLES :
//   Pente complète : MinHeight = 0, MaxHeight = 1000
//     localZ = 0 → hauteur = 0
//     localZ = 500 → hauteur = 500
//     localZ = 1000 → hauteur = 1000
//
//   Demi-pente haute : MinHeight = 500, MaxHeight = 1000
//     localZ = 0 → hauteur = 500
//     localZ = 1000 → hauteur = 1000
//
// ============================================================================
// CONCEPTS : INTERPOLATION LINÉAIRE
// ============================================================================
// "Interpoler" = trouver une valeur ENTRE deux points connus.
// "Linéaire" = la progression est régulière (ligne droite).
//
// FORMULE :
//   résultat = début + (fin - début) * progression / max
//
// EXEMPLE CONCRET :
//   Tu es à 30% du chemin entre 0 et 100.
//   Position = 0 + (100 - 0) * 30 / 100 = 30
//
// C'est comme ça qu'on calcule la hauteur sur une pente.
//
// ============================================================================
// PIÈGES À ÉVITER
// ============================================================================
// - MinHeight et MaxHeight sont en millièmes (0 à 1000)
// - La pente suit l'axe Z. Pour une pente sur X, il faudrait une autre classe
// - L'orientation (North, South...) est gérée ailleurs, pas ici
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - ICollisionShape.cs : l'interface qu'on implémente
// - (Futur) Orientation : déterminera dans quelle direction la pente monte
// - (Futur) Player : marchera fluidement sur les pentes grâce à GetHeightAt
// ============================================================================

namespace ProjetColony2.Core.Collision;

public class SlopeCollisionShape : ICollisionShape
{
    // ========================================================================
    // HAUTEURS — Définissent l'inclinaison de la pente
    // ========================================================================
    // MinHeight : hauteur au début de la pente (localZ = 0)
    // MaxHeight : hauteur à la fin de la pente (localZ = 1000)
    //
    // EXEMPLES :
    //   Pente complète : MinHeight = 0, MaxHeight = 1000
    //   Demi-pente basse : MinHeight = 0, MaxHeight = 500
    //   Demi-pente haute : MinHeight = 500, MaxHeight = 1000
    public int MinHeight;
    public int MaxHeight;

    // Crée une pente avec les hauteurs de début et fin
    public SlopeCollisionShape(int minHeight, int maxHeight)
    {
        MinHeight = minHeight;
        MaxHeight = maxHeight;
    }

    // ========================================================================
    // GETHEIGHTAT — Hauteur de la surface selon la position
    // ========================================================================
    // Interpolation linéaire entre MinHeight et MaxHeight selon localZ.
    //
    // FORMULE :
    //   hauteur = Min + (Max - Min) * localZ / 1000
    //
    // EXEMPLES (pente complète Min=0, Max=1000) :
    //   localZ = 0 → 0 + (1000-0) * 0 / 1000 = 0
    //   localZ = 500 → 0 + (1000-0) * 500 / 1000 = 500
    //   localZ = 1000 → 0 + (1000-0) * 1000 / 1000 = 1000
    //
    // NOTE : On ignore localX car la pente est constante sur cet axe.
    //        La pente monte/descend selon Z uniquement.
    public int GetHeightAt(int localX, int localZ)
    {
        return MinHeight + (MaxHeight - MinHeight) * localZ / ICollisionShape.Unit;
    }

    // ========================================================================
    // CONTAINSPOINT — Le point est-il dans la pente ?
    // ========================================================================
    // Un point est dans la pente si :
    //   1. Il est dans les limites X et Z du bloc (0 à Unit)
    //   2. Son Y est entre 0 et la hauteur de la pente à cette position
    //
    // On réutilise GetHeightAt() pour avoir la hauteur locale.
    // C'est le principe DRY (Don't Repeat Yourself) :
    //   La formule d'interpolation est écrite UNE SEULE fois.
    public bool ContainsPoint(int localX, int localY, int localZ)
    {
        int heightHere = GetHeightAt(localX, localZ);

        return localX >= 0 && localX < ICollisionShape.Unit
            && localZ >= 0 && localZ < ICollisionShape.Unit
            && localY >= 0 && localY < heightHere;
    }
}