using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TCGPCard
{
    public string id;
    public string set_number;
    public string name;

    public CardCategory category;
    public PokemonStage sub_category;
    public PokemonType type;

    public int hp;
    public int retreat_cost;

    public string rarity;
    public string evolve_from;

    public string description;
    public string effect;

    public Weakness weakness;
    public List<Ability> ability;
    public List<Move> moves;
    public List<Pack> packs;

    public string image_url;
}

[Serializable]
public class Move
{
    public List<string> cost;
    public string name;
    public string damage;
    public string effect;
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
    public string type;
    public string name;
    public string effect;
}

[System.Serializable]
public class Pack
{
    public string id;
    public string name;
}