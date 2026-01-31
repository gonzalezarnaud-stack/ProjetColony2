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
│     ├── Flip (1 bit) : normal/inversé          │
│     ├── StretchY (4 bits) : 1-16 blocs         │
│     └── Coupe (5 bits) : 0-31 niveaux (×1/25)  │
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

**Flip (1 bit) :**
```
0 = Normal (haut en haut)
1 = Inversé (haut en bas)
```

**StretchY (4 bits) :**
```
Valeur 0-15 = hauteur de 1 à 16 blocs
Une pente stretch 2 = pente sur 2 blocs de haut
```

**Coupe (5 bits) :**
```
Valeur 0-31 = niveau de coupe (×1/25 du bloc)
0 = pas de coupe (bloc entier)
5 = coupé à 5/25 = 1/5 de la hauteur
20 = coupé à 20/25 = 4/5 de la hauteur
```

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
uniform bool flip;
uniform float stretchY;    // 1.0 - 16.0
uniform float coupe;       // 0.0 - 1.0

vec3 transformVertex(vec3 v) {
    // 1. Stretch vertical
    v.y *= stretchY;
    
    // 2. Coupe (discard si au-dessus)
    // Géré dans fragment shader
    
    // 3. Flip
    if (flip) v.y = stretchY - v.y;
    
    // 4. Rotation Y
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

Cycle unique à travers toutes les orientations valides de l'objet :

```
Debout → Couché N → Couché E → Couché S → Couché O → Inversé → ...
```

Chaque objet définit ses orientations autorisées.

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

### 8.1 Terrain

```csharp
// Génération colline
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
- StretchY si pente douce sur plusieurs blocs

### 8.2 Entités

```csharp
// Placement arbres
foreach spawn in treeSpawns:
    PlaceEntity("oak_tree", spawn.position, RandomOrientation());
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
5. Ajouter stretch/coupe au shader
6. Ajouter flip
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

*Document créé le 31/01/2026 — *ProjetColony MVP-B