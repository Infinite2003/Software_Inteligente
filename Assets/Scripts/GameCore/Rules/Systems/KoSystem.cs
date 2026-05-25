using UnityEngine;

public static class KoSystem 
{
    public static bool CheckKO(PokemonInstance pokemon)
    {
        if (pokemon == null)
            return false;

        return pokemon.currentHP <= 0;
    }
}
