// ============================================================================
// ENTITY.CS — Une entité dans le monde (joueur, colon, animal...)
// ============================================================================
// Une entité = n'importe quoi qui EXISTE et AGIT dans le monde.
// Le joueur, un colon, un gobelin, un mouton — tous sont des Entity.
//
// POURQUOI UNE SEULE CLASSE ?
//   Au lieu d'avoir Player.cs, Colon.cs, Animal.cs séparés (avec du code
//   dupliqué), on a UNE classe Entity. Ce qui différencie un joueur d'un
//   colon, c'est son CERVEAU (Brain), pas sa structure.
//
// CE QUE CONTIENT UNE ENTITY :
//   - Id : identifiant unique (pour retrouver l'entité)
//   - Position : où elle est dans le monde (fixed point ×1000)
//   - Intent : ce qu'elle veut faire ce tick
//   - Brain : qui prend les décisions (joueur ou IA)
//
// COMMENT ÇA MARCHE (chaque tick) :
//   1. Brain.Think() est appelé (pour les colons, décide quoi faire)
//   2. Pour le joueur, les Commands remplissent l'Intent
//   3. La simulation lit l'Intent et modifie Position
//   4. Intent.Clear() pour le prochain tick
//
// FIXED POINT POUR LA POSITION :
//   PositionX = 5000 signifie X = 5.000 (5 blocs depuis l'origine)
//   PositionX = 5500 signifie X = 5.500 (5 blocs et demi)
//   On a une précision de 1/1000e de bloc, sans utiliser de floats.
// ============================================================================

namespace ProjetColony2.Core.Entities;

public class Entity
{
    // ========================================================================
    // IDENTIFIANT UNIQUE
    // ========================================================================
    // Chaque entité a un numéro différent : 1, 2, 3...
    // Utilisé pour :
    //   - Retrouver une entité dans une liste
    //   - Identifier qui envoie une Command
    //   - Synchroniser en multijoueur ("l'entité 7 bouge")
    public int Id;

    // ========================================================================
    // POSITION DANS LE MONDE — Fixed point ×1000
    // ========================================================================
    // La position est en millièmes de bloc (1 bloc = 1000 unités).
    //
    // Exemples :
    //   PositionX = 0       → X = 0 (origine)
    //   PositionX = 5000    → X = 5 blocs
    //   PositionX = 5500    → X = 5.5 blocs (au milieu d'un bloc)
    //   PositionX = -3000   → X = -3 blocs (derrière l'origine)
    //
    // POURQUOI PAS UN SEUL OBJET "Position" ?
    //   On pourrait avoir une struct Position { X, Y, Z }.
    //   Pour l'instant on garde simple avec 3 champs séparés.
    //   On pourra refactorer plus tard si besoin.
    public int PositionX;
    public int PositionY;
    public int PositionZ;

    // ========================================================================
    // ROTATION — Direction où regarde l'entité (en degrés)
    // ========================================================================
    // Valeur de 0 à 359 degrés.
    //   0 = vers -Z (Nord)
    //   90 = vers +X (Est)
    //   180 = vers +Z (Sud)
    //   270 = vers -X (Ouest)
    //
    // Stocké en int pour le déterminisme (lockstep).
    // Converti en radians seulement dans Engine pour les calculs Godot.
    public int RotationY = 0;

    // ========================================================================
    // ROTATION VERTICALE — Où regarde l'entité (haut/bas)
    // ========================================================================
    // Valeur de -90 à +90 degrés.
    //   -90 = regarde tout en bas (ses pieds)
    //   0 = regarde droit devant
    //   +90 = regarde tout en haut (le ciel)
    //
    // Utilisé par :
    //   - PlayerCamera : met à jour cette valeur (souris)
    //   - PlayerController : calcule la direction du raycast
    public int RotationX = 0; 

    // ========================================================================
    // VITESSE — Unités de déplacement par tick
    // ========================================================================
    // 100 = 0.1 bloc par tick = 6 blocs par seconde (à 60 ticks/s)
    // 
    // CALCUL :
    //   Intent.MoveX (1000 max) × Speed (100) / 1000 = 100 unités/tick
    //
    // FUTUR :
    //   Cette valeur viendra d'un JSON (EntityDefinition).
    //   Chaque type d'entité aura sa propre vitesse.
    //   Un humain, un nain, un ogre → vitesses différentes.
    public int Speed = 100;

    // ========================================================================
    // VÉLOCITÉ VERTICALE — Pour le saut et la chute
    // ========================================================================
    // Positif = monte (saut)
    // Négatif = descend (chute)
    // Zéro = immobile verticalement
    //
    // Chaque tick :
    //   1. ApplyJump met VelocityY = JumpForce si on saute
    //   2. ApplyGravity réduit VelocityY de Gravity
    //   3. VelocityY est appliquée à PositionY
    //
    // Exemple de saut (JumpForce=280, Gravity=25) :
    //   Tick 0 : VelocityY = 280 (impulsion)
    //   Tick 1 : VelocityY = 255, monte
    //   ...
    //   Tick 11 : VelocityY = 5, presque au sommet
    //   Tick 12 : VelocityY = -20, redescend
    //   ...
    public int VelocityY = 0;

    // ========================================================================
    // FORCE DE SAUT — Impulsion initiale vers le haut
    // ========================================================================
    // Plus la valeur est grande, plus le saut est haut.
    // 280 = environ 1.5 blocs de hauteur.
    //
    // Contrairement à VelocityY qui change chaque tick,
    // JumpForce est une CARACTÉRISTIQUE de l'entité (constante).
    //
    // Futur : défini par EntityDefinition (JSON).
    // Un lapin aura JumpForce = 400, un ogre = 200.
    public int JumpForce = 280;

    // ========================================================================
    // INTENTIONS — Ce que l'entité veut faire
    // ========================================================================
    // Voir IntentComponent.cs pour les détails.
    // Rempli par les Commands (joueur) ou le Brain (colon).
    public IntentComponent Intent;

    // ========================================================================
    // CERVEAU — Qui prend les décisions ?
    // ========================================================================
    // Le Brain décide ce que l'entité VEUT faire.
    //
    // JOUEUR (PlayerBrain) :
    //   Think() ne fait rien. Les décisions viennent des Commands
    //   envoyées par le PlayerController (clavier/souris).
    //
    // COLON (DwarfBrain, plus tard) :
    //   Think() regarde s'il a un job, sinon en cherche un.
    //   Il décide seul, pas besoin d'input humain.
    //
    // ANIMAL (AnimalBrain, futur) :
    //   Think() gère les comportements : fuir, manger, dormir...
    //
    // On passe le Brain au constructeur car une entité DOIT avoir un cerveau.
    // Pas de cerveau = pas de décisions = entité inutile.
    public BrainComponent Brain;

    // ========================================================================
    // CONSTRUCTEUR — Crée une nouvelle entité
    // ========================================================================
    // Appelé quand on écrit : new Entity(1, new PlayerBrain())
    //
    // Paramètres :
    //   id : numéro unique de l'entité
    //   brain : le cerveau qui prendra les décisions
    //
    // Ce qui est initialisé :
    //   - Id : stocké pour identification
    //   - Intent : créé vide, prêt à recevoir des intentions
    //   - Brain : le cerveau passé en paramètre
    //   - Position : reste à (0, 0, 0), sera défini après création
    public Entity (int id, BrainComponent brain)
    {
        Id = id;
        Intent = new IntentComponent();
        Brain = brain;
    }
}