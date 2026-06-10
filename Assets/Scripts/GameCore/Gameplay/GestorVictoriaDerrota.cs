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

    private ControladorTurnos sistemaTurnos;
    private SincronizadorRed red;
    private bool juegoTerminado = false;
    private bool _ultimoEstadoTurno;

    void Start()
    {
        if (objetoVictoria != null) objetoVictoria.SetActive(false);
        if (objetoDerrota != null) objetoDerrota.SetActive(false);
        if (botonRegresar != null) botonRegresar.SetActive(false);

        sistemaTurnos = Object.FindFirstObjectByType<ControladorTurnos>();

        StartCoroutine(BucleMonitoreoPartida());
    }

    private IEnumerator EsperarSincronizador()
    {
        yield return new WaitUntil(() => SincronizadorRed.Instancia != null);

        red = SincronizadorRed.Instancia;

        // Escuchamos el evento de que el rival perdió (nos toca mostrar victoria)
        red.OnRivalPerdio += () => DefinirResultadoLocal(true);

        if (sistemaTurnos != null)
            _ultimoEstadoTurno = sistemaTurnos.EsMiTurno();

        StartCoroutine(BucleMonitoreoPartida());
    }

    void OnDestroy()
    {
        if (red != null)
            red.OnRivalPerdio -= () => DefinirResultadoLocal(true);
    }

    void Update()
    {
        // Solo el servidor lleva la cuenta de turnos transcurridos
        if (red == null || sistemaTurnos == null || juegoTerminado) return;
        if (!red.IsServer) return;

        bool turnoActual = sistemaTurnos.EsMiTurno();
        if (turnoActual != _ultimoEstadoTurno)
        {
            _ultimoEstadoTurno = turnoActual;
            red.IncrementarTurnosTranscurridos();
        }
    }


    private IEnumerator BucleMonitoreoPartida()
    {
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
        if (juegoTerminado) return;

        int pokemonEnActivo = ContarPokemonesEnContenedor(zonaCartaJugada);
        int pokemonEnBanca = ContarPokemonesEnContenedor(zonaBanca);

        Debug.Log($"[Verificación] ClientId={Unity.Netcode.NetworkManager.Singleton.LocalClientId} | " +
                  $"Activo={pokemonEnActivo} | Banca={pokemonEnBanca}");

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