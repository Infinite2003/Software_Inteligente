using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TCGPCard
{
    public string id;
    public string name;

    public CardCategory category;
    public PokemonStage sub_category;
    public PokemonType type;

    public int hp;
    public int retreat_cost;

    public string description;
    public string effect; // Añadido para los efectos únicos de los Entrenadores.

    public Weakness weakness; 
    public List<Ability> ability; // Pasado de 'Ability' individual a 'List<Ability>' porque el JSON envía un array

    public List<Move> moves;
}

[Serializable]
public class Move
{
    public List<string> cost;
    public string name;
    public string damage;
}

[System.Serializable]
public class Weakness
{
    public PokemonType type;
    public string value;
}

[System.Serializable]
public class Ability
{
    public string type;   // Añadido (ej. "Habilidad")
    public string name;
    public string effect; // Cambiado de 'description' a 'effect' según tu JSON (ej. de Butterfree)
}
