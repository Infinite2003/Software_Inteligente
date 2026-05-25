using UnityEngine;

public static class RetreatSystem
{
    public static bool CanRetreat(PokemonInstance active)
    {
        if (active == null)
            return false;

        return active.attachedEnergy >= active.data.retreat_cost;
    }

    public static void Retreat(PokemonInstance active)
    {
        active.attachedEnergy -= active.data.retreat_cost;
    }
}
