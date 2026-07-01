using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GestorVictoriaDerrota : MonoBehaviour
{
    [Header("Referencias de UI en Canvas (Objetos Existentes)")]
    [SerializeField] private GameObject objetoVictoria;
    [SerializeField] private GameObject objetoDerrota;
    [SerializeField] private GameObject botonRegresar;

    [Header("Zonas a Bloquear al Finalizar")]
    [SerializeField] private CanvasGroup contenedorFichasTablero;
    [SerializeField] private GameObject botonPasarTurno;

    [Header("Zonas de Validación de Cartas")]
    [SerializeField] private Transform zonaCartaJugada;
    [SerializeField] private Transform zonaBanca;

    private SincronizadorRed red;
    private bool juegoTerminado = false;

    // Guardamos las lambdas para poder desuscribirnos correctamente en OnDestroy
    private System.Action<ulong, ulong> _onTurnoCambiado;
    private System.Action _onRivalPerdio;

    void Start()
    {
        Debug.Log($"[GestorVictoriaDerrota] Start en '{gameObject.name}' | InstanceID={gameObject.GetInstanceID()} | Parent={transform.parent?.name ?? "ninguno"}");
        if (objetoVictoria != null) objetoVictoria.SetActive(false);
        if (objetoDerrota != null) objetoDerrota.SetActive(false);
        if (botonRegresar != null) botonRegresar.SetActive(false);

        // FIX: Siempre esperamos al sincronizador antes de hacer cualquier cosa
        StartCoroutine(EsperarSincronizador());
    }

    private IEnumerator EsperarSincronizador()
    {
        Debug.Log("[Sincronizador] Esperando SincronizadorRed...");
        yield return new WaitUntil(() => SincronizadorRed.Instancia != null);
        Debug.Log("[Sincronizador] SincronizadorRed encontrado. Iniciando bucle.");
        red = SincronizadorRed.Instancia;

        yield return null;

        ZonaTablero[] zonas = Object.FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None);
        bool soyHost = Unity.Netcode.NetworkManager.Singleton.IsHost;

        foreach (var z in zonas)
        {
            if (z.gameObject.name.Contains("_J1"))
                z.ForzarConfiguracion(true, true);
            else if (z.gameObject.name.Contains("_J2"))
                z.ForzarConfiguracion(false, true);
        }

        // Ahora buscar la zona correcta
        foreach (var z in zonas)
        {
            if (z.EsActivo() && z.EsMiZona())
            {
                zonaCartaJugada = z.transform;
                Debug.Log($"[Gestor] Zona encontrada: {z.gameObject.name}");
            }
        }

        Debug.Log($"[Gestor] IsHost={Unity.Netcode.NetworkManager.Singleton.IsHost} | " +
          $"zonaCartaJugada={(zonaCartaJugada != null ? zonaCartaJugada.name : "NULL")}");

        if (zonaCartaJugada == null)
            Debug.LogWarning("[GestorVictoriaDerrota] No se encontró zona activa local.");

        _onTurnoCambiado = (anterior, nuevo) =>
        {
            if (anterior == ulong.MaxValue) return;
            if (red.IsServer)
                red.IncrementarTurnosTranscurridos();
        };

        _onRivalPerdio = () => DefinirResultadoLocal(true);

        red.OnTurnoCambiado += _onTurnoCambiado;
        red.OnRivalPerdio += _onRivalPerdio;

        StartCoroutine(BucleMonitoreoPartida());
    }

    void OnDestroy()
    {
        if (red != null)
        {
            if (_onTurnoCambiado != null) red.OnTurnoCambiado -= _onTurnoCambiado;
            if (_onRivalPerdio != null) red.OnRivalPerdio -= _onRivalPerdio;
        }
    }

    private IEnumerator BucleMonitoreoPartida()
    {
        Debug.Log($"[Bucle] Iniciando espera. TurnosTotales={red?.TurnosTotales}");
        yield return new WaitUntil(() => red != null && red.TurnosTotales >= 2);

        Debug.Log("Fase de preparación terminada. Activando escáner de condiciones de victoria/derrota.");

        while (!juegoTerminado)
        {
            VerificarCondicionDeVida();
            yield return new WaitForSeconds(1.0f);
        }
    }

    public void VerificarCondicionDeVida()
    {
        Debug.Log($"[Verificación] zonaCartaJugada={(zonaCartaJugada != null ? zonaCartaJugada.name + " hijos:" + zonaCartaJugada.childCount : "NULL")} | zonaBanca={(zonaBanca != null ? zonaBanca.name + " hijos:" + zonaBanca.childCount : "NULL")}");
        if (juegoTerminado) return;

        //Debug.Log($"[Verificación] zonaCartaJugada={(zonaCartaJugada != null ? zonaCartaJugada.name + " hijos:" + zonaCartaJugada.childCount : "NULL")} | " + $"zonaBanca={(zonaBanca != null ? zonaBanca.name + " hijos:" + zonaBanca.childCount : "NULL")}");

        int pokemonEnActivo = ContarPokemonesEnContenedor(zonaCartaJugada);
        int pokemonEnBanca = ContarPokemonesEnContenedor(zonaBanca);

        //Debug.Log($"[Verificación] ClientId={Unity.Netcode.NetworkManager.Singleton.LocalClientId} | " + $"Activo={pokemonEnActivo} | Banca={pokemonEnBanca}");

        if (pokemonEnActivo == 0 && pokemonEnBanca == 0)
        {
            Debug.Log("[Derrota] Sin Pokémon en juego.");
            DefinirResultadoLocal(false);
            red.NotificarDerrotaServerRpc();
        }
    }

    private int ContarPokemonesEnContenedor(Transform contenedor)
    {
        if (contenedor == null) return 0;
        int contador = 0;
        foreach (Transform hijo in contenedor)
        {
            CardTablero cardUI = hijo.GetComponent<CardTablero>();
            if (cardUI != null && cardUI.cardData != null)
            {
                string categoria = cardUI.cardData.category.ToString().Trim();
                if (categoria.Equals("Pokemon", System.StringComparison.OrdinalIgnoreCase))
                    contador++;
            }
        }
        return contador;
    }

    public void DefinirResultadoLocal(bool ganeLaPartida)
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        if (botonRegresar != null) botonRegresar.SetActive(true);
        if (objetoVictoria != null) objetoVictoria.SetActive(ganeLaPartida);
        if (objetoDerrota != null) objetoDerrota.SetActive(!ganeLaPartida);
        if (botonPasarTurno != null) botonPasarTurno.SetActive(false);

        if (contenedorFichasTablero != null)
        {
            contenedorFichasTablero.blocksRaycasts = false;
            contenedorFichasTablero.interactable = false;
        }
    }

    public void VolverAlMenuPrincipal()
    {
        EstadoTableroLocal.yaHuboPokemonActivo.Clear();

        if (Unity.Netcode.NetworkManager.Singleton != null)
            Unity.Netcode.NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene("MainMenu");
    }
}