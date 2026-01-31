// ============================================================================
// PLAYERCONTROLLER.CS — Capture les inputs et crée des Commands
// ============================================================================
// Ce fichier est dans ENGINE car il utilise Godot (Input, Node).
// C'est le PONT entre le joueur humain et le système de commandes.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Le joueur appuie sur des touches. Mais la simulation ne comprend pas
// "flèche gauche" — elle comprend "MoveCommand(-1000, 0)".
//
// PlayerController traduit :
//   Touches → Commands → Buffer → Simulation
//
// C'EST LE SEUL ENDROIT où on lit les inputs.
// La simulation ne sait pas que Godot existe.
//
// ============================================================================
// FLUX COMPLET (chaque frame)
// ============================================================================
//   1. Godot appelle _Process()
//   2. On lit les touches (Input.IsActionPressed)
//   3. On convertit en direction fixed point (-1000 / 0 / +1000)
//   4. Si mouvement → on crée une MoveCommand
//   5. On ajoute la command au buffer
//   6. Plus tard, la simulation exécutera les commands du buffer
//
// ============================================================================
// POURQUOI PASSER PAR DES COMMANDS ?
// ============================================================================
// On pourrait faire directement :
//   _entity.Intent.MoveX = -1000;  // ❌ Court-circuit !
//
// Mais ça casse le lockstep multijoueur :
//   - Joueur 1 modifie son Intent localement
//   - Joueur 2 ne voit rien
//   - Désynchronisation !
//
// Avec les Commands :
//   - Joueur 1 crée une MoveCommand
//   - La command est envoyée à tous (futur)
//   - Tout le monde l'exécute au même tick
//   - Synchronisé !
//
// ============================================================================
// CONCEPTS : _PROCESS VS _PHYSICSPROCESS
// ============================================================================
// Godot a deux boucles :
//   - _Process(delta) : appelée chaque FRAME (60 FPS, variable)
//   - _PhysicsProcess(delta) : appelée à intervalle FIXE (par défaut 60/s)
//
// On utilise _Process car on veut des inputs RÉACTIFS.
// La simulation sera dans _PhysicsProcess (plus tard).
//
// "delta" = temps écoulé depuis la dernière frame (en secondes).
// On l'ignore car notre simulation utilise des ticks, pas le temps réel.
//
// ============================================================================
// PIÈGES À ÉVITER
// ============================================================================
// - Ne JAMAIS modifier l'Intent directement ici (toujours via Command)
// - Ne pas oublier d'appeler Initialize() avant d'utiliser le controller
// - Vérifier que moveX OU moveZ != 0 avant de créer une command
//   (sinon on spam des commands "immobile" inutiles)
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Entity.cs : on a besoin de l'Id pour créer les commands
// - CommandBuffer.cs : on y ajoute les commands
// - MoveCommand.cs : la command qu'on crée
// - (Futur) GameManager : créera et initialisera le PlayerController
// ============================================================================

namespace ProjetColony2.Engine;

using Godot;
using ProjetColony2.Core.Entities;
using ProjetColony2.Core.Commands;
using ProjetColony2.Core.Systems;
using ProjetColony2.Core.World;
using System;

public partial class PlayerController : Node
{
    private Entity _entity;
    private CommandBuffer _commandBuffer;
    private GameWorld _world; // Le monde, pour le raycast (savoir où sont les blocs)

    // ========================================================================
    // INITIALIZE — Configure le controller
    // ========================================================================
    // Appelée UNE FOIS après la création, avant la première frame.
    //
    // POURQUOI PAS LE CONSTRUCTEUR ?
    //   En Godot, on crée le node puis on l'initialise séparément.
    //   Le constructeur est appelé trop tôt (pas encore dans l'arbre).
    //
    // PARAMÈTRES :
    //   entity : l'entité que ce joueur contrôle
    //   commandBuffer : le buffer partagé où stocker les commands
    //   world : le monde, pour le raycast (trouver quel bloc on vise)
    public void Initialize(Entity entity, CommandBuffer commandBuffer, GameWorld world)
    {
        _entity = entity;
        _commandBuffer = commandBuffer;
        _world = world;

    }

    // ========================================================================
    // _PROCESS — Appelée chaque frame par Godot
    // ========================================================================
    // C'est ici qu'on lit les inputs et qu'on crée les commands.
    //
    // PARAMÈTRE :
    //   delta : temps écoulé depuis la dernière frame (ignoré)
    //
    // ALGORITHME :
    //   1. Lire l'état des touches de direction
    //   2. Convertir en valeurs fixed point (-1000 / 0 / +1000)
    //   3. Si mouvement détecté → créer et stocker une MoveCommand
    //
    // POURQUOI DES IF SÉPARÉS (pas else if) ?
    //   Le joueur peut appuyer sur DEUX touches en même temps.
    //   Gauche + Haut = diagonale (moveX = -1000, moveZ = -1000)
    //   Avec else if, on ne capturerait qu'une seule direction.
    //
    // POURQUOI VÉRIFIER moveX != 0 || moveZ != 0 ?
    //   Si le joueur n'appuie sur rien, pas besoin de command.
    //   On évite de spammer le buffer avec des "commandes immobiles".
    public override void _Process(double delta)
    {
        InputHelper.Update();
        int moveX = 0;
        int moveZ = 0;

        if (Input.IsActionPressed("ui_left"))
        {
            moveX = -1000;
        }
        if (Input.IsActionPressed("ui_right"))
        {
            moveX = 1000;
        }
        if (Input.IsActionPressed("ui_up"))
        {
            moveZ = -1000;
        }
        if (Input.IsActionPressed("ui_down"))
        {
            moveZ = 1000;
        }

        if(moveX != 0 || moveZ != 0)
        {
            _commandBuffer.Add(new MoveCommand(_entity.Id, moveX, moveZ));
        }

        // ====================================================================
        // SAUT — Détecte l'appui sur Espace
        // ====================================================================
        // IsActionJustPressed = vrai UNE SEULE frame au moment de l'appui.
        // Contrairement à IsActionPressed qui reste vrai tant qu'on appuie.
        //
        // Si on utilisait IsActionPressed, le joueur créerait 60 JumpCommands
        // par seconde tant qu'il maintient Espace !
        //
        // "ui_accept" = action par défaut de Godot (Espace/Entrée).
        // Futur : créer une action "jump" personnalisée.
        if (Input.IsActionJustPressed("ui_accept"))
        {
            _commandBuffer.Add(new JumpCommand(_entity.Id));
        }

        // ====================================================================
        // MINAGE ET PLACEMENT — Casser ou poser un bloc
        // ====================================================================
        // Quand le joueur clique, on doit :
        //   1. Savoir OÙ il regarde (raycast)
        //   2. Créer la commande appropriée (Mine ou Place)
        //
        // CLIC GAUCHE = casser le bloc visé
        // CLIC DROIT = poser un bloc à côté du bloc visé
        if (InputHelper.IsMouseButtonPressed(MouseButton.Left) || InputHelper.IsMouseButtonJustPressed(MouseButton.Right))
        {
            // ================================================================
            // POSITION DES YEUX — D'où part le rayon
            // ================================================================
            // Le rayon part des YEUX du joueur, pas de ses pieds.
            // On convertit la position fixed point (×1000) en float.
            // On ajoute 1.5 bloc en Y pour la hauteur des yeux.
            //
            // EXEMPLE :
            //   PositionX = 5000 → eyeX = 5.0
            //   PositionY = 1000 → eyeY = 1.0 + 1.5 = 2.5
            float eyeX = _entity.PositionX / 1000f;
            float eyeY = _entity.PositionY / 1000f + 1.5f;
            float eyeZ = _entity.PositionZ / 1000f;

            // ================================================================
            // DIRECTION DU REGARD — Où va le rayon
            // ================================================================
            // On a deux angles :
            //   - RotationY : gauche/droite (0° à 360°)
            //   - RotationX : haut/bas (-90° à +90°)
            //
            // Il faut convertir ces angles en VECTEUR DIRECTION (dirX, dirY, dirZ).
            // C'est de la trigonométrie 3D.
            //
            // CONVERSION DEGRÉS → RADIANS :
            //   Les fonctions Sin/Cos de C# utilisent des radians, pas des degrés.
            //   Formule : radians = degrés × π / 180
            //
            // POURQUOI -RotationY ?
            //   Même raison que pour le mouvement : notre système de coordonnées
            //   est inversé par rapport à la convention mathématique standard.
            float angleY = -_entity.RotationY * (MathF.PI / 180f);
            float angleX = -_entity.RotationX * (MathF.PI / 180f);

            // ================================================================
            // FORMULES TRIGONOMÉTRIQUES — Angle → Direction
            // ================================================================
            // C'est la partie mathématique. Pas besoin de tout comprendre !
            //
            // INTUITION :
            //   - Si on regarde droit (angleX = 0) : dirY = 0 (pas de haut/bas)
            //   - Si on regarde en haut (angleX > 0) : dirY > 0 (rayon monte)
            //   - Si on regarde en bas (angleX < 0) : dirY < 0 (rayon descend)
            //
            // Le Cos(angleX) "aplatit" dirX et dirZ quand on regarde en haut/bas.
            // Exemple : si on regarde pile vers le ciel, dirX = dirZ = 0.
            //
            // POURQUOI -dirZ ?
            //   Dans Godot, -Z = devant, +Z = derrière.
            //   Sans le moins, le rayon irait à l'opposé de où on regarde !
            float dirX = MathF.Cos(angleX) * MathF.Sin(angleY);
            float dirY = -MathF.Sin(angleX);
            float dirZ = -MathF.Cos(angleX) * MathF.Cos(angleY);

            // ================================================================
            // RAYCAST — Trouver le bloc visé
            // ================================================================
            // On lance un rayon invisible depuis les yeux dans la direction du regard.
            // Le raycast retourne :
            //   - found : true si un bloc solide a été touché
            //   - hitBlockX/Y/Z : coordonnées du bloc touché
            //   - previousX/Y/Z : coordonnées du bloc AVANT (pour poser)
            //   - hitPointX/Y/Z : point exact d'impact (futur : sous-grille)
            bool found = RaycastSystem.TryGetTargetBlock(
                _world,
                eyeX, eyeY, eyeZ,
                dirX, dirY, dirZ,
                out int hitBlockX, out int hitBlockY, out int hitBlockZ,
                out int previousX, out int previousY, out int previousZ,
                out float hitPointX, out float hitPointY, out float hitPointZ
            );

            // ================================================================
            // CRÉATION DES COMMANDES
            // ================================================================
            // Si on a trouvé un bloc, on crée la commande appropriée.
            //
            // CLIC GAUCHE → MineCommand
            //   On veut casser le bloc TOUCHÉ (hitBlock).
            //
            // CLIC DROIT → PlaceCommand
            //   On veut poser dans le bloc AVANT (previous).
            //   C'est le bloc d'air juste avant le bloc solide.
            //   Le "1" est le MaterialId (pierre) — temporaire, futur : inventaire.
            if (found)
            {
                if (InputHelper.IsMouseButtonPressed(MouseButton.Left))
                {
                    _commandBuffer.Add(new MiningCommand(_entity.Id, hitBlockX, hitBlockY, hitBlockZ));
                }

                if (InputHelper.IsMouseButtonJustPressed(MouseButton.Right))
                {
                    _commandBuffer.Add(new PlaceCommand(_entity.Id, previousX, previousY, previousZ, 1));
                }
            }
        }
    }
}