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

    private Transform zonaActivaJ1;
    private Transform zonaActivaJ2;
    private Transform bancaJ1;
    private Transform bancaJ2;

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
        red = SincronizadorRed.Instancia;
        yield return new WaitForSeconds(0.5f);

        // En EsperarSincronizador, reemplaza la asignación de bancas por esto:
        ZonaTablero[] zonas = Object.FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None);
        Transform bancaCompartida = null;

        foreach (var z in zonas)
        {
            if (z.gameObject.name == "CartaJugada_J1") zonaActivaJ1 = z.transform;
            if (z.gameObject.name == "CartaJugada_J2") zonaActivaJ2 = z.transform;
            if (z.gameObject.name == "Banca") bancaCompartida = z.transform;
        }

        // Ambos jugadores comparten la misma banca visual por ahora
        bancaJ1 = bancaCompartida;
        bancaJ2 = bancaCompartida;

        Debug.Log($"[Gestor] J1: activo={zonaActivaJ1?.name} banca={bancaJ1?.name} | " +
                  $"J2: activo={zonaActivaJ2?.name} banca={bancaJ2?.name}");

        ulong id = Unity.Netcode.NetworkManager.Singleton.LocalClientId;

        foreach (var z in zonas)
        {
            if (id == 0 && z.name == "CartaJugada_J1")
            {
                zonaCartaJugada = z.transform;
            }

            if (id == 1 && z.name == "CartaJugada_J2")
            {
                zonaCartaJugada = z.transform;
            }
        }

        Debug.Log($"[Gestor] LocalClientId={id} | Zona asignada={zonaCartaJugada?.name}");

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

        // Esperar además a que ambas zonas activas tengan al menos un Pokémon
        yield return new WaitUntil(() =>
            ContarPokemonesEnContenedor(zonaActivaJ1) > 0 &&
            ContarPokemonesEnContenedor(zonaActivaJ2) > 0
        );

        Debug.Log("Fase de preparación terminada. Activando escáner de condiciones de victoria/derrota.");

        while (!juegoTerminado)
        {
            VerificarCondicionDeVida();
            yield return new WaitForSeconds(1.0f);
        }
    }

    public void VerificarCondicionDeVida()
    {
        if (juegoTerminado) return;

        int activoJ1 = ContarPokemonesEnContenedor(zonaActivaJ1);
        int activoJ2 = ContarPokemonesEnContenedor(zonaActivaJ2);
        int enBanca = ContarPokemonesEnContenedor(bancaJ1);

        Debug.Log($"[Verificación] activoJ1={activoJ1} activoJ2={activoJ2} enBanca={enBanca}");


        // Un jugador pierde si su zona activa está vacía y no hay nada en banca
        bool j1SinPokemon = activoJ1 == 0 && enBanca == 0;
        bool j2SinPokemon = activoJ2 == 0 && enBanca == 0;

        if (!j1SinPokemon && !j2SinPokemon) return;

        var ids = Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds;
        if (ids.Count < 2) return;

        ulong perdedor;

        if (j1SinPokemon && j2SinPokemon)
            perdedor = Unity.Netcode.NetworkManager.Singleton.LocalClientId; // empate = todos pierden
        else if (j1SinPokemon)
            perdedor = ids[0]; // J1 perdió
        else
            perdedor = ids[1]; // J2 perdió

        // Solo el servidor o el que detectó notifica, para evitar llamadas dobles
        if (!juegoTerminado)
            red.NotificarDerrotaServerRpc(perdedor);
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