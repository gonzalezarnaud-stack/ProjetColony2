// ============================================================================
// CUBEFACES.CS — Les 6 faces d'un cube pour le rendu optimisé
// ============================================================================
// Quand on dessine un cube, on ne dessine pas "un cube" mais 6 FACES.
// Chaque face est un carré (4 coins = 4 vertices).
//
// POURQUOI C'EST IMPORTANT ?
// On ne veut dessiner que les faces VISIBLES. Si un bloc de pierre est
// entouré d'autres blocs, aucune de ses faces n'est visible → on ne
// dessine rien → économie de performance !
//
// STRUCTURE D'UNE FACE :
//   - Direction : vers où pointe la face (ex: Top pointe vers Y+)
//   - Vertices : les 4 coins du carré
//   - Normal : vecteur perpendiculaire à la face (pour l'éclairage)
//
// ORDRE DES VERTICES (winding order) :
//   Le GPU a besoin de savoir quel côté de la face est "l'extérieur".
//   Il regarde dans quel sens les vertices tournent :
//     - Anti-horaire (vu de l'extérieur) = face visible
//     - Horaire (vu de l'extérieur) = face cachée (backface)
//   
//   Godot utilise l'anti-horaire. Si on se trompe, la face sera invisible
//   depuis l'extérieur mais visible depuis l'intérieur du cube !
//
// LES 6 FACES :
//   - Top (Y+) : dessus du cube
//   - Bottom (Y-) : dessous du cube
//   - North (Z-) : face arrière
//   - South (Z+) : face avant
//   - East (X+) : face droite
//   - West (X-) : face gauche
// ============================================================================

using Godot;

namespace ProjetColony2.Engine.Rendering;

// ============================================================================
// CUBEFACE — Une seule face de cube
// ============================================================================
// "struct" car c'est petit et on ne le modifie jamais après création.
//
// Direction : Vector3I (vecteur d'entiers) qui indique la direction.
//   Exemple : (0, 1, 0) = vers le haut
//
// Vertices : tableau de 4 Vector3, les coins de la face.
//   L'ordre compte ! (voir winding order ci-dessus)
//
// Normal : vecteur perpendiculaire à la face, de longueur 1.
//   Utilisé par le moteur pour calculer l'éclairage.
//   Une face qui "regarde" vers la lumière sera plus éclairée.
public struct CubeFace
{
    public Vector3I Direction;
    public Vector3[] Vertices;
    public Vector3 Normal;

    public CubeFace(Vector3I direction, Vector3[] vertices, Vector3 normal)
    {
        Direction = direction;
        Vertices = vertices;
        Normal = normal;
    }
}

// ============================================================================
// CUBEFACES — Les 6 faces pré-définies
// ============================================================================
// "static class" = on ne peut pas créer d'instance avec "new CubeFaces()".
// C'est juste un conteneur pour des données partagées.
//
// "readonly" = ces valeurs ne peuvent pas être modifiées après l'initialisation.
// C'est une sécurité : personne ne peut faire CubeFaces.Top = autreFace;
//
// CONVENTION D'AXES (Godot) :
//   Y+ = haut       Y- = bas
//   X+ = droite     X- = gauche  
//   Z+ = devant     Z- = derrière (vers l'écran)
public static class CubeFaces
{
    public static readonly CubeFace Top = new CubeFace(
        new Vector3I(0, 1, 0),
        new Vector3[] {
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1),
        },
        new Vector3(0, 1, 0)
    );

    public static readonly CubeFace Bottom = new CubeFace(
        new Vector3I(0, -1, 0),
        new Vector3[] {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
        },
        new Vector3(0, -1, 0)
    );

    public static readonly CubeFace North = new CubeFace(
        new Vector3I(0, 0, -1),
        new Vector3[] {
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
        },
        new Vector3(0, 0, -1)
    );

    public static readonly CubeFace South = new CubeFace(
        new Vector3I(0, 0, 1),
        new Vector3[] {
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
        },
        new Vector3(0, 0, 1)
    );

    public static readonly CubeFace East = new CubeFace(
        new Vector3I(1, 0, 0),
        new Vector3[] {
            new Vector3(1, 1, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
        },
        new Vector3(1, 0, 0)
    );

    public static readonly CubeFace West = new CubeFace(
        new Vector3I(-1, 0, 0),
        new Vector3[] {
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 0),
        },
        new Vector3(-1, 0, 0)
    );

    // Tableau contenant les 6 faces, pratique pour boucler dessus :
    // foreach (var face in CubeFaces.All) { ... }
    public static readonly CubeFace[] All = {Top, Bottom, North, South, East, West};
}