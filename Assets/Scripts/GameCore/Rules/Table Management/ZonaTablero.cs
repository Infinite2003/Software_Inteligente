using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class ZonaTablero : NetworkBehaviour
{
    [Header("¿A qué jugador pertenece esta zona?")]
    [SerializeField] private bool esDelHost = true;
    [SerializeField] private bool esActivo;
    public PokemonInstance pokemonEnZona { get; private set; }

    public bool EsMiZona()
    {
        if (!NetworkManager.Singleton.IsListening) return esDelHost;
        bool soyHost = NetworkManager.Singleton.IsHost;
        return soyHost == esDelHost;
    }

    public void ColocarPokemon(TCGPCard cartaData)
    {
        pokemonEnZona = new PokemonInstance(cartaData);
        Debug.Log($"{cartaData.name} colocado en zona de {(EsMiZona() ? "LOCAL" : "RIVAL")}");
    }

    public void LiberarZona()
    {
        pokemonEnZona = null;
    }

    private NetworkVariable<int> hpActual = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner  // solo el dueño actualiza su propio HP
);

    private NetworkVariable<int> energiaActual = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<FixedString64Bytes> idCarta = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // Cuando recibes daño del rival, actualizas tu propia NetworkVariable
    public void RecibirDaño(int cantidad)
    {
        if (pokemonEnZona == null) return;
        pokemonEnZona.TakeDamage(cantidad);
        hpActual.Value = pokemonEnZona.currentHP; // esto se replica al rival automáticamente
    }

    public bool EsActivo()
    {
        return esActivo;
    }
    public bool EstaOcupada() => pokemonEnZona != null;
}