// ============================================================================
// PLAYERBRAIN.CS — Le cerveau du joueur (vide)
// ============================================================================
// Ce cerveau est VOLONTAIREMENT vide. Pourquoi ?
//
// LE JOUEUR EST DIFFÉRENT DES COLONS :
//   - Colon : le cerveau (DwarfBrain) décide quoi faire
//   - Joueur : le cerveau ne décide rien, il ATTEND les Commands
//
// FLUX DES DÉCISIONS :
//
//   COLON :
//   Brain.Think() → "j'ai faim, je vais manger" → remplit Intent
//
//   JOUEUR :
//   Brain.Think() → ne fait rien
//   Command (depuis les inputs) → remplit Intent
//
// POURQUOI AVOIR UN BRAIN VIDE QUAND MÊME ?
//   Pour que l'architecture soit uniforme. La simulation fait :
//
//   foreach (entity in entities)
//   {
//       entity.Brain.Think(entity, world);  // Marche pour tous
//       ProcessIntent(entity);
//   }
//
//   Si le joueur n'avait pas de Brain, il faudrait un cas spécial.
//   Avec un Brain vide, pas de cas spécial — c'est plus propre.
//
// "override" :
//   On REMPLACE la méthode abstraite Think() de BrainComponent.
//   C'est obligatoire, sinon le compilateur refuse.
// ============================================================================

using ProjetColony2.Core.World;

namespace ProjetColony2.Core.Entities;

public class PlayerBrain : BrainComponent
{
    // ========================================================================
    // THINK — Ne fait rien (les Commands gèrent les intentions du joueur)
    // ========================================================================
    // Corps vide volontairement.
    // Les intentions du joueur viennent des Commands, pas du cerveau.
    public override void Think(Entity entity, GameWorld world)
    {
        
    }
}