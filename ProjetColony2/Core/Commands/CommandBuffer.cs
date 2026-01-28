// ============================================================================
// COMMANDBUFFER.CS — File d'attente des commandes
// ============================================================================
// Le buffer STOCKE les commandes en attendant le moment de les exécuter.
//
// POURQUOI UN BUFFER ?
//   En lockstep, on ne peut pas exécuter une commande immédiatement.
//   Le flux est :
//     1. Joueur appuie sur Z → MoveCommand créée
//     2. MoveCommand ajoutée au buffer
//     3. (En multijoueur : commande envoyée aux autres joueurs)
//     4. Au tick suivant : ExecuteAll() exécute toutes les commandes
//     5. Buffer vidé, prêt pour le prochain tick
//
// ANALOGIE :
//   C'est comme une boîte aux lettres.
//   - Add() = déposer une lettre
//   - ExecuteAll() = relever le courrier et le traiter
//   - Clear() = vider la boîte après traitement
//
// LIST<T> :
//   List<ICommand> = une liste qui contient des objets ICommand.
//   Taille dynamique : on peut ajouter autant de commandes qu'on veut.
//   Méthodes utiles : Add(), Clear(), foreach pour parcourir.
// ============================================================================

using System.Collections.Generic;
using ProjetColony2.Core.Entities;

namespace ProjetColony2.Core.Commands;

public class CommandBuffer
{
    // La liste des commandes en attente d'exécution
    private List<ICommand> _commands;

    // Crée un buffer vide, prêt à recevoir des commandes
    public CommandBuffer()
    {
        _commands = new List<ICommand>();
    }

    // ========================================================================
    // ADD — Ajoute une commande au buffer
    // ========================================================================
    // Appelée quand le joueur fait une action (appuie sur une touche, clique...).
    // La commande attend dans le buffer jusqu'au prochain tick.
    public void Add(ICommand command)
    {
        _commands.Add(command);
    }

    // ========================================================================
    // EXECUTEALL — Exécute toutes les commandes puis vide le buffer
    // ========================================================================
    // Appelée une fois par tick par la simulation.
    //
    // ORDRE DES OPÉRATIONS :
    //   1. foreach : parcourt chaque commande
    //   2. Execute() : applique la commande (remplit l'Intent)
    //   3. Clear() : vide la liste APRÈS la boucle (pas pendant !)
    //
    // POURQUOI CLEAR APRÈS ?
    //   Si on vide pendant la boucle, C# lève une exception :
    //   "Collection was modified during enumeration"
    //   On ne peut pas modifier une liste qu'on parcourt.
    public void ExecuteAll(Entity entity)
    {
        foreach (ICommand command in _commands)
        {
            command.Execute(entity);    
        }

        _commands.Clear();
    }
}