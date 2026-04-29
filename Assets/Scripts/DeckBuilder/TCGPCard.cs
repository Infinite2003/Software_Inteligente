using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TCGPCard
{
    public string id; // Cambiado a string (Ej: "A1-001")
    public string set_number;
    public string name;
    public string category; // "Pokémon", "Trainer", etc.
    public string sub_category;
    public int hp;
    public int retreat_cost;
    public string rarity; // Nueva propiedad añadida
    public string type;
    public Weakness weakness; // Cambiado de string a la clase Weakness
    public string description;
    public List<Move> moves;
    public Ability ability;
    public List<string> packs;
}

[System.Serializable]
public class Move
{
    public List<string> cost; // Cambiado de List<Cost> a List<string> que lee el array {"Planta", "Incolora"}
    public string name;
    public string damage;
}

[System.Serializable]
public class Weakness
{
    public string type;
    public string value;
}

[System.Serializable]
public class Ability
{
    public string name;
    public string description;
}
