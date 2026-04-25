using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TCGPCard
{
    public int id;
    public string set_number;
    public string name;
    public string category;
    public string sub_category;
    public int hp;
    public int retreat_cost;
    public string type;
    public string weakness;
    public string description;
    public List<Move> moves;
    public Ability ability;
    public List<string> packs;
}

[System.Serializable]
public class Move
{
    public string name;
    public string damage;
    public List<Cost> costs;
}

[System.Serializable]
public class Cost
{
    public string type;
    public int amount;
}

[System.Serializable]
public class Ability
{
    public string name;
    public string description;
}
