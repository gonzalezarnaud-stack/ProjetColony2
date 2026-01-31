# Architecture : Système de Sous-Grille

## Vue d'ensemble

ProjetColony utilise un système de voxels avec **formes paramétriques** pour le terrain, et **sous-grille sparse 25×25×25** pour les constructions joueur.

---

## 1. Structure du Voxel

### 1.1 Format (3 bytes)

```
┌─────────────────────────────────────────────────┐
│ Voxel (3 bytes = 24 bits)                       │
│ ├── MaterialId (8 bits) : 0-255 matériaux      │
│ └── ShapeData (16 bits)                         │
│     ├── BaseShape (4 bits) : 0-15 formes       │
│     ├── Rotation (2 bits) : N/E/S/O            │
│     ├── Position (2 bits) : Debout/Couché/Inv  │
│     └── Height (8 bits) : 0-255 (en 1/25 bloc) │
└─────────────────────────────────────────────────┘
```

### 1.2 Valeurs spéciales

| ShapeData | Signification |
|-----------|---------------|
| 0x0000 | Air (vide) |
| 0xFFFF | Custom → chercher dans sparse |

---

## 2. Formes de base

### 2.1 Liste (BaseShape 4 bits = 16 max)

| Id | Nom | Description |
|----|-----|-------------|
| 0 | Air | Vide |
| 1 | Cube | Bloc plein |
| 2 | Pente | Triangle incliné (base sud) |
| 3 | Angle extérieur | Coin convexe |
| 4 | Angle intérieur | Coin concave |
| 5 | Poteau | Cylindre/carré central |
| 6-15 | Réservé | Extensions futures |

### 2.2 Transformations

**Rotation (2 bits) :**
```
0 = Nord (0°)
1 = Est (90°)
2 = Sud (180°)
3 = Ouest (270°)
```

**Position (2 bits) :**
```
0 = Debout (Y+ vers le haut)
1 = Couché (Y+ vers le côté)
2 = Inversé (Y+ vers le bas)
3 = Réservé
```

**Height (8 bits) :**
```
Hauteur en 1/25 de bloc (= 4cm de précision)
Valeur 25 = 1 bloc standard
Valeur 35 = 1 bloc + 2/5 (pente douce)
Valeur 50 = 2 blocs
Max 255 = ~10 blocs
```

Exemples :
- Pente 1 bloc : Height = 25
- Pente 1.4 bloc : Height = 35
- Pente 2 blocs : Height = 50
- Bloc partiel 3/5 : Height = 15

---

## 3. Stockage par type

### 3.1 Terrain naturel (génération procédurale)

```
Voxel { MaterialId, ShapeData }
3 bytes par voxel
16×16×16 chunk = 12 Ko par chunk
```

Léger, rapide, suffisant pour collines, falaises, cavernes.

### 3.2 Terrain taillé par le joueur

Quand le joueur taille un voxel terrain :

```
1. Voxel.ShapeData = 0xFFFF (flag "custom")
2. Créer entrée dans Dictionary<int, SubCellData[]>
3. Initialiser avec les sous-cellules correspondant à la forme originale
4. Retirer les sous-cellules taillées
```

### 3.3 Constructions joueur

Directement en sparse 25×25×25 :

```csharp
// Dans Chunk
Dictionary<int, SubCellBlock> _customBlocks;

// Clé = index du voxel (x + y*16 + z*256)
// Valeur = données des sous-cellules
```

### 3.4 Entités naturelles (arbres, rochers)

Objets complets, non modifiables (sauf destruction totale) :

```csharp
public class WorldEntity
{
    public int EntityTypeId;      // "oak_tree", "boulder"...
    public Vector3Int Position;   // Position voxel
    public byte Orientation;      // Rotation
    public int Health;            // Pour abattage/minage
}
```

---

## 4. Sous-grille 25×25×25

### 4.1 Quand utilisée

- Voxel avec ShapeData = 0xFFFF
- Constructions joueur
- Objets fins (meubles, décorations)

### 4.2 Structure SubCellBlock

```csharp
public class SubCellBlock
{
    // Sparse : seulement les cellules occupées
    public Dictionary<int, SubCell> Cells;
    // Clé = x + y*25 + z*625 (0 à 15624)
}

public struct SubCell
{
    public byte MaterialId;
    public byte ShapeId;      // Forme de la sous-cellule (cube, pente mini...)
    public byte Orientation;  // Rotation + flip
}
```

### 4.3 Mémoire estimée

| Situation | Sous-cellules | Mémoire |
|-----------|---------------|---------|
| Voxel non custom | 0 | 0 |
| Voxel légèrement taillé | ~100 | ~300 bytes |
| Voxel très sculpté | ~1000 | ~3 Ko |
| Chunk 100% custom (irréaliste) | 64M | Explosion ❌ |

Protection naturelle : tailler prend du temps.

---

## 5. Rendu (Shader)

### 5.1 Principe

Le GPU transforme les 5-6 meshes de base selon ShapeData.

```glsl
// Vertex shader
uniform int baseShape;
uniform int rotation;      // 0-3
uniform int position;      // 0=debout, 1=couché, 2=inversé
uniform float height;      // En blocs (ex: 1.4)

vec3 transformVertex(vec3 v) {
    // 1. Hauteur (scale Y)
    v.y *= height;
    
    // 2. Position
    if (position == 1) {
        // Couché : rotation 90° sur X
        float tmp = v.y;
        v.y = -v.z;
        v.z = tmp;
    } else if (position == 2) {
        // Inversé : flip vertical
        v.y = height - v.y;
    }
    
    // 3. Rotation Y
    float angle = rotation * 1.5708; // π/2
    mat2 rot = mat2(cos(angle), -sin(angle), sin(angle), cos(angle));
    v.xz = rot * v.xz;
    
    return v;
}
```

### 5.2 Avantages

- 5 meshes en mémoire au lieu de 1000+
- Transformations = quasi gratuit sur GPU
- Infinité de combinaisons possibles

---

## 6. Contrôles joueur

### 6.1 Modes

| Touche (Y/Tab) | Mode | Granularité |
|----------------|------|-------------|
| 1er appui | Normal | Voxel entier (1m) |
| 2e appui | Construction | Snap 5×5 (20cm) |
| 3e appui | Précision | Snap 25×25 (4cm) |

### 6.2 Actions

| Manette | Mode Normal | Mode Construction/Précision |
|---------|-------------|----------------------------|
| RT maintenu | Miner voxel | Tailler sous-cellule |
| LT | Poser voxel | Poser sous-cellule |
| D-pad ←/→ | — | Cycle orientation |

### 6.3 Orientations

**Deux axes indépendants :**

| Input | Action |
|-------|--------|
| D-pad ↑/↓ | Cycle position (Debout → Couché → Inversé) |
| D-pad ←/→ | Rotation horizontale (N → E → S → O) |

**Positions (3) :**
```
Debout   = Y+ vers le haut (normal)
Couché   = Y+ vers le côté (objet sur le flanc)
Inversé  = Y+ vers le bas (à l'envers)
```

**Rotations (4) :**
```
Nord (0°) → Est (90°) → Sud (180°) → Ouest (270°)
```

**Total : 3 × 4 = 12 orientations max**

Chaque objet définit ses positions autorisées :

```json
{
  "Id": "mug",
  "AllowedPositions": ["debout", "couché", "inversé"]
}
```

```json
{
  "Id": "charrette", 
  "AllowedPositions": ["debout"]
}
```

Si une seule position autorisée → D-pad ↑/↓ ne fait rien.

---

## 7. Matériaux et débris

### 7.1 Récupération

```
1 sous-cellule taillée = 1 débris de [matériau]
125 débris = 1 bloc (5×5×5)
```

### 7.2 Inventaire

```
Pierre (blocs) : 3
Pierre (débris) : 47
```

Icônes distinctes à terme.

### 7.3 Consommation

- Poser 1 sous-cellule = 1 débris
- Pas assez de débris ? Consomme 1 bloc → 124 débris restants

---

## 8. Génération procédurale

### 8.1 Vue d'ensemble — Catégories

| Catégorie | Méthode | Stockage |
|-----------|---------|----------|
| Terrain | Noise (Simplex) | Voxel ShapeData |
| Végétation grande | L-System / procédural | Entités |
| Végétation petite | Placement simple | Billboards |
| Minéraux | Noise + scatter | Prefabs |
| Structures | Grammaire + règles | Voxels + Objets |

### 8.2 Terrain

```csharp
for each voxel in region:
    float height = noise(x, z);
    
    if (y < height - 1)
        voxel = { Stone, CUBE }
    else if (y < height)
        voxel = { Grass, CalculateSlope(neighbors) }
    else
        voxel = { Air }
```

CalculateSlope analyse les voisins pour déterminer :
- BaseShape (pente, angle...)
- Rotation
- Height si pente douce sur plusieurs blocs

### 8.3 Végétation grande (Arbres)

```csharp
// L-System ou algo dédié
// Chaque arbre = entité (abattable d'un coup)
foreach spawn in treeSpawns:
    Tree tree = GenerateTree(biome, Random.seed);
    PlaceEntity(tree, spawn.position, RandomRotation());
```

Formes utilisées : Tronc (cylindre), Branches, Feuillage (blob)

### 8.4 Végétation petite (Plantes, buissons)

```csharp
// Style Minecraft : 2 quads croisés en X
// Pas de vraie 3D, juste une image (billboard)
foreach spawn in plantSpawns:
    PlaceBillboard("grass_tuft", spawn.position);
```

Léger, simple, efficace.

### 8.5 Minéraux (Rochers)

```csharp
// Prefabs avec variations (taille, rotation)
foreach spawn in rockSpawns:
    string prefab = PickRandom("boulder_small", "boulder_medium", "boulder_large");
    float scale = Random.Range(0.8f, 1.2f);
    PlacePrefab(prefab, spawn.position, RandomRotation(), scale);
```

### 8.6 Structures (Bâtiments)

**Grammaire de bâtiment :**
```
Bâtiment = Fondation + Murs + Toit + Intérieur
```

**Génération des murs :**
```csharp
void GenerateWall(int x, int z, int height, Direction facing)
{
    for (int y = 0; y < height; y++)
    {
        PlaceVoxel(x, y, z, Material.Stone, Shape.Cube);
    }
    
    // Fenêtre ?
    if (Random.Chance(30%) && height > 2)
    {
        PlaceVoxel(x, 2, z, Material.Air);
        PlaceObject(x, 2, z, "window_frame", facing);
    }
}
```

**Ameublement :**
```csharp
void FurnishRoom(Room room, RoomType type)
{
    if (type == RoomType.Bedroom)
    {
        PlaceObject(room.Corner, "bed", facing: room.Wall);
        PlaceObject(room.OtherCorner, "chest");
        if (room.Size > 16) 
            PlaceObject(room.Center, "table_small");
    }
    else if (type == RoomType.Kitchen)
    {
        PlaceObject(room.Wall, "hearth");
        PlaceObject(room.Center, "table_large");
        PlaceObjects(room.Around("table"), "chair", count: 2-4);
    }
}
```

**Avantages :**
- Utilise exactement le même système que le joueur
- Murs = Voxels ShapeData
- Meubles = Objets prefabs 25×25×25
- Le joueur peut modifier, réparer, agrandir tout bâtiment généré

**Variations par culture/richesse :**
```
Maison paysanne :
┌─────────┐
│ ▢     ▢ │  ← Fenêtres simples
│    ░    │  ← Porte simple
└─────────┘

Maison riche :
┌─────────────┐
│ ▢    ◊    ▢ │  ← Fenêtre ornée
│ ▢         ▢ │
│      ░░     │  ← Double porte
└─────────────┘
```

---

## 9. Résumé mémoire

| Élément | Stockage | Pour 1 chunk |
|---------|----------|--------------|
| Terrain voxels | 3 bytes/voxel | 12 Ko |
| Sparse custom | Variable | ~0-50 Ko |
| Entités | ~20 bytes/entité | ~1 Ko |
| **Total typique** | | **~15-65 Ko** |

Vs approach naïve (dense 25×25×25) : **128 Mo** par chunk.

---

## 10. Fichiers à créer

| Fichier | Contenu |
|---------|---------|
| `Core/World/Voxel.cs` | Modifier : MaterialId + ShapeData |
| `Core/World/ShapeData.cs` | Struct pour encoder/décoder les 16 bits |
| `Core/World/SubCellBlock.cs` | Stockage sparse sous-cellules |
| `Core/World/SubCell.cs` | Une sous-cellule (existe déjà, adapter) |
| `Core/Data/BaseShapeDefinition.cs` | Définition forme de base |
| `Data/shapes.json` | Les 5-6 formes de base |
| `Engine/Shaders/terrain.gdshader` | Shader transformation |

---

## 11. Ordre d'implémentation

### Phase 1 : Voxel paramétrique
1. Modifier Voxel.cs (MaterialId + ShapeData)
2. Créer ShapeData.cs (encodage/décodage)
3. Adapter ChunkRenderer pour ShapeData
4. Créer shader basique (rotation seulement)

### Phase 2 : Transformations complètes
5. Ajouter height au shader
6. Ajouter position (debout/couché/inversé)
7. Tester génération terrain avec pentes

### Phase 3 : Sous-grille sparse
8. Créer SubCellBlock.cs
9. Détecter ShapeData = 0xFFFF → sparse
10. Implémenter taille joueur
11. Conversion forme → sous-cellules

### Phase 4 : Contrôles
12. Mode Normal/Construction/Précision
13. Cycle orientations
14. Système débris

---

*Document créé le 31/01/2026 — ProjetColony MVP-B*