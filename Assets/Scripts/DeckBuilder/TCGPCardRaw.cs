using System;
using System.Collections.Generic;

[Serializable]
public class TCGPCardRaw
{
    public string id;
    public string set_number;
    public string name;
    public string category;
    public string sub_category;
    public int hp;
    public int retreat_cost;
    public string rarity;
    public string type;
    public WeaknessRaw weakness;
    public string description;
    public List<MoveRaw> moves;
    public AbilityRaw ability;
}

[Serializable]
public class MoveRaw
{
    public List<string> cost;
    public string name;
    public string damage;
}

[Serializable]
public class WeaknessRaw
{
    public string type;
    public string value;
}

[Serializable]
public class AbilityRaw
{
    public string name;
    public string description;
}