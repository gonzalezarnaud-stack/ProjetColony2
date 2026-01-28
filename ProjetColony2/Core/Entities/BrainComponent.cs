// ============================================================================
// BRAINCOMPONENT.CS — Le cerveau d'une entité (classe abstraite)
// ============================================================================
// Le cerveau décide ce que l'entité VEUT faire. Il remplit l'Intent.
//
// POURQUOI UNE CLASSE ABSTRAITE ?
//   "abstract" signifie : cette classe est INCOMPLÈTE, on ne peut pas
//   l'utiliser directement. Elle définit un CONTRAT que les enfants
//   doivent respecter.
//
//   On ne peut PAS faire : new BrainComponent()  ← Erreur !
//   On DOIT faire : new PlayerBrain() ou new DwarfBrain()
//
// C'EST QUOI LE CONTRAT ?
//   Tout cerveau DOIT avoir une méthode Think().
//   Comment Think() fonctionne, ça dépend du type de cerveau :
//     - PlayerBrain.Think() : ne fait rien (attend les Commands)
//     - DwarfBrain.Think() : cherche un job, planifie, décide
//
// ANALOGIE :
//   "BrainComponent" c'est comme dire "un véhicule".
//   Tu ne peux pas conduire "un véhicule" abstrait.
//   Tu conduis une voiture, une moto, un camion — des véhicules concrets.
//   Mais tous ont un volant et des roues (le contrat).
//
// MÉTHODE ABSTRAITE :
//   "public abstract void Think(...)" n'a PAS de corps { }.
//   C'est juste une signature qui dit : "celui qui hérite DOIT l'implémenter".
//   Si une classe hérite de BrainComponent sans implémenter Think(), 
//   le compilateur refuse.
// ============================================================================

using ProjetColony2.Core.World;

namespace ProjetColony2.Core.Entities;

public abstract class BrainComponent
{
    // ========================================================================
    // THINK — Réfléchit et remplit les intentions (à implémenter)
    // ========================================================================
    // Appelée chaque tick pour chaque entité.
    //
    // Paramètres :
    //   entity : l'entité qui possède ce cerveau (pour accéder à Intent, Position...)
    //   world : le monde (pour voir les blocs autour, trouver un chemin...)
    //
    // Ce que Think() devrait faire :
    //   Analyser la situation → Décider quoi faire → Remplir entity.Intent
    //
    // Exemples d'implémentation :
    //   PlayerBrain : ne fait rien, les Commands remplissent l'Intent
    //   DwarfBrain : regarde si j'ai un job, sinon en cherche un, etc.
    //
    // "abstract" = pas de corps ici, les enfants l'implémentent.
    public abstract void Think(Entity entity, GameWorld world);

}