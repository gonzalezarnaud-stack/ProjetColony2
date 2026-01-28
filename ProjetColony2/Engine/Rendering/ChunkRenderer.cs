// ============================================================================
// CHUNKRENDERER.CS — Affiche un chunk à l'écran (version optimisée)
// ============================================================================
// Ce fichier est dans ENGINE (pas Core) car il utilise Godot.
//
// SON RÔLE :
// Transformer les DONNÉES (Chunk) en VISUEL (mesh 3D).
// C'est le traducteur entre le monde abstrait (des nombres) et ce que
// tu vois à l'écran (des formes 3D colorées et éclairées).
//
// ============================================================================
// POURQUOI UNE VERSION "OPTIMISÉE" ?
// ============================================================================
//
// VERSION SIMPLE (qu'on avait avant) :
//   - 1 voxel solide = 1 objet MeshInstance3D
//   - 1000 blocs = 1000 objets dans l'arbre de scène Godot
//   - Chaque objet = mémoire + calculs de transformation + draw call
//   - PROBLÈME : Le GPU passe son temps à changer d'objet au lieu de dessiner
//
// VERSION OPTIMISÉE (ce fichier) :
//   - 1 chunk ENTIER = 1 seul objet MeshInstance3D
//   - Peu importe s'il y a 100 ou 4000 blocs, c'est UN mesh
//   - Le GPU dessine tout d'un coup = BEAUCOUP plus rapide
//
// MAIS CE N'EST PAS TOUT ! On optimise aussi CE QU'ON DESSINE :
//   - Un bloc entouré d'autres blocs → aucune face visible → on ne dessine RIEN
//   - Seules les faces "à l'air libre" sont ajoutées au mesh
//
// EXEMPLE CONCRET :
//   Un cube solide de 10×10×10 blocs = 1000 blocs
//   - Chaque bloc a 6 faces = 6000 faces théoriques
//   - Mais seules les faces EXTÉRIEURES sont visibles
//   - Surface du cube : 6 faces × 10×10 = 600 faces
//   - On dessine 600 faces au lieu de 6000 = 10× moins de travail !
//
// ============================================================================
// COMMENT ÇA MARCHE ?
// ============================================================================
//
// ÉTAPE 1 : On parcourt chaque voxel du chunk (16×16×16 = 4096)
//
// ÉTAPE 2 : Pour chaque voxel SOLIDE, on regarde ses 6 voisins
//           (haut, bas, gauche, droite, devant, derrière)
//
// ÉTAPE 3 : Si un voisin est AIR (ou hors du chunk) → cette face est VISIBLE
//           On l'ajoute au mesh.
//
// ÉTAPE 4 : Si un voisin est SOLIDE → cette face est CACHÉE
//           On ne fait rien (on économise des triangles).
//
// ÉTAPE 5 : À la fin, on a un mesh avec UNIQUEMENT les faces visibles.
//
// ============================================================================
// CONCEPTS IMPORTANTS
// ============================================================================
//
// SURFACETOOL :
//   Outil Godot pour construire un mesh "à la main", vertex par vertex.
//   C'est comme dessiner point par point au lieu d'utiliser une forme prête.
//   Plus de travail, mais contrôle total sur le résultat.
//
// VERTEX (pluriel : VERTICES) :
//   Un "coin" dans l'espace 3D. Un triangle a 3 vertices, un carré en a 4.
//   Chaque vertex a une position (x, y, z) et d'autres infos (normale, UV...).
//
// NORMALE :
//   Un vecteur perpendiculaire à une surface. Utilisé pour l'ÉCLAIRAGE.
//   Si la normale pointe vers la lumière → surface bien éclairée.
//   Si la normale pointe à l'opposé → surface sombre.
//
// TRIANGLE :
//   Le GPU ne sait dessiner QUE des triangles. Tout le reste (carrés, cercles,
//   personnages...) est décomposé en triangles. Un carré = 2 triangles.
//
// "partial class" :
//   Obligatoire pour toute classe héritant d'un nœud Godot.
//   Godot génère du code C# automatiquement pour faire le lien avec le moteur.
//   "partial" permet de fusionner ton code avec le code généré.
//
// ": Node3D" :
//   ChunkRenderer HÉRITE de Node3D, donc :
//   - C'est un objet 3D avec position, rotation, échelle
//   - Il peut avoir des enfants (notre MeshInstance3D)
//   - Il peut être ajouté à l'arbre de scène Godot
// ============================================================================

using Godot;
using ProjetColony2.Core.World;

namespace ProjetColony2.Engine.Rendering;

public partial class ChunkRenderer : Node3D
{
    // ========================================================================
    // CHAMPS — Les données que ChunkRenderer garde en mémoire
    // ========================================================================
    //
    // _chunk :
    //   Le chunk qu'on affiche. Vient de Core (données pures, pas de Godot).
    //   On le garde pour pouvoir RÉGÉNÉRER le mesh si le chunk change.
    //   Exemple : le joueur casse un bloc → on doit recalculer le mesh.
    //
    // _meshInstance :
    //   L'objet Godot qui AFFICHE le mesh dans la scène.
    //   MeshInstance3D = "montre ce mesh à l'écran à cette position".
    //   On en crée UN SEUL pour tout le chunk (c'est l'optimisation !).
    //
    // CONVENTION :
    //   Le underscore "_" indique un champ privé.
    //   "private" = seul ChunkRenderer peut y accéder, pas les autres classes.
    private Chunk _chunk;

    // Le mesh 3D qui sera affiché
    // Un seul MeshInstance3D pour tout le chunk !
    private MeshInstance3D _meshInstance;

    // ========================================================================
    // INITIALIZE — Configure le renderer avec un chunk
    // ========================================================================
    // Appelée UNE FOIS après la création, pour donner les données à afficher.
    //
    // POURQUOI PAS LE CONSTRUCTEUR ?
    //   En Godot, les constructeurs C# sont délicats. Le nœud n'est pas
    //   encore "prêt" dans le constructeur (pas dans l'arbre de scène).
    //   
    //   Pattern recommandé :
    //     1. var renderer = new ChunkRenderer();  // Crée l'objet
    //     2. renderer.Initialize(chunk);          // Configure avec les données
    //     3. AddChild(renderer);                  // Ajoute à la scène
    //
    // CE QUE FAIT INITIALIZE :
    //   1. Stocke le chunk dans _chunk (pour usage futur)
    //   2. Crée un MeshInstance3D vide
    //   3. L'ajoute comme enfant (sinon invisible !)
    //   4. Appelle GenerateMesh() pour construire le mesh
    public void Initialize(Chunk chunk)
    {
        _chunk = chunk;

        _meshInstance = new MeshInstance3D();
        AddChild(_meshInstance);

        GenerateMesh();
    }

    // ========================================================================
    // GENERATEMESH — Construit le mesh optimisé du chunk
    // ========================================================================
    // C'est le CŒUR de l'optimisation. On construit un mesh sur mesure
    // avec uniquement les faces visibles.
    //
    // ========================================================================
    // ÉTAPE PAR ÉTAPE :
    // ========================================================================
    //
    // 1. CRÉER LE SURFACETOOL
    //    var st = new SurfaceTool();
    //    → Notre "pinceau" pour dessiner le mesh vertex par vertex.
    //
    //    st.Begin(Mesh.PrimitiveType.Triangles);
    //    → On dit : "je vais dessiner des triangles".
    //    → Chaque groupe de 3 vertices ajoutés = 1 triangle.
    //
    // 2. PARCOURIR TOUS LES VOXELS
    //    Trois boucles imbriquées : x de 0 à 15, y de 0 à 15, z de 0 à 15.
    //    → 16 × 16 × 16 = 4096 itérations.
    //
    // 3. IGNORER L'AIR
    //    if (voxel.IsAir) continue;
    //    → L'air n'a pas de faces à dessiner, on passe au suivant.
    //
    // 4. POUR CHAQUE VOXEL SOLIDE, VÉRIFIER LES 6 FACES
    //    foreach (CubeFace face in CubeFaces.All)
    //    → On boucle sur Top, Bottom, North, South, East, West.
    //
    // 5. CALCULER LA POSITION DU VOISIN
    //    nx = x + face.Direction.X
    //    → Si on regarde la face East (droite), Direction = (1, 0, 0)
    //    → Donc le voisin est à x+1, y, z.
    //
    // 6. SI LE VOISIN EST AIR → DESSINER LA FACE
    //    if (IsAirAt(nx, ny, nz)) AddFace(st, position, face);
    //    → Face visible ! On l'ajoute au mesh.
    //
    //    SI LE VOISIN EST SOLIDE → NE RIEN FAIRE
    //    → Face cachée entre deux blocs, inutile de la dessiner.
    //
    // 7. FINALISER LE MESH
    //    st.GenerateNormals();
    //    → Calcule automatiquement les normales pour l'éclairage.
    //    → On pourrait les définir nous-mêmes, mais c'est plus simple ainsi.
    //
    //    _meshInstance.Mesh = st.Commit();
    //    → Commit() = "j'ai fini, donne-moi le mesh final".
    //    → On l'assigne au MeshInstance3D pour qu'il s'affiche.
    //
    // ========================================================================
    // POURQUOI Vector3 position = new Vector3(x, y, z) ?
    // ========================================================================
    // Les vertices dans CubeFaces sont en coordonnées LOCALES (0 à 1).
    // On doit les décaler à la position du voxel dans le chunk.
    //
    // Exemple :
    //   Voxel en (5, 0, 3)
    //   Face Top, vertex (0, 1, 0)
    //   Position finale = (5, 0, 3) + (0, 1, 0) = (5, 1, 3)
    private void GenerateMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int z = 0; z < Chunk.Size; z++)
                {
                    Voxel voxel = _chunk.GetVoxel(x, y, z);

                    if (voxel.IsAir)
                    {
                        continue;
                    }

                    Vector3 position = new Vector3(x, y, z);

                    foreach (CubeFace face in CubeFaces.All)
                    {
                        int nx = x + face.Direction.X;
                        int ny = y + face.Direction.Y;
                        int nz = z + face.Direction.Z;

                        if (IsAirAt(nx, ny, nz))
                        {
                            AddFace(st, position, face);
                        }
                    }
                }
            }
        }

        st.GenerateNormals();
        _meshInstance.Mesh = st.Commit();

        // ================================================================
        // MATÉRIAU — Apparence visuelle du mesh
        // ================================================================
        // CullMode.Disabled = les deux côtés des faces sont visibles.
        // Sans ça, les faces "de l'intérieur" seraient invisibles
        // (optimisation GPU par défaut appelée "back-face culling").
        //
        // AlbedoColor = la couleur de base de la surface.
        // Futur : couleur différente selon le matériau du voxel.
        var material = new StandardMaterial3D();
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        material.AlbedoColor = new Color(0.6f, 0.6f, 0.6f);
        _meshInstance.MaterialOverride = material;
    }

    // ========================================================================
    // ISAIRAT — Vérifie si une position est de l'air (ou hors limites)
    // ========================================================================
    // Cette méthode répond à la question : "Y a-t-il de l'air ici ?"
    //
    // RETOURNE TRUE SI :
    //   - La position est HORS DU CHUNK (x, y ou z < 0 ou >= 16)
    //   - OU le voxel à cette position EST DE L'AIR
    //
    // RETOURNE FALSE SI :
    //   - Le voxel à cette position est SOLIDE
    //
    // ========================================================================
    // POURQUOI "HORS LIMITES = AIR" ?
    // ========================================================================
    // Imagine un bloc au bord du chunk, position (0, 5, 5).
    // Sa face WEST (gauche) a un voisin en (-1, 5, 5).
    // Mais -1 est hors du chunk ! Que faire ?
    //
    // Option 1 : Ne pas dessiner la face → MAUVAIS
    //   On verrait un trou sur les bords du chunk.
    //
    // Option 2 : Considérer "hors limites" comme de l'air → CORRECT
    //   La face du bord est visible, on la dessine.
    //
    // NOTE POUR PLUS TARD :
    // Quand on aura plusieurs chunks côte à côte, on devra vérifier
    // le voxel dans le chunk VOISIN au lieu de dire "c'est de l'air".
    // Mais pour l'instant, un seul chunk, donc cette simplification suffit.
    //
    // ========================================================================
    // ANALYSE DU CODE
    // ========================================================================
    //
    // if (x < 0 || x >= Chunk.Size || y < 0 || ...)
    //   → "||" signifie "OU". Si UNE condition est vraie, tout est vrai.
    //   → On vérifie les 6 limites (min et max pour x, y, z).
    //   → Chunk.Size = 16, donc valeurs valides = 0 à 15.
    //
    // return true;
    //   → Hors limites = on considère que c'est de l'air.
    //
    // return _chunk.GetVoxel(x, y, z).IsAir;
    //   → Dans les limites = on demande au chunk si c'est de l'air.
    private bool IsAirAt (int x, int y, int z)
    {
        if (x < 0 || x >= Chunk.Size ||
            y < 0 || y >= Chunk.Size ||
            z < 0 || z >= Chunk.Size)
        {
            return true;
        }
        return _chunk.GetVoxel(x, y, z).IsAir;
    }

    // ========================================================================
    // ADDFACE — Ajoute une face (2 triangles) au mesh en construction
    // ========================================================================
    // Une face de cube = un CARRÉ = 4 coins.
    // Mais les GPU ne savent dessiner que des TRIANGLES !
    // Donc on décompose le carré en 2 triangles.
    //
    // ========================================================================
    // DÉCOMPOSITION D'UN CARRÉ EN TRIANGLES
    // ========================================================================
    //
    //   v[0]─────v[1]        Les 4 vertices (coins) du carré
    //    │ \       │         numérotés 0, 1, 2, 3.
    //    │   \     │
    //    │     \   │         Triangle 1 : v[0] → v[1] → v[2]
    //    │       \ │         Triangle 2 : v[0] → v[2] → v[3]
    //   v[3]─────v[2]
    //
    // POURQUOI CET ORDRE ?
    //   Les deux triangles partagent le côté v[0]-v[2] (la diagonale).
    //   C'est la façon standard de découper un quad en triangles.
    //
    // ========================================================================
    // SETNORMAL() AVANT ADDVERTEX()
    // ========================================================================
    // En Godot/SurfaceTool, on CONFIGURE d'abord, puis on AJOUTE.
    //
    //   st.SetNormal(normal);   ← "Le prochain vertex aura cette normale"
    //   st.AddVertex(position); ← "Ajoute un vertex avec la config actuelle"
    //
    // C'est pour ça qu'on répète SetNormal() 6 fois (une par vertex).
    // Tous les vertices d'une face ont la MÊME normale (face plate).
    //
    // ========================================================================
    // POSITION + V[I] : LE DÉCALAGE
    // ========================================================================
    // Les vertices dans CubeFaces.Top sont RELATIFS au coin (0,0,0) :
    //   v[0] = (0, 1, 0), v[1] = (1, 1, 0), etc.
    //
    // Mais notre bloc peut être n'importe où dans le chunk !
    // Si le bloc est en position (5, 0, 3), on doit DÉCALER les vertices :
    //
    //   position = (5, 0, 3)
    //   v[0] = (0, 1, 0)
    //   position + v[0] = (5, 1, 3)  ← Position FINALE du vertex
    //
    // C'est comme dire : "le cube est là-bas, donc ses coins aussi".
    private void AddFace(SurfaceTool st, Vector3 position, CubeFace face)
    {
        Vector3[] v = face.Vertices;

        st.SetNormal(face.Normal);
        st.AddVertex(position + v[0]);
        st.SetNormal(face.Normal);
        st.AddVertex(position + v[1]);
        st.SetNormal(face.Normal);
        st.AddVertex(position + v[2]);

        st.SetNormal(face.Normal);
        st.AddVertex(position + v[0]);
        st.SetNormal(face.Normal);
        st.AddVertex(position + v[2]);
        st.SetNormal(face.Normal);
        st.AddVertex(position + v[3]);
    }

    // ========================================================================
    // REBUILD — Reconstruit le mesh après modification du chunk
    // ========================================================================
    // Appelé quand un voxel est cassé ou posé.
    // Le mesh doit être régénéré pour refléter les changements.
    //
    // POURQUOI UNE MÉTHODE SÉPARÉE ?
    //   GenerateMesh() est private (détail d'implémentation).
    //   Rebuild() est public (interface pour l'extérieur).
    //   C'est une bonne pratique d'encapsulation.
    public void Rebuild()
    {
        GenerateMesh();
    }

}