using System;
using System.Collections.Generic;

[Serializable]
public class TCGPCardRaw
{
    public string id;
    public string name;
    public string category;
    public string sub_category;
    public string stage;
    public int hp;
    public int retreat_cost;
    public string type;
    public WeaknessRaw weakness;
    public string description;
    public string effect;
    public List<MoveRaw> moves;
    public List<AbilityRaw> ability;
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
    public string type;
    public string name;
    public string effect;
}