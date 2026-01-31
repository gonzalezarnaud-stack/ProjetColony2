// ============================================================================
// ENTITYRENDERER.CS — Affiche une entité à l'écran
// ============================================================================
// Ce fichier est dans ENGINE car il utilise Godot (Node3D, MeshInstance3D).
// Il fait le PONT entre les données (Entity dans Core) et le visuel (Godot).
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// L'Entity dans Core ne sait pas s'afficher. Elle ne connaît que :
//   - Sa position (en fixed point)
//   - Ses intentions
//   - Son cerveau
//
// EntityRenderer s'occupe de :
//   - Créer un mesh visible (cube pour l'instant)
//   - Suivre la position de l'Entity
//   - Convertir fixed point → coordonnées Godot
//
// SÉPARATION CORE / ENGINE :
//   Entity (Core) : "Je suis en position (8000, 1000, 8000)"
//   EntityRenderer (Engine) : "Je dessine un cube en (8, 1, 8)"
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// 1. GameManager crée l'EntityRenderer
// 2. Initialize() reçoit l'Entity à suivre et crée le mesh
// 3. Chaque frame, _Process() :
//    a. Lit la position de l'Entity (fixed point)
//    b. Convertit en float (÷ 1000)
//    c. Met à jour la position du Node3D
//
// Le cube "suit" l'Entity automatiquement.
//
// ============================================================================
// CONCEPTS : HÉRITAGE DE NODE3D
// ============================================================================
// EntityRenderer HÉRITE de Node3D, donc :
//   - C'est un objet 3D avec une Position dans l'espace
//   - Il peut avoir des enfants (notre MeshInstance3D)
//   - Godot appelle automatiquement _Process() chaque frame
//
// HIÉRARCHIE DANS LA SCÈNE :
//   GameManager
//     └── EntityRenderer (Node3D avec Position)
//           └── MeshInstance3D (le cube visible)
//
// Quand on change EntityRenderer.Position, le mesh suit automatiquement
// car il est ENFANT du renderer.
//
// ============================================================================
// CONCEPTS : FIXED POINT → FLOAT
// ============================================================================
// Core utilise des int (×1000) pour le déterminisme.
// Godot utilise des float pour les positions 3D.
//
// Conversion :
//   int fixedPoint = 8500;          // 8.5 blocs en fixed point
//   float godot = fixedPoint / 1000f;  // 8.5f en coordonnées Godot
//
// Le "f" après 1000 est IMPORTANT :
//   8500 / 1000 = 8 (division entière, on perd le .5)
//   8500 / 1000f = 8.5f (division flottante, précis)
//
// ============================================================================
// ÉVOLUTION FUTURE
// ============================================================================
// Pour l'instant : un simple cube blanc.
// Plus tard :
//   - Charger un vrai modèle 3D
//   - Couleur différente selon le type d'entité
//   - Animations (marche, attaque...)
//   - Interpolation pour un mouvement fluide
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Entity.cs : l'entité qu'on suit (position, données)
// - GameManager.cs : crée et initialise l'EntityRenderer
// - (Futur) Interpolation : lissera le mouvement entre les ticks
// ============================================================================

using Godot;
using ProjetColony2.Core.Entities;

namespace ProjetColony2.Engine.Rendering;

public partial class EntityRenderer : Node3D
{
    // ========================================================================
    // CHAMPS — Ce que le renderer garde en mémoire
    // ========================================================================
    // _entity : l'entité à suivre
    //   On lit sa position chaque frame pour mettre à jour le visuel.
    //
    // _meshInstance : le cube 3D visible
    //   Stocké au cas où on voudrait le modifier plus tard
    //   (changer de couleur, de taille, le cacher...).
    private Entity _entity;
    private MeshInstance3D _mesh;

    // ========================================================================
    // INITIALIZE — Configure le renderer avec une entité à suivre
    // ========================================================================
    // Appelée UNE FOIS après la création.
    //
    // CE QUI SE PASSE :
    //   1. On stocke l'entité pour pouvoir lire sa position plus tard
    //   2. On crée un BoxMesh (la FORME géométrique d'un cube)
    //   3. On crée un MeshInstance3D (l'OBJET qui affiche la forme)
    //   4. On ajoute le mesh comme enfant pour qu'il soit visible
    //
    // BOXMESH VS MESHINSTANCE3D :
    //   BoxMesh = la recette du cube (taille, proportions)
    //   MeshInstance3D = le cube réel dans la scène
    //   
    //   Plusieurs MeshInstance3D peuvent partager le même BoxMesh.
    //   C'est comme : un plan d'architecte (Mesh) vs les maisons construites (Instance).
    //
    // ADDCHILD :
    //   Ajoute le mesh comme ENFANT de EntityRenderer.
    //   Le mesh hérite de la position du parent.
    //   Quand on bouge EntityRenderer, le mesh suit automatiquement.
    public void Initialize(Entity entity)
    {
        _entity = entity;
        
        BoxMesh box = new BoxMesh();
        box.Size = new Vector3(1, 1, 1);

        // ====================================================================
        // MATÉRIAU — Couleur du cube
        // ====================================================================
        // Sans matériau, le cube est blanc/gris et se confond avec le terrain.
        // On lui donne une couleur vive (rouge) pour le repérer facilement.
        //
        // StandardMaterial3D : matériau basique de Godot
        // AlbedoColor : la couleur "de base" de la surface
        // Color(R, G, B) : rouge=1, vert=0, bleu=0 → rouge pur
        //
        // Plus tard : charger un vrai modèle 3D avec ses propres textures.
        StandardMaterial3D material = new StandardMaterial3D();
        material.AlbedoColor = new Color(1, 0, 0);  // Transparent
        box.Material = material;

        MeshInstance3D meshInstance = new MeshInstance3D();
        _mesh = meshInstance;
        meshInstance.Mesh = box;
        meshInstance.Position = new Vector3(0, 0.5f, 0);

        AddChild(meshInstance);
    }

    // ========================================================================
    // _PROCESS — Met à jour la position chaque frame
    // ========================================================================
    // Appelée par Godot à chaque frame (60 FPS typiquement).
    //
    // ALGORITHME :
    //   1. Lire la position de l'entité (en fixed point, ex: 8500)
    //   2. Convertir en float (÷ 1000f, ex: 8.5)
    //   3. Créer un Vector3 avec les trois coordonnées
    //   4. Assigner à Position (propriété héritée de Node3D)
    //
    // POURQUOI / 1000f ET PAS / 1000 ?
    //   En C#, int / int = int (division entière, tronquée)
    //   8500 / 1000 = 8 (on perd le .5 !)
    //
    //   Mais int / float = float (division flottante, précise)
    //   8500 / 1000f = 8.5f ✓
    //
    //   Le "f" après 1000 en fait un float, forçant une division précise.
    //
    // NOTE SUR LA FLUIDITÉ :
    //   Pour l'instant, le cube "saute" de position en position.
    //   Plus tard, on ajoutera de l'INTERPOLATION pour lisser le mouvement
    //   entre deux ticks de simulation.
    public override void _Process(double delta)
    {
        float x = _entity.PositionX / 1000f;
        float y = _entity.PositionY / 1000f;
        float z = _entity.PositionZ / 1000f;
        
        Position = new Vector3(x, y, z);
    }
}