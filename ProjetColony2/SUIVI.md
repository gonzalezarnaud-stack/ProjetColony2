# ProjetColony2 — Suivi du développement

## État actuel

**Date :** 28/01/2026
**Phase :** MVP-A TERMINÉ ✓
**Statut :** Monde voxel jouable avec minage, placement et génération procédurale

---

## Architecture

```
ProjetColony2/
├── Core/                      ← Simulation pure (pas de Godot)
│   ├── World/
│   │   ├── Voxel.cs           ✓ Terrain (MaterialId)
│   │   ├── SubCell.cs         ✓ Constructions (forme, matériau, orientation)
│   │   ├── SubCellCoord.cs    ✓ Position dans sous-grille
│   │   ├── Chunk.cs           ✓ Conteneur 16×16×16
│   │   └── GameWorld.cs       ✓ Gestionnaire de chunks
│   │
│   ├── Entities/
│   │   ├── Entity.cs          ✓ Entité (position, velocity, speed, brain)
│   │   ├── IntentComponent.cs ✓ Intentions (mouvement, actions, cibles)
│   │   ├── BrainComponent.cs  ✓ Cerveau abstrait
│   │   └── PlayerBrain.cs     ✓ Cerveau joueur (vide, attend commands)
│   │
│   ├── Commands/
│   │   ├── ICommand.cs        ✓ Interface commande
│   │   ├── MoveCommand.cs     ✓ Commande déplacement
│   │   ├── JumpCommand.cs     ✓ Commande saut
│   │   ├── MineCommand.cs     ✓ Commande minage
│   │   ├── PlaceCommand.cs    ✓ Commande placement
│   │   └── CommandBuffer.cs   ✓ File d'attente
│   │
│   ├── Collision/
│   │   ├── ICollisionShape.cs ✓ Interface collision
│   │   ├── BoxCollisionShape.cs ✓ Boîtes (cube, demi-bloc)
│   │   └── SlopeCollisionShape.cs ✓ Pentes
│   │
│   ├── Shapes/
│   │   └── Orientation.cs     ✓ 12 orientations possibles
│   │
│   └── Systems/
│       ├── MovementSystem.cs  ✓ Collisions, gravité, saut
│       ├── RaycastSystem.cs   ✓ Viser un bloc (DDA)
│       └── WorldGenerator.cs  ✓ Génération procédurale (Simplex)
│
├── Engine/                    ← Rendu et input (Godot)
│   ├── Rendering/
│   │   ├── ChunkRenderer.cs   ✓ Affiche un chunk (optimisé, rebuild)
│   │   ├── CubeFaces.cs       ✓ Données des 6 faces
│   │   └── EntityRenderer.cs  ✓ Affiche une entité (cube rouge)
│   │
│   ├── PlayerController.cs    ✓ Input → Commands (mouvement, saut, minage)
│   ├── PlayerCamera.cs        ✓ Caméra FPS (rotation X et Y)
│   └── GameManager.cs         ✓ Orchestrateur (multi-chunks)
│
└── Scenes/
    └── Main.tscn              ✓ Scène principale
```

**Légende :** ✓ = fait | ○ = à faire | ✗ = pas prévu pour l'instant

---

## Fichiers créés (ordre chronologique)

| # | Fichier | Rôle |
|---|---------|------|
| 1 | Orientation.cs | 12 orientations de blocs |
| 2 | SubCellCoord.cs | Position sous-grille 5×5×5 |
| 3 | Voxel.cs | Cube de terrain |
| 4 | SubCell.cs | Bloc construit |
| 5 | ICollisionShape.cs | Interface collision |
| 6 | BoxCollisionShape.cs | Collision boîte |
| 7 | SlopeCollisionShape.cs | Collision pente |
| 8 | Chunk.cs | Conteneur 16³ voxels |
| 9 | GameWorld.cs | Monde infini |
| 10 | ChunkRenderer.cs | Rendu optimisé |
| 11 | GameManager.cs | Point d'entrée |
| 12 | CubeFaces.cs | Données des faces |
| 13 | IntentComponent.cs | Intentions entité |
| 14 | Entity.cs | Entité de base |
| 15 | BrainComponent.cs | Cerveau abstrait |
| 16 | PlayerBrain.cs | Cerveau joueur |
| 17 | ICommand.cs | Interface commande |
| 18 | MoveCommand.cs | Commande déplacement |
| 19 | CommandBuffer.cs | File d'attente |
| 20 | PlayerController.cs | Input clavier/souris |
| 21 | EntityRenderer.cs | Affiche entité |
| 22 | PlayerCamera.cs | Caméra FPS |
| 23 | MovementSystem.cs | Collisions et gravité |
| 24 | JumpCommand.cs | Commande saut |
| 25 | RaycastSystem.cs | Raycast DDA |
| 26 | MineCommand.cs | Commande minage |
| 27 | PlaceCommand.cs | Commande placement |
| 28 | WorldGenerator.cs | Génération Simplex |

---

## Ce qui fonctionne (MVP-A complet)

- [x] Monde voxel (grille 3×3 chunks)
- [x] Génération procédurale (collines Simplex)
- [x] Rendu optimisé (faces visibles uniquement)
- [x] Joueur = Entité avec Brain
- [x] Système de Commands (lockstep-ready)
- [x] Déplacement avec flèches
- [x] Caméra FPS (souris)
- [x] Mouvement suit la direction du regard
- [x] Vitesse paramétrable (Entity.Speed)
- [x] Collisions terrain (murs)
- [x] Collisions hauteur (2 blocs)
- [x] Glissement le long des murs
- [x] Gravité
- [x] Saut (avec vélocité)
- [x] Raycast DDA (viser un bloc)
- [x] Casser des blocs (clic gauche)
- [x] Poser des blocs (clic droit)
- [x] Rebuild du bon chunk après modification

---

## Prochaines étapes

### MVP-B : Construction fine

| Priorité | Tâche | Fichier |
|----------|-------|---------|
| 1 | Sous-grille 5×5×5 | SubCell, raycast |
| 2 | Formes (pente, poteau, demi-bloc) | ShapeDefinition.cs |
| 3 | Mode placement fin | PlayerController |
| 4 | Inventaire basique | Inventory.cs |
| 5 | Couleurs par matériau | ChunkRenderer |

### MVP-C : Double vie

- Colons avec DwarfBrain
- Pathfinding (A*)
- Jobs (mineur, transporteur...)
- Vue gestion (caméra libre, toggle)
- Besoins (faim, soif, fatigue)

### V1 : Version complète

- Combat style Dark Souls
- Génération monde avec histoire
- Fluides et température
- Craft avancé
- Multijoueur lockstep

---

## Décisions techniques

| Sujet | Décision | Raison |
|-------|----------|--------|
| Positions | int × 1000 (fixed point) | Déterminisme lockstep |
| Rotation | int en degrés (0-359) | Lisibilité + déterminisme |
| Joueur | = Entité avec PlayerBrain | Uniformité, pas de cas spécial |
| Actions | Via Commands | Lockstep multijoueur |
| Grille construction | 5×5×5 | Centre exact (2,2,2) |
| Grille objets | 25×25×25 | Précision fine (futur) |
| Tableaux | 3D pour l'instant | Lisibilité (1D = optimisation future) |
| Minage FPS | 1×1, 1×2 ou 2×2 au choix | Flexibilité joueur |
| Minage colons | Selon taille entité | Automatique et cohérent |
| Raycast | DDA (pas pas-à-pas) | Précision, standard industrie |
| Bruit terrain | Simplex (pas Perlin) | Meilleur en 3D (grottes futures) |
| Hauteur entité | 2 blocs (Medium) | Futur : entity.Height |
| Gravité | 25 units/tick | Modifiable par zone |
| Force saut | 280 units | Par entité (Entity.JumpForce) |

---

## Notes pour plus tard

- **JSON** : entity_sizes.json, materials.json, shapes.json quand nécessaire
- **Optimisation** : Tableaux 1D + calcul simple si performance insuffisante
- **Multijoueur** : GameClock + InputBuffer quand on y arrive
- **Manette** : Ajouter stick droit dans PlayerCamera (quelques lignes)
- **Interpolation** : Lisser le rendu entre les ticks (EntityRenderer, PlayerCamera)
- **Textures** : Atlas de textures au lieu de couleurs unies
- **Chunks voisins** : ChunkRenderer devrait vérifier les blocs des chunks adjacents

---

## Bugs connus

Aucun pour l'instant.

---

## Journal des sessions

### Session 1 (date inconnue)
- Création architecture Core/Engine
- Fichiers Voxel, SubCell, Chunk, World
- Collision shapes

### Session 2 (25/01/2026)
- Système Entity/Command complet
- PlayerController, PlayerCamera
- EntityRenderer (cube rouge)
- Mouvement FPS fonctionnel
- Documentation VISION.md et SUIVI.md

### Session 3 (28/01/2026)
- **MovementSystem** : Collisions terrain, glissement murs
- **Gravité** : Avec vélocité Y, atterrissage, plafond
- **Saut** : JumpCommand, JumpForce par entité
- **Raycast DDA** : TryGetTargetBlock avec hitPoint
- **Minage/Placement** : MineCommand, PlaceCommand, rebuild chunk
- **Génération procédurale** : WorldGenerator avec Simplex noise
- **Multi-chunks** : Grille 3×3, dictionnaire de chunks
- **Fix back-face culling** : Matériau avec CullMode.Disabled
- MVP-A TERMINÉ !
