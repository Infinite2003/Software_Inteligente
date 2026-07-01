using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class ZonaTablero : MonoBehaviour
{
    [Header("¿A qué jugador pertenece esta zona?")]
    [SerializeField] private bool esDelHost = true;
    [SerializeField] private bool esActivo;
    public PokemonInstance pokemonEnZona { get; private set; }

    private int hpActual = 0;
    private int energiaActual = 0;
    private string idCarta = "";

    void Awake()
    {
        string nombre = gameObject.name;

        if (nombre.Contains("_J1"))
        {
            esDelHost = true;
            esActivo = true;
        }
        else if (nombre.Contains("_J2"))
        {
            esDelHost = false;
            esActivo = true;
        }
        else
        {
            Debug.LogWarning($"[ZonaTablero] '{nombre}' no tiene sufijo _J1 o _J2. " +
                             $"Usando valores del Inspector: esDelHost={esDelHost}, esActivo={esActivo}");
        }

        Debug.Log($"[ZonaTablero] '{nombre}' inicializado → esDelHost={esDelHost}, esActivo={esActivo}");
        Debug.Log($"Debug GPT: [{gameObject.name}] pokemonEnZona = {pokemonEnZona}");
    }

    public bool EsMiZona()
    {
        // Si la red no está activa (modo offline/editor), confiamos en el Inspector
        if (Unity.Netcode.NetworkManager.Singleton == null ||
            !Unity.Netcode.NetworkManager.Singleton.IsListening)
            return esDelHost;

        bool soyHost = Unity.Netcode.NetworkManager.Singleton.IsHost;
        Debug.Log($"[ZonaTablero] '{gameObject.name}' | soyHost={soyHost} | esDelHost={esDelHost} | resultado={soyHost == esDelHost}");
        return soyHost == esDelHost;
    }

    public bool EsActivo() => esActivo;

    public bool EstaOcupada() => pokemonEnZona != null;

    public void ColocarPokemon(TCGPCard cartaData)
    {
        pokemonEnZona = new PokemonInstance(cartaData);
        hpActual = pokemonEnZona.currentHP;
        idCarta = cartaData.id;
        Debug.Log($"[ZonaTablero] {cartaData.name} colocado en '{gameObject.name}' " +
                  $"({(EsMiZona() ? "LOCAL" : "RIVAL")})");
    }

    public void LiberarZona()
    {
        pokemonEnZona = null;
        hpActual = 0;
        energiaActual = 0;
        idCarta = "";
    }

    // Cuando recibes daño del rival, actualizas tu propia NetworkVariable
    public void RecibirDaño(int cantidad)
    {
        if (pokemonEnZona == null) return;
        pokemonEnZona.TakeDamage(cantidad);
        hpActual = pokemonEnZona.currentHP;
        Debug.Log($"[ZonaTablero] '{gameObject.name}' recibió {cantidad} de daño. HP restante: {hpActual}");
    }

    public void ForzarConfiguracion(bool delHost, bool activo)
    {
        esDelHost = delHost;
        esActivo = activo;
    }
}