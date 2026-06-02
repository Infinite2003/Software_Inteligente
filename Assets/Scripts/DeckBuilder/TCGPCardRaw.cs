using System;
using System.Collections.Generic;

[Serializable]
public class TCGPCardRaw
{
    public string id;
    public string set_number;
    public string name;
    public string category;
    public string trainer_type; // "Partidario", "Objeto", etc.
    public string stage;        // "Básico", "Fase 1", "Fase 2"
    public string evolve_from;  // Nombre del Pokémon previo en la cadena evolutiva
    public int hp;
    public int retreat_cost;
    public string rarity;
    public string type;
    public WeaknessRaw weakness;
    public string description;
    public string effect;
    public List<MoveRaw> moves;
    public List<AbilityRaw> ability;
    public List<PackRaw> packs;
}

[Serializable]
public class MoveRaw
{
    public List<string> cost;
    public string name;
    public string damage;
    public string effect; // Efecto específico del movimiento
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

[Serializable]
public class PackRaw
{
    public string id;
    public string name;
}