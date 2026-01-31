// ============================================================================
// DATALOADER.CS — Charge les données de configuration depuis les fichiers JSON
// ============================================================================
// Ce fichier est dans CORE car il ne dépend pas de Godot.
// C'est de la pure logique de chargement de données.
//
// ============================================================================
// POURQUOI CE FICHIER EXISTE ?
// ============================================================================
// Pattern "data-driven" : les données sont SÉPARÉES du code.
//
// Avant (hardcodé) :
//   if (materialId == 1) color = gray;
//   if (materialId == 2) color = brown;
//
// Maintenant (data-driven) :
//   List<MaterialDefinition> materials = DataLoader.LoadMaterials(path);
//   color = materials[materialId].Color;
//
// AVANTAGES :
//   - Modifier les couleurs sans recompiler
//   - Ajouter des matériaux sans toucher au code
//   - Support des mods (les joueurs créent leurs propres JSON)
//
// ============================================================================
// SYSTEM.TEXT.JSON — La bibliothèque de désérialisation
// ============================================================================
// Désérialiser = convertir du texte JSON en objets C#.
//
// Le JSON :
//   [{"Id": 1, "Name": "Stone", "Color": [128, 128, 128, 255]}]
//
// Devient :
//   List<MaterialDefinition> avec un élément où Id=1, Name="Stone", etc.
//
// JsonSerializer.Deserialize<T>(json) fait tout automatiquement.
// Il fait correspondre les noms : "Name" dans JSON → Name en C#.
//
// ============================================================================
// LIENS AVEC LES AUTRES FICHIERS
// ============================================================================
// - MaterialDefinition.cs : la structure qui représente UN matériau
// - Data/materials.json : le fichier de données
// - ChunkRenderer.cs : utilise les couleurs pour le rendu
// ============================================================================

using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ProjetColony2.Core.Data;

public static class DataLoader
{
    // ========================================================================
    // LOADMATERIALS — Charge la liste des matériaux depuis un fichier JSON
    // ========================================================================
    // PARAMÈTRE :
    //   path : chemin vers le fichier (ex: "Data/materials.json")
    //
    // RETOUR :
    //   Liste de MaterialDefinition, un par objet {...} dans le JSON
    //
    // ÉTAPES :
    //   1. File.ReadAllText lit le fichier entier en string
    //   2. JsonSerializer.Deserialize convertit la string en objets C#
    //   3. On retourne la liste
    //
    // EXEMPLE :
    //   var materials = DataLoader.LoadMaterials("Data/materials.json");
    //   byte[] stoneColor = materials[1].Color;  // [128, 128, 128, 255]
    public static List<MaterialDefinition> LoadMaterials(string path)
    {
        string materialsData = File.ReadAllText(path);
        List<MaterialDefinition> list = JsonSerializer.Deserialize<List<MaterialDefinition>>(materialsData);
        return list;
    }
}