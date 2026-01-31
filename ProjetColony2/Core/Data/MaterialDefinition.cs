// ============================================================================
// MATERIALDEFINITION.CS — Représente UN matériau chargé depuis le JSON
// ============================================================================
// Ce fichier est dans CORE car il ne dépend pas de Godot.
// C'est une pure structure de données.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Chaque objet {...} dans materials.json devient un MaterialDefinition.
//
// JSON :
//   {"Id": 1, "Name": "Stone", "Color": [128, 128, 128, 255]}
//
// C# :
//   MaterialDefinition avec Id=1, Name="Stone", Color=[128,128,128,255]
//
// ============================================================================
// CLASSE VS STRUCT
// ============================================================================
// On utilise une CLASSE ici, pas une struct.
//
// Pourquoi ? JsonSerializer a besoin de :
//   1. Un constructeur sans paramètre (créé automatiquement pour les classes)
//   2. Des propriétés avec { get; set; }
//
// Les structs marchent aussi, mais les classes sont plus simples ici.
//
// ============================================================================
// LES PROPRIÉTÉS { get; set; }
// ============================================================================
// Ce sont des "auto-properties" — C# génère le code automatiquement.
//
// Équivalent à :
//   private byte _id;
//   public byte Id { get { return _id; } set { _id = value; } }
//
// Mais en une seule ligne !
//
// JsonSerializer EXIGE des propriétés (pas des champs simples).
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - DataLoader.cs : charge le JSON et crée les MaterialDefinition
// - Data/materials.json : la source des données
// - ChunkRenderer.cs : utilise Color pour colorer les faces
// ============================================================================

namespace ProjetColony2.Core.Data;

public class MaterialDefinition
{
    // ========================================================================
    // ID — Identifiant unique du matériau (0-255)
    // ========================================================================
    // Correspond au MaterialId dans Voxel.
    // 0 = Air, 1 = Stone, 2 = Dirt, 3 = Grass, etc.
    public byte Id { get; set; }

    // ========================================================================
    // NAME — Nom lisible du matériau
    // ========================================================================
    // Pour l'affichage à l'écran, les logs, le débogage.
    // Ex: "Stone", "Dirt", "Grass"
    public string Name { get; set; }

    // ========================================================================
    // COLOR — Couleur RGBA du matériau
    // ========================================================================
    // Tableau de 4 bytes : [Rouge, Vert, Bleu, Alpha]
    // Valeurs de 0 à 255.
    //
    // Exemples :
    //   [128, 128, 128, 255] = gris opaque (Stone)
    //   [139, 90, 43, 255]   = marron opaque (Dirt)
    //   [0, 0, 0, 0]         = transparent (Air)
    //
    // Futur : remplacé par une texture (TexturePath).
    public int[] Color { get; set; }

    public int Hardness { get; set; } // En millisecondes
}