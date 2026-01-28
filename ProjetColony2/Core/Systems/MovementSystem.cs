// ============================================================================
// MOVEMENTSYSTEM.CS — Gère le déplacement des entités avec collisions
// ============================================================================
// Ce fichier est dans CORE car il ne dépend pas de Godot.
// C'est de la pure logique de simulation.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Avant, GameManager appliquait le mouvement directement :
//   entity.PositionX += moveX;  // Passe à travers les murs !
//
// Maintenant, MovementSystem vérifie AVANT de bouger :
//   "Y a-t-il un bloc solide là où je veux aller ?"
//
// ============================================================================
// POURQUOI UNE CLASSE STATIQUE ?
// ============================================================================
// MovementSystem n'a pas d'ÉTAT (pas de champs).
// C'est juste une collection de MÉTHODES utilitaires.
//
// Au lieu de :
//   var system = new MovementSystem();
//   system.ApplyMovement(...);
//
// On fait directement :
//   MovementSystem.ApplyMovement(...);
//
// C'est plus simple et plus clair.
//
// ============================================================================
// CONCEPT : GLISSEMENT LE LONG DES MURS
// ============================================================================
// On vérifie X et Z SÉPARÉMENT.
//
// Exemple : le joueur fonce en diagonale dans un mur vertical.
//   - Test X : bloqué (mur)
//   - Test Z : libre
//   - Résultat : le joueur GLISSE le long du mur
//
// Si on testait X et Z ensemble :
//   - Position diagonale bloquée → arrêt complet
//   - Moins fluide, frustrant pour le joueur
//
// ============================================================================
// CONCEPT : HAUTEUR DE L'ENTITÉ
// ============================================================================
// Une entité Medium (humain/nain) fait 2 blocs de haut.
// On doit vérifier les collisions aux PIEDS et à la TÊTE.
//
// Sinon le joueur pourrait passer sous un plafond de 1 bloc !
//
// IsBlockedForEntity vérifie chaque bloc de hauteur :
//   height = 2 → vérifie Y et Y+1000 (pieds et tête)
//
// ============================================================================
// CONCEPT : DIVISION PLANCHER (FLOOR DIVISION)
// ============================================================================
// En C#, la division entière TRONQUE vers zéro :
//   7 / 3 = 2 (pas 2.33)
//   -7 / 3 = -2 (pas -3 !)
//
// Pour les coordonnées, on veut une division PLANCHER :
//   -500 devrait donner bloc -1, pas bloc 0
//
// La formule (posX - 999) / 1000 corrige ça pour les négatifs.
//
// EXEMPLES :
//   posX = 1500  → 1500 / 1000 = 1 ✓
//   posX = 500   → 500 / 1000 = 0 ✓
//   posX = -500  → (-500 - 999) / 1000 = -1499 / 1000 = -1 ✓
//   posX = -1500 → (-1500 - 999) / 1000 = -2499 / 1000 = -2 ✓
//
// ============================================================================
// LIMITATION ACTUELLE
// ============================================================================
// On peut sortir du chunk car GetVoxel retourne "air" par défaut
// pour les positions hors du monde.
//
// Futur : monde infini avec chunks générés à la volée, ou monde
// qui boucle (planète). Cette limitation disparaîtra.
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Entity.cs : l'entité qu'on déplace
// - GameWorld.cs : le monde qu'on interroge (GetVoxel)
// - Voxel.cs : on teste IsAir pour savoir si c'est traversable
// - GameManager.cs : appelle ApplyMovement dans _PhysicsProcess
// ============================================================================
using ProjetColony2.Core.Entities;
using ProjetColony2.Core.World;

namespace ProjetColony2.Core.Systems;

public static class MovementSystem
{
    // ========================================================================
    // GRAVITÉ — Vitesse de chute par défaut
    // ========================================================================
    // En unités fixed point par tick.
    // 50 unités/tick × 60 ticks/seconde = 3000 unités/seconde = 3 blocs/seconde
    //
    // "public const" = constante accessible de l'extérieur, jamais modifiable.
    // GameManager peut la lire pour initialiser sa propre variable _gravity.
    public const int Gravity = 25;

    // Force de saut par défaut (référence pour Entity.JumpForce)
    public const int JumpForce = 280;

    // ========================================================================
    // APPLYMOVEMENT — Déplace une entité en respectant les collisions
    // ========================================================================
    // PARAMÈTRES :
    //   entity : l'entité à déplacer
    //   world : le monde pour vérifier les collisions
    //   moveX, moveZ : le déplacement souhaité (en fixed point)
    //
    // ALGORITHME :
    //   1. Calculer nouvelle position X
    //   2. Vérifier collision sur toute la hauteur de l'entité
    //   3. Si pas bloqué → appliquer X
    //   4. Même chose pour Z
    //
    // POURQUOI SÉPARER X ET Z ?
    //   Permet le glissement le long des murs.
    //   Si X est bloqué mais Z est libre, on avance quand même en Z.
    //
    // HAUTEUR FIXE À 2 :
    //   Pour l'instant, on considère toutes les entités comme Medium (2 blocs).
    //   Futur : lire entity.Height depuis EntityDefinition (JSON).

    public static void ApplyMovement(Entity entity, GameWorld world, int moveX, int moveZ)
    {
        int height = 2; // Medium = 2 blocs de haut (futur : entity.Height)
        int newX = entity.PositionX + moveX;
        if (!IsBlockedForEntity(world, newX, entity.PositionY, entity.PositionZ, height))
        {
            entity.PositionX = newX;
        }

        int newZ = entity.PositionZ + moveZ;
        if (!IsBlockedForEntity(world, entity.PositionX, entity.PositionY, newZ, height))
        {
            entity.PositionZ = newZ;
        }      
    }

    // ========================================================================
    // APPLYJUMP — Déclenche un saut si conditions remplies
    // ========================================================================
    // CONDITIONS :
    //   1. L'entité VEUT sauter (WantsToJump = true)
    //   2. L'entité est AU SOL (pas déjà en l'air)
    //
    // Si les deux conditions sont vraies :
    //   → VelocityY = JumpForce (impulsion vers le haut)
    //
    // La gravité fera le reste (ralentir, puis redescendre).
    //
    // POURQUOI VÉRIFIER LE SOL ?
    //   Sans ça, le joueur pourrait sauter en l'air (double saut infini).
    //   Futur : option de double saut pour certaines entités.
    public static void ApplyJump(Entity entity, GameWorld world)
    {
        if (!entity.Intent.WantsToJump) return;

        int feetY = entity.PositionY - 1;
        bool hasGround = IsBlocked(world, entity.PositionX, feetY, entity.PositionZ);

        if (hasGround)
        {
            entity.VelocityY = entity.JumpForce;
        }
    }

    // ========================================================================
    // APPLYGRAVITY — Applique la gravité et gère les collisions verticales
    // ========================================================================
    // ALGORITHME :
    //   1. Réduire VelocityY de gravity (accélération vers le bas)
    //   2. Calculer la nouvelle position Y
    //   3. Si on DESCEND (VelocityY < 0) :
    //      - Vérifier s'il y a un sol
    //      - Si oui → atterrir, VelocityY = 0
    //   4. Si on MONTE (VelocityY > 0) :
    //      - Vérifier s'il y a un plafond
    //      - Si oui → stopper, VelocityY = 0
    //   5. Sinon → appliquer le mouvement
    //
    // HAUTEUR DE L'ENTITÉ :
    //   On vérifie le plafond à newY + (height * 1000).
    //   Une entité de 2 blocs doit vérifier 2 blocs au-dessus des pieds.
    //
    // ATTERRISSAGE :
    //   On repositionne l'entité EXACTEMENT sur le bloc.
    //   (blockY + 1) * 1000 = sommet du bloc = position des pieds.
    public static void ApplyGravity(Entity entity, GameWorld world, int gravity)
    {
        entity.VelocityY -= gravity;

        int newY = entity.PositionY + entity.VelocityY;
        

        if (entity.VelocityY < 0)
        {
            int feetY = entity.PositionY - 1;
            bool hasGround = IsBlocked(world, entity.PositionX, feetY, entity.PositionZ);

            if (hasGround)
            {
                int blockY = feetY >= 0 ? feetY / 1000 : (feetY - 999) / 1000;
                entity.PositionY = (blockY + 1) * 1000;
                entity.VelocityY = 0;
                return;
            }
        }

        if (entity.VelocityY > 0)
        {
            int height = 2;
            int headY = newY + (height * 1000);
            bool hasRoof = IsBlocked(world, entity.PositionX, headY, entity.PositionZ);
            if (hasRoof)
            {
                entity.VelocityY = 0;
                return;
            }
        }

        entity.PositionY = newY;
    }

    // ========================================================================
    // ISBLOCKEDFORENTITY — Vérifie les collisions sur toute la hauteur
    // ========================================================================
    // PARAMÈTRES :
    //   world : le monde à interroger
    //   posX, posY, posZ : position des pieds (en fixed point)
    //   height : hauteur de l'entité en blocs (2 pour Medium)
    //
    // ALGORITHME :
    //   Pour chaque bloc de hauteur (0, 1, 2...) :
    //     - Calculer la position Y de ce bloc
    //     - Vérifier si c'est bloqué
    //     - Si oui → retourner true immédiatement
    //
    // EXEMPLE (height = 2) :
    //   h = 0 → vérifie posY (pieds)
    //   h = 1 → vérifie posY + 1000 (tête)
    //
    // Si un seul niveau est bloqué, l'entité ne peut pas passer.
     private static bool IsBlockedForEntity(GameWorld world, int posX, int posY, int posZ, int height)
    {
        for (int h = 0; h < height; h++)
        {
            int checkY = posY + (h * 1000);
            if (IsBlocked(world, posX, checkY, posZ))
            {
                return true;
            }
        }
        return false;
    }

    // ========================================================================
    // ISBLOCKED — Vérifie si une position est dans un bloc solide
    // ========================================================================
    // PARAMÈTRES :
    //   world : le monde à interroger
    //   posX, posY, posZ : la position à tester (en fixed point)
    //
    // RETOUR :
    //   true = il y a un bloc solide (collision)
    //   false = c'est de l'air (passage libre)
    //
    // ÉTAPES :
    //   1. Convertir fixed point → coordonnées bloc (÷ 1000)
    //   2. Gérer les coordonnées négatives (division plancher)
    //   3. Récupérer le voxel à cette position
    //   4. Retourner true si ce n'est pas de l'air
    //
    // L'OPÉRATEUR TERNAIRE (? :) :
    //   condition ? valeurSiVrai : valeurSiFaux
    //   C'est un raccourci pour if/else sur une seule ligne.
    private static bool IsBlocked(GameWorld world, int posX, int posY, int posZ)
    {
        int blockX = posX >= 0 ? posX / 1000 : (posX - 999) / 1000;
        int blockY = posY >= 0 ? posY / 1000 : (posY - 999) / 1000;
        int blockZ = posZ >= 0 ? posZ / 1000 : (posZ - 999) / 1000;

        Voxel voxel = world.GetVoxel(blockX, blockY, blockZ);
        return !voxel.IsAir;
    }
}