// ============================================================================
// GAMEMANAGER.CS — Point d'entrée et orchestrateur du jeu
// ============================================================================
// Ce fichier est dans ENGINE car il utilise Godot (Node3D, AddChild...).
// C'est le CHEF D'ORCHESTRE qui crée tout et fait tourner la simulation.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Quelqu'un doit :
//   - Créer le monde (GameWorld, Chunk)
//   - Créer le joueur (Entity avec PlayerBrain)
//   - Créer les systèmes (CommandBuffer, PlayerController)
//   - Créer le rendu (ChunkRenderer)
//   - Faire tourner la boucle de jeu
//
// C'est GameManager. Il connecte Core (simulation) et Engine (rendu/input).
//
// ============================================================================
// LA BOUCLE DE JEU
// ============================================================================
// Godot appelle deux méthodes en boucle :
//
//   _Process(delta) — Chaque FRAME (60 FPS, variable)
//     → Rendu, animations, input réactif
//     → PlayerController lit les touches ici
//
//   _PhysicsProcess(delta) — À intervalle FIXE (60 fois/seconde)
//     → Simulation déterministe
//     → Commands exécutées, entités déplacées
//
// POURQUOI DEUX BOUCLES ?
//   La simulation doit être STABLE (même vitesse partout).
//   Le rendu peut varier (30 FPS sur un vieux PC, 144 sur un bon).
//   On les sépare pour que la simulation soit toujours identique.
//
// ============================================================================
// FLUX D'UNE FRAME COMPLÈTE
// ============================================================================
//   1. _Process : PlayerController lit "flèche gauche" → MoveCommand créée
//   2. _PhysicsProcess :
//      a. CommandBuffer.ExecuteAll() → Intent.MoveX = -1000
//      b. Brain.Think() → (rien pour le joueur)
//      c. Position += Intent → entité se déplace
//      d. Intent.Clear() → prêt pour le prochain tick
//   3. Godot dessine la frame
//   4. Répéter
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// Core (simulation) :
//   - GameWorld.cs : le monde avec ses chunks
//   - Entity.cs : le joueur
//   - CommandBuffer.cs : les commands en attente
//   - PlayerBrain.cs : le cerveau du joueur
//
// Engine (rendu/input) :
//   - ChunkRenderer.cs : affiche le chunk
//   - PlayerController.cs : capture les inputs
// ============================================================================

using System.Collections.Generic;
using Godot;
using ProjetColony2.Core.Commands;
using ProjetColony2.Core.Data;
using ProjetColony2.Core.Entities;
using ProjetColony2.Core.Systems;
using ProjetColony2.Core.World;
using ProjetColony2.Engine.Rendering;

namespace ProjetColony2.Engine;

public partial class GameManager : Node3D
{
    // ========================================================================
    // CHAMPS — Tout ce que GameManager gère
    // ========================================================================
    //
    // MONDE :
    //   _world : le monde avec tous ses voxels
    //   _chunks : dictionnaire des chunks chargés (clé = position cx,cy,cz)
    //   _chunkRenderers : liste des renderers (un par chunk)
    //
    // JOUEUR :
    //   _gravity : vitesse de chute (modifiable par zone/effet)
    //   _playerEntity : l'entité du joueur (position, intent, brain)
    //   _playerController : capture les inputs, crée les commands
    //   _commandBuffer : file d'attente des commands
    //   _playerRenderer : affiche le joueur (cube rouge)
    //   _playerCamera : caméra FPS qui suit le joueur
    //
    // POURQUOI UN DICTIONNAIRE POUR LES CHUNKS ?
    //   On accède aux chunks par leur position : _chunks[(1, 0, 2)]
    //   Un dictionnaire permet un accès rapide O(1).
    //   La clé est un tuple (cx, cy, cz) = coordonnées du chunk.
    //
    // POURQUOI UNE LISTE POUR LES RENDERERS ?
    //   On doit parcourir tous les renderers pour trouver celui à rebuild.
    //   Futur : utiliser aussi un dictionnaire pour accès direct.
    //
    // Le underscore "_" indique que ce sont des champs privés.
    // "private" = seulement GameManager peut y accéder.
    private GameWorld _world;
    private Dictionary<(int, int, int), Chunk> _chunks;
    private List<MaterialDefinition> _materials;
    private List<ChunkRenderer> _chunkRenderers;

    private int _gravity = MovementSystem.Gravity;
    private Entity _playerEntity;
    private EntityRenderer _playerRenderer;
    private CommandBuffer _commandBuffer;
    private PlayerController _playerController;
    private PlayerCamera _playerCamera;

    // ========================================================================
    // _READY — Appelé automatiquement par Godot au démarrage
    // ========================================================================
    // Quand tu lances le jeu (F5), Godot fait dans l'ordre :
    //   1. Charge la scène (Main.tscn)
    //   2. Crée les nœuds définis dans la scène
    //   3. Appelle _Ready() sur chaque nœud qui a cette méthode
    //
    // "override" : On REMPLACE la méthode _Ready() héritée de Node3D.
    //              Node3D a une _Ready() vide, on met notre code dedans.
    //
    // C'est comme Start() dans Unity, ou window.onload en JavaScript.
    //
    // ========================================================================
    // ÉTAPES DANS NOTRE _READY
    // ========================================================================
    //
    // ÉTAPE 1 : Créer le monde
    //   _world = new GameWorld();
    //   → Crée un monde vide (aucun chunk, aucun voxel)
    //
    // ÉTAPE 2 : Remplir un sol de pierre
    //   On parcourt x de 0 à 15, z de 0 à 15 (16×16 = 256 blocs)
    //   Pour chaque position, on met un voxel de pierre (MaterialId = 1)
    //   y = 0 car on veut un SOL (le niveau le plus bas)
    //
    // ÉTAPE 3 : Récupérer le chunk
    //   Les voxels qu'on a placés sont dans le chunk (0, 0, 0)
    //   GetOrCreateChunk retourne ce chunk
    //
    // ÉTAPE 4 : Créer et initialiser le renderer
    //   new ChunkRenderer() → crée l'objet
    //   Initialize(chunk) → lui donne les données à afficher
    //   AddChild() → l'ajoute à la scène pour qu'il soit visible
    //
    // ÉTAPE 5 : Créer le système de commandes
    //   _commandBuffer = new CommandBuffer();
    //   → File d'attente où seront stockées les commands du joueur
    //
    // ÉTAPE 6 : Créer le joueur
    //   _playerEntity = new Entity(1, new PlayerBrain());
    //   → Entité avec Id=1 et un cerveau de joueur (vide, attend les commands)
    //   → Position au centre du chunk, 1 bloc au-dessus du sol
    //
    // ÉTAPE 7 : Créer le controller
    //   _playerController = new PlayerController();
    //   → Capture les inputs clavier
    //   → Initialize() le connecte à l'entité et au buffer
    //   → AddChild() pour qu'il reçoive _Process() de Godot
    //
    // ÉTAPE 8 : Créer le rendu du joueur
    //   _playerRenderer = new EntityRenderer();
    //   → Crée un cube visible pour représenter le joueur
    //   → Initialize() le connecte à l'entité joueur
    //   → AddChild() pour qu'il soit visible dans la scène
    //
    // ÉTAPE 9 : Créer la caméra FPS
    //   _playerCamera = new PlayerCamera();
    //   → Caméra qui suit la position du joueur
    //   → Gère le regard avec la souris
    //   → AddChild() pour qu'elle soit active (Godot utilise
    //     automatiquement la première Camera3D dans la scène)
    //
    // ORDRE IMPORTANT :
    //   On ne peut pas afficher un monde qui n'existe pas.
    //   On ne peut pas contrôler un joueur qui n'existe pas.
    //   Chaque étape dépend des précédentes.
    public override void _Ready()
    {
        _materials = DataLoader.LoadMaterials("Data/materials.json");
        _world = new GameWorld();

        // ====================================================================
        // CRÉATION DU TERRAIN — Grille de chunks
        // ====================================================================
        // On crée une grille de 3×3 chunks (9 au total).
        // Chaque chunk fait 16×16×16 blocs.
        // Total : 48×16×48 blocs de terrain.
        //
        // DICTIONNAIRE _chunks :
        //   Stocke les chunks par leur position.
        //   Clé : tuple (cx, cy, cz) = coordonnées du chunk
        //   Valeur : le Chunk lui-même
        //
        // LISTE _chunkRenderers :
        //   Stocke les renderers dans l'ordre de création.
        //   On en a besoin pour rebuild après minage/placement.
        //
        // POSITION DU RENDERER :
        //   Chunk (0,0,0) → position (0, 0, 0)
        //   Chunk (1,0,0) → position (16, 0, 0)
        //   Chunk (2,0,2) → position (32, 0, 32)
        //   Formule : position = (cx × 16, cy × 16, cz × 16)
        _chunks = new Dictionary<(int, int, int), Chunk>();
        _chunkRenderers = new List<ChunkRenderer>();
        
        // Taille de la grille (3×3 = 9 chunks)
        int gridSize = 3;
        
        for (int cx = 0; cx < gridSize; cx++)
        {
            for (int cz = 0; cz < gridSize; cz++)
            {
                int cy = 0;  // Un seul niveau de chunks pour l'instant
                
                // Créer le chunk
                Chunk chunk = _world.GetOrCreateChunk(cx, cy, cz);
                
                // Générer le terrain
                WorldGenerator.GenerateChunk(chunk, cx, cy, cz);
                
                // Stocker le chunk
                _chunks[(cx, cy, cz)] = chunk;
                
                // Créer le renderer
                ChunkRenderer renderer = new ChunkRenderer();
                renderer.Initialize(chunk, _materials);
                
                // Positionner le renderer dans le monde
                renderer.Position = new Vector3(
                    cx * Chunk.Size,
                    cy * Chunk.Size,
                    cz * Chunk.Size
                );
                
                AddChild(renderer);
                _chunkRenderers.Add(renderer);
            }
        };

        // ====================================================================
        // CRÉATION DU JOUEUR
        // ====================================================================
        // 1. CommandBuffer : où stocker les commands
        // 2. Entity : le joueur avec son Id (1) et son cerveau (PlayerBrain)
        // 3. Position : au centre du chunk, 1 bloc au-dessus du sol
        //
        // POSITION EN FIXED POINT :
        //   8000 = 8 blocs (centre du chunk 16×16)
        //   1000 = 1 bloc au-dessus du sol (Y=0)
        _commandBuffer = new CommandBuffer();
        _playerEntity = new Entity(1, new PlayerBrain());
        
        int centerX = (3 * Chunk.Size / 2) * 1000;
        int centerZ = (3 * Chunk.Size / 2) * 1000;
        int spawnY = (WorldGenerator.MaxHeight + 2) * 1000;

        _playerEntity.PositionX = centerX;  // Centre du chunk (8 blocs)
        _playerEntity.PositionY = spawnY;  // 1 bloc au-dessus du sol
        _playerEntity.PositionZ = centerZ;

        // ====================================================================
        // CRÉATION DU CONTROLLER
        // ====================================================================
        // Le PlayerController est un Node Godot (pour recevoir _Process).
        // On l'initialise avec l'entité et le buffer.
        // AddChild() l'ajoute à l'arbre de scène pour qu'il reçoive les events.
        _playerController = new PlayerController();
        _playerController.Initialize(_playerEntity, _commandBuffer, _world);
        AddChild(_playerController);

        // ====================================================================
        // CRÉATION DU RENDU JOUEUR
        // ====================================================================
        // EntityRenderer affiche un cube qui suit l'entité.
        // Pour l'instant c'est un simple cube blanc.
        // Plus tard : vrai modèle 3D, animations, etc.
        //_playerRenderer = new EntityRenderer();
        //_playerRenderer.Initialize(_playerEntity);
        //AddChild(_playerRenderer);

        // ====================================================================
        // CRÉATION DE LA CAMÉRA
        // ====================================================================
        // PlayerCamera suit le joueur et gère le regard (souris).
        // C'est une Camera3D, donc Godot l'utilise automatiquement
        // pour le rendu dès qu'elle est dans la scène.
        _playerCamera = new PlayerCamera();
        _playerCamera.Initialize(_playerEntity);
        AddChild(_playerCamera);
    }

    // ========================================================================
    // _PHYSICSPROCESS — Simulation à intervalle fixe
    // ========================================================================
    // Appelée 60 fois par seconde (par défaut), peu importe le FPS.
    // C'est ici qu'on fait avancer la simulation d'UN TICK.
    //
    // ÉTAPES DU TICK :
    //   1. ExecuteAll : les commands remplissent les Intents
    //   2. Brain.Think : l'IA réfléchit (rien pour le joueur)
    //   3. Mouvement : on applique l'Intent à la position
    //   4. Clear : on nettoie pour le prochain tick
    //
    // POURQUOI DANS CET ORDRE ?
    //   - Les commands doivent être exécutées AVANT le mouvement
    //   - Le cerveau peut modifier l'Intent APRÈS les commands
    //   - Le mouvement utilise l'Intent FINAL
    //   - Clear prépare le prochain tick
    //
    // NOTE SUR LE MOUVEMENT :
    // Le joueur appuie sur "avancer" → intentZ = -1000
    // Mais "avancer" dépend de où il REGARDE (RotationY en degrés).
    //
    //      CONVERSION :
    //   Entity.RotationY = degrés (int, pour lockstep)
    //   angleRad = radians (float, pour sin/cos)
    //
    //      FORMULE DE ROTATION 2D :
    //   newX = x * cos(angle) - z * sin(angle)
    //   newZ = x * sin(angle) + z * cos(angle)
    //
    // Ceci transforme une direction RELATIVE (avant/arrière/gauche/droite)
    // en direction ABSOLUE dans le monde.
    public override void _PhysicsProcess(double delta)
    {
        // 1. Exécuter les commands en attente
        _commandBuffer.ExecuteAll(_playerEntity);

        // 2. Le cerveau réfléchit (ne fait rien pour PlayerBrain)
        _playerEntity.Brain.Think(_playerEntity, _world);

        // 3. Appliquer le mouvement (transformé selon le regard)
        int intentX = _playerEntity.Intent.MoveX;
        int intentZ = _playerEntity.Intent.MoveZ;

        float angleRad = Mathf.DegToRad(-_playerEntity.RotationY);
        float sin = Mathf.Sin(angleRad);
        float cos = Mathf.Cos(angleRad);

        int moveX = (int)(intentX * cos - intentZ * sin) * _playerEntity.Speed / 1000;
        int moveZ = (int)(intentX * sin + intentZ * cos) * _playerEntity.Speed / 1000;

        MovementSystem.ApplyMovement(_playerEntity, _world, moveX, moveZ);
        MovementSystem.ApplyJump(_playerEntity, _world);
        MovementSystem.ApplyGravity(_playerEntity, _world, _gravity);

        // ====================================================================
        // MINAGE — Casser le bloc visé
        // ====================================================================
        // Si l'entité veut miner, on remplace le bloc par de l'air.
        // Puis on reconstruit le mesh du chunk pour voir le changement.
        if (_playerEntity.Intent.WantsToMine)
        {
            int x = _playerEntity.Intent.TargetBlockX;
            int y = _playerEntity.Intent.TargetBlockY;
            int z = _playerEntity.Intent.TargetBlockZ;

            _world.SetVoxel(x, y, z, Voxel.Air);
            RebuildChunkAt(x, y, z);
        }

        // ====================================================================
        // MINAGE — Progression et cassage du bloc
        // ====================================================================
        // MiningSystem gère la progression. Retourne true si le bloc est cassé.
        if (MiningSystem.ApplyMining(_playerEntity, _world, _materials, out int minedX, out int minedY, out int minedZ))
        {
            _world.SetVoxel(minedX, minedY, minedZ, Voxel.Air);
            RebuildChunkAt(minedX, minedY, minedZ);
        }

        // ====================================================================
        // PLACEMENT — Poser un bloc
        // ====================================================================
        // Si l'entité veut poser, on crée un nouveau voxel avec le matériau choisi.
        // Le bloc est posé dans l'air AVANT le bloc visé (previous du raycast).
        if (_playerEntity.Intent.WantsToPlace)
        {
            int x = _playerEntity.Intent.TargetBlockX;
            int y = _playerEntity.Intent.TargetBlockY;
            int z = _playerEntity.Intent.TargetBlockZ;
            byte material = _playerEntity.Intent.PlaceMaterialId;

            _world.SetVoxel(x, y, z, new Voxel(material));
            RebuildChunkAt(x, y, z);
        }

        // 4. Nettoyer les intentions pour le prochain tick
        _playerEntity.Intent.Clear();
    }

    // ========================================================================
    // REBUILDCHUNKAT — Reconstruit le chunk contenant ce bloc
    // ========================================================================
    // Quand on casse ou pose un bloc, le mesh du chunk doit être régénéré.
    // Mais on a PLUSIEURS chunks ! Il faut trouver LE BON.
    //
    // PARAMÈTRES :
    //   blockX, blockY, blockZ : coordonnées du bloc modifié (en blocs)
    //
    // ALGORITHME :
    //   1. Convertir coordonnées bloc → coordonnées chunk
    //      Ex : bloc (20, 5, 8) → chunk (1, 0, 0) car 20÷16 = 1
    //
    //   2. Chercher le chunk dans le dictionnaire
    //
    //   3. Trouver le renderer correspondant dans la liste
    //
    //   4. Appeler Rebuild() sur ce renderer
    //
    // POURQUOI LA FORMULE COMPLIQUÉE POUR LES NÉGATIFS ?
    //   bloc -5 devrait donner chunk -1, pas chunk 0.
    //   La division C# tronque vers zéro : -5 / 16 = 0 (faux !)
    //   On utilise la même astuce que dans MovementSystem.
    //
    // LIMITATION ACTUELLE :
    //   On parcourt le dictionnaire pour trouver l'index du renderer.
    //   C'est lent si on a beaucoup de chunks.
    //   Futur : utiliser un dictionnaire de renderers aussi.
    private void RebuildChunkAt(int blockX, int blockY, int blockZ)
    {
        // Trouver quel chunk contient ce bloc
        int cx = blockX >= 0 ? blockX / Chunk.Size : (blockX - Chunk.Size + 1) / Chunk.Size;
        int cy = blockY >= 0 ? blockY / Chunk.Size : (blockY - Chunk.Size + 1) / Chunk.Size;
        int cz = blockZ >= 0 ? blockZ / Chunk.Size : (blockZ - Chunk.Size + 1) / Chunk.Size;
        
        // Trouver le renderer correspondant
        var key = (cx, cy, cz);
        if (_chunks.ContainsKey(key))
        {
            // Trouver l'index du chunk
            int index = 0;
            foreach (var k in _chunks.Keys)
            {
                if (k == key) break;
                index++;
            }
            
            if (index < _chunkRenderers.Count)
            {
                _chunkRenderers[index].Rebuild();
            }
        }
    }
}