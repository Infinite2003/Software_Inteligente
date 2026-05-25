using UnityEngine;

public static class EnergySystem 
{
    public static bool AttachEnergy(PokemonInstance target)
    {
        if (target == null)
            return false;

        target.attachedEnergy++;

        Debug.Log($"Energía unida a {target.data.name}");

        return true;
    }
}
