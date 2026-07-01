using UnityEngine;

public class PokemonSelector : MonoBehaviour
{
    public static PokemonSelector Instance;

    private System.Action<PokemonInstance> callback;

    void Awake()
    {
        Instance = this;
    }

    public void StartSelection(System.Action<PokemonInstance> onSelected)
    {
        callback = onSelected;
    }

    public void SelectPokemon(PokemonInstance pokemon)
    {
        callback?.Invoke(pokemon);
        callback = null;
    }
}
