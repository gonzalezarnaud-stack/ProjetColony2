# ProjetColony — Vision du projet

## Le concept en une phrase

**Un jeu de gestion de colonie en voxels, jouable à la première personne ET en vue stratégique.**

---

## Les trois piliers

### Pilier 1 : La double vie

Pouvoir jouer son personnage (FPS) ET gérer sa colonie (vue du dessus).

**Mode FPS (Minecraft-like) :**
- Vue à la première personne
- Contrôle direct du personnage
- Construction bloc par bloc
- Combat action (style Dark Souls)

**Mode Gestion (Dwarf Fortress-like) :**
- Vue du dessus / isométrique
- Gestion des colons, jobs, priorités
- Vue par couches (tranches horizontales)
- Ordres de construction et minage

**Transition fluide :**
Une touche pour passer d'un mode à l'autre. La simulation continue, seul le rendu change.

---

### Pilier 2 : Le monde vivant

Un monde qui vit et évolue, inspiré de Dwarf Fortress.

**Génération procédurale :**
- Histoire du monde générée (civilisations, guerres, héros)
- Terrain avec biomes, rivières, montagnes
- Ruines, donjons, villages

**Simulation systémique :**
- Température, propagation du feu
- Fluides (eau, lave)
- Gravité (blocs non supportés tombent)
- Écosystème (animaux, plantes)

**Colons autonomes :**
- Besoins (faim, soif, sommeil, bonheur)
- Relations entre colons
- Préférences, aptitudes, humeurs
- Jobs assignés ou automatiques

---

### Pilier 3 : La construction expressive

Construire avec précision et créativité.

**Sous-grille 5×5×5 :**
- Chaque voxel divisible en 5×5×5
- Centre exact possible (2, 2, 2)
- Poteaux, pentes, dalles, détails

**Sous-grille 25×25×25 (objets) :**
- Placement fin des meubles et décorations
- Précision de 4cm dans un bloc d'1m
- Compatible avec la grille 5×5×5

**Formes disponibles :**
- Bloc plein, demi-bloc
- Pente (bloc et demi-bloc)
- Poteau, coin, escalier

**Modes de placement :**
- Normal : voxel entier, rotation auto
- Fin : sous-grille, rotation manuelle

---

## Principes techniques fondamentaux

### Déterminisme absolu

**Règle d'or :** Deux machines avec les mêmes inputs produisent exactement le même résultat.

**Comment :**
- Positions en int × 1000 (fixed point, pas de floats)
- Rotations en int (degrés 0-359)
- RNG seedée et contrôlée
- Pas de `DateTime.Now`, pas de `Random.Shared`
- Ordre de traitement garanti (pas de Dictionary non trié)

**Pourquoi :**
- Multijoueur lockstep possible
- Replays parfaits
- Debug et tests fiables
- Sauvegarde légère (seed + inputs)

---

### Architecture lockstep

**Inspiré de :** Age of Empires, Factorio, RimWorld

**Principe :**
```
Joueur appuie sur Z
       ↓
PlayerController crée MoveCommand
       ↓
Command ajoutée au buffer
       ↓
(En multi : envoyée aux autres joueurs)
       ↓
Tick N+1 : tous exécutent la même Command
       ↓
Simulation identique partout
```

**Le joueur n'agit jamais "maintenant".**
Il demande que quelque chose arrive au tick suivant.

---

### Séparation Core / Engine

**Core (simulation) :**
- Pur C#, AUCUNE dépendance à Godot
- Déterministe, testable isolément
- Peut tourner sur un serveur headless

**Engine (rendu) :**
- Dépend de Godot
- Affiche l'état du Core
- Ne modifie JAMAIS le Core directement
- Interpolation pour fluidité visuelle

**Règle :** Le rendu ne contient aucune logique de jeu.

---

### Data-driven (JSON)

**Tout ce qui est configurable = JSON :**
- Matériaux (pierre, bois, fer...)
- Formes de blocs (cube, pente, poteau...)
- Entités (humain, nain, gobelin...)
- Tailles d'entités (tiny, medium, huge...)
- Recettes de craft
- Jobs et comportements

**Avantages :**
- Modifiable sans recompiler
- Moddable par les joueurs
- Centralisé et lisible
- Traductions simplifiées (textes dans JSON)

**Exemple `entity_sizes.json` :**
```json
[
  {
    "Id": "medium",
    "Name": "Moyen",
    "Width": 1,
    "Height": 2,
    "PassageWidth": 1,
    "PassageHeight": 2
  }
]
```

---

### Behaviors et Tags

**Tags :** Propriétés passives d'un objet
```json
{
  "Id": "wood",
  "Tags": ["FLAMMABLE", "ORGANIC", "MINEABLE"]
}
```

**Behaviors :** Actions/réactions
```json
{
  "Id": "wood",
  "Behaviors": [
    {"On": "FIRE", "Do": "BURN", "Duration": 60},
    {"On": "AXE_HIT", "Do": "DROP_ITEM", "Item": "wood_log"}
  ]
}
```

**Avantages :**
- Interactions émergentes (feu + bois = brûle)
- Facilement extensible
- Moddable

---

### Entité uniforme

**Règle :** Le joueur n'est pas spécial.

```
Joueur = Entité + PlayerBrain (attend les Commands)
Colon = Entité + DwarfBrain (IA autonome)
Animal = Entité + AnimalBrain (comportements)
```

**Même système pour tous :**
- Mêmes composants (position, intent, stats)
- Même pipeline de simulation
- Mêmes commandes (Move, Mine, Attack...)

**Possibilité future :** "Posséder" un colon en mode Aventure.

---

### Inputs universels

**Jouable clavier/souris ET manette sans refactoring.**

**Architecture :**
```
Clavier/Souris  →  Actions abstraites  →  Commands
Manette         →  Actions abstraites  →  Commands
```

**Actions abstraites :**
- move_forward, move_back, move_left, move_right
- look_up, look_down, look_left, look_right
- action_primary, action_secondary
- menu_confirm, menu_cancel

**Mapping configurable par le joueur.**

---

### Traductions simplifiées

**Tous les textes visibles = dans des fichiers JSON.**

```json
{
  "lang": "fr",
  "strings": {
    "material_stone": "Pierre",
    "material_wood": "Bois",
    "job_miner": "Mineur",
    "ui_confirm": "Confirmer"
  }
}
```

**Le code n'affiche jamais de texte en dur.**
Il utilise des clés : `GetText("material_stone")`

---

### Scalabilité

**Monde infini :**
- Chunks chargés/déchargés dynamiquement
- Génération à la demande

**Optimisations prévues :**
- Tableaux 1D + calcul d'index (au lieu de 3D)
- Greedy meshing pour le rendu
- LOD pour les chunks distants
- Threading pour génération de mesh

**On n'optimise pas prématurément.**
On construit proprement, on optimise quand c'est mesuré nécessaire.

---

## Tailles d'entités

Défini par JSON, utilisé pour collisions et minage auto.

| Catégorie | Largeur × Hauteur | Passage min | Exemples |
|-----------|-------------------|-------------|----------|
| Tiny | 1×1 | 1×1 | Rat, poulet |
| Small | 1×1 | 1×1 | Gobelin, enfant |
| Medium | 1×2 | 1×2 | Humain, nain, elfe |
| Large | 2×2 | 2×2 | Ogre, ours |
| Huge | 2×3 | 2×3 | Troll |
| Colossal | 3×3+ | 3×3+ | Géant, dragon |

---

## Modes de minage

**Le joueur choisit la taille (touche M pour cycler) :**

| Mode | Taille | Usage |
|------|--------|-------|
| Précis | 1×1 | Lucarne, sculpture |
| Passage | 1×2 | Couloir standard |
| Large | 2×2 | Salle, couloir large |

**Les colons utilisent le même système.**
Un ordre de minage contient la taille demandée.

---

## Références et inspirations

### De Dwarf Fortress

- Génération du monde avec histoire
- Gestion des colons (jobs, besoins, humeurs)
- Vue par couches
- Simulation systémique (température, fluides)
- Mécanique de gravité

### De Minecraft

- Monde voxel
- Vue FPS immersive
- Minage et construction directe
- Exploration

### De Factorio

- Architecture lockstep
- Scalabilité massive
- Comportements data-driven

### D'ailleurs

- Combat : Dark Souls (action, timing)
- Magie : Baldur's Gate (parchemins)
- Futur lointain : automatisation (Satisfactory)

---

## Scope MVP

### MVP-A : Le monde voxel de base ✓ TERMINÉ

**Objectif :** Se balader et interagir avec un monde voxel.

- [x] Chunk et voxels
- [x] Rendu optimisé
- [x] Joueur = Entité
- [x] Système de Commands
- [x] Déplacement FPS
- [x] Caméra souris
- [x] Collisions terrain
- [x] Gravité et saut
- [x] Génération terrain (Simplex)
- [x] Casser/Poser blocs

### MVP-B : La construction fine

**Objectif :** Prouver le système de sous-grille.

- [ ] Grille 5×5×5 fonctionnelle
- [ ] Formes : bloc, demi-bloc, pente, poteau
- [ ] Mode normal et mode fin
- [ ] Inventaire basique
- [ ] Couleurs par matériau

### MVP-C : La double vie

**Objectif :** Jouer ET gérer.

- [ ] Colons avec IA (DwarfBrain)
- [ ] Vue gestion (caméra libre)
- [ ] Jobs de base (mineur, transporteur)
- [ ] Besoins (faim, soif, fatigue)
- [ ] Pathfinding (A*)

### V1 : Version jouable complète

- Combat style Dark Souls
- Génération monde avec histoire
- Plus de jobs et métiers
- Fluides et température
- Craft avancé
- Multijoueur lockstep

---

## Le rêve (futur lointain)

- Automatisation style Satisfactory
- Magie avancée (sorts personnalisés)
- Véhicules (chariots, bateaux)
- Mods communautaires
- Monde persistant multijoueur

---

## Résumé technique

| Aspect | Choix | Raison |
|--------|-------|--------|
| Positions | int × 1000 | Déterminisme |
| Rotations | int degrés | Lisibilité + déterminisme |
| Simulation | Ticks lockstep | Multi, replays, tests |
| Actions | Commands | Lockstep-ready |
| Données | JSON | Moddable, traduisible |
| Joueur | = Entité | Uniformité |
| Core/Engine | Séparés | Testable, portable |
| Inputs | Actions abstraites | Clavier + manette |
| Textes | JSON externalisé | Traductions |
| Grille blocs | 5×5×5 | Centre exact |
| Grille objets | 25×25×25 | Précision fine |
| Raycast | DDA | Standard, précis |
| Bruit terrain | Simplex | Meilleur en 3D |
