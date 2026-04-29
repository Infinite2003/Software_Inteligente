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
    public string name;
    public string description;
}
