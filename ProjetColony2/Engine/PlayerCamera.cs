// ============================================================================
// PLAYERCAMERA.CS — Caméra FPS qui suit le joueur
// ============================================================================
// Ce fichier est dans ENGINE car il utilise Godot (Camera3D, Input).
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// En mode FPS, le joueur voit à travers les "yeux" de son personnage.
// La caméra doit :
//   - Suivre la position de l'entité joueur
//   - Être à hauteur des yeux (pas au sol)
//   - Tourner avec la souris (regarder autour)
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
// 1. _Ready() : Capture la souris (invisible, bloquée au centre)
// 2. _Input() : Détecte les mouvements souris → met à jour les rotations
// 3. _Process() : Chaque frame, positionne la caméra sur l'entité
//
// SÉPARATION DES RESPONSABILITÉS :
//   - Entity (Core) : position logique du joueur
//   - PlayerCamera (Engine) : où regarder, comment afficher
//
// ============================================================================
// CONCEPTS : ROTATION EN RADIANS
// ============================================================================
// Godot utilise des RADIANS, pas des degrés.
//   - 360° = 2π radians
//   - 180° = π radians
//   - 90° = π/2 radians
//
// Mathf.Pi / 2 ≈ 1.57 radians = 90°
// On limite _rotationX entre -90° et +90° pour ne pas regarder "à l'envers".
//
// ============================================================================
// CONCEPTS : INPUT CAPTURED
// ============================================================================
// Input.MouseModeEnum.Captured signifie :
//   - La souris est INVISIBLE
//   - Elle reste BLOQUÉE au centre de la fenêtre
//   - On reçoit les MOUVEMENTS RELATIFS (combien elle a bougé)
//
// C'est le mode standard pour les jeux FPS.
// Pour libérer la souris (menu pause), on utilisera :
//   Input.MouseMode = Input.MouseModeEnum.Visible;
//
// ============================================================================
// ÉVOLUTION FUTURE
// ============================================================================
// - Support manette (stick droit pour regarder)
// - Vue DF (caméra détachée du joueur, vue du dessus)
// - Transitions fluides entre les modes
// - Sensibilité configurable par le joueur
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - Entity.cs : on lit sa position pour placer la caméra
// - GameManager.cs : crée et initialise la PlayerCamera
// - (Futur) Options : sensibilité, inversion Y...
// ============================================================================

using System;
using Godot;
using ProjetColony2.Core.Entities;

namespace ProjetColony2.Engine;

public partial class PlayerCamera : Camera3D
{
    // ========================================================================
    // CHAMPS — État de la caméra
    // ========================================================================
    // _entity : le joueur qu'on suit
    //
    // _rotationX : rotation verticale (regarder haut/bas)
    //   Valeur en radians, limitée entre -π/2 et +π/2
    //
    // _rotationY : rotation horizontale (regarder gauche/droite)
    //   Valeur en radians, pas de limite (peut tourner à 360°)
    //
    // _sensitivity : vitesse de rotation de la caméra
    //   0.005 = valeur typique, ajustable selon les préférences
    private Entity _entity;
    
    private float _rotationX; // Haut/Bas
    private float _rotationY; // Gauche/Droite
    private float _sensitivity = 0.005f;

    // Hauteur des yeux par rapport aux pieds (en blocs)
    // 1.5 blocs ≈ hauteur des yeux d'un humain de ~1.8m
    public const float EyeHeight = 1.5f;

    // Connecte la caméra à l'entité à suivre
    public void Initialize(Entity entity)
    {
        _entity = entity;
    }

    // ========================================================================
    // _READY — Capture la souris au démarrage
    // ========================================================================
    // MouseModeEnum.Captured = souris invisible et bloquée au centre.
    // Indispensable pour un contrôle FPS fluide.
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    // ========================================================================
    // _INPUT — Détecte les mouvements de souris
    // ========================================================================
    // Appelée par Godot à chaque événement input (touche, souris, manette...).
    //
    // InputEventMouseMotion = la souris a bougé.
    //   Relative.X = mouvement horizontal (pixels)
    //   Relative.Y = mouvement vertical (pixels)
    //
    // On ACCUMULE (+=) les mouvements dans _rotationX et _rotationY.
    // On multiplie par _sensitivity pour contrôler la vitesse.
    //
    // Mathf.Clamp limite _rotationX entre -π/2 et +π/2 :
    //   - Empêche de regarder "derrière soi" en levant trop la tête
    //   - -π/2 = regarder tout en bas
    //   - +π/2 = regarder tout en haut
    public override void _Input(InputEvent @event)
    {
        
        if (@event is InputEventMouseMotion mouseMotion)
        {
            _rotationX -= mouseMotion.Relative.Y * _sensitivity;
            _rotationY -= mouseMotion.Relative.X * _sensitivity;
            _rotationX = Mathf.Clamp(_rotationX, -Mathf.Pi / 2, Mathf.Pi / 2);

            // ====================================================================
            // MISE À JOUR DE LA ROTATION VERTICALE
            // ====================================================================
            // On convertit _rotationX (radians, interne à la caméra)
            // en degrés pour Entity.RotationX.
            //
            // Math.Clamp limite la valeur entre -90 et +90 :
            //   - On ne peut pas regarder "derrière soi" par le haut ou le bas
            //   - -90 = pieds, +90 = ciel, c'est la limite physique
            int deg = (int)Mathf.Round(Mathf.RadToDeg(_rotationY));
            deg = (deg % 360 + 360) % 360;
            _entity.RotationY = deg;

            int degX = (int)Mathf.Round(Mathf.RadToDeg(_rotationX));
            degX = Math.Clamp(degX, -90, 90);
            _entity.RotationX = degX;

        }
    }

    // ========================================================================
    // _PROCESS — Met à jour la position et rotation chaque frame
    // ========================================================================
    // 1. POSITION :
    //    - Lit la position de l'entité (fixed point)
    //    - Convertit en float (÷ 1000f)
    //    - Ajoute EyeHeight pour être à hauteur des yeux
    //
    // 2. ROTATION :
    //    - Applique _rotationX (haut/bas) et _rotationY (gauche/droite)
    //    - Le Z reste à 0 (pas d'inclinaison latérale)
    public override void _Process(double delta)
    {
        float x = _entity.PositionX / 1000f;
        float y = _entity.PositionY / 1000f + EyeHeight;
        float z = _entity.PositionZ / 1000f;

        Rotation = new Vector3(_rotationX, _rotationY, 0);
        Position = new Vector3(x, y, z);
    }
}