using UnityEngine;

public static class EnergySystem
{
    public static bool AttachEnergy(
        PokemonInstance target,
        int amount = 1)
    {
        if (target == null)
            return false;

        target.attachedEnergy += amount;

        return true;
    }

    public static bool TransferEnergy(
        PokemonInstance source,
        PokemonInstance target,
        int amount)
    {
        if (source == null || target == null)
            return false;

        if (source.attachedEnergy < amount)
            return false;

        source.attachedEnergy -= amount;
        target.attachedEnergy += amount;

        return true;
    }
}
