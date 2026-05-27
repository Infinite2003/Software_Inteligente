using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode; // Requisito de red para Unity 6

public class GestorVictoriaDerrota : NetworkBehaviour
{
    [Header("Referencias de UI en Canvas (Objetos Existentes)")]
    [SerializeField] private GameObject objetoVictoria;   // Tu objeto 'Victoria'
    [SerializeField] private GameObject objetoDerrota;     // Tu objeto 'Derrota'
    [SerializeField] private GameObject botonRegresar;     // Tu objeto 'Regresar'

    [Header("Zonas a Bloquear al Finalizar")]
    [SerializeField] private CanvasGroup contenedorFichasTablero;
    [SerializeField] private GameObject botonPasarTurno;

    [Header("Zonas de Validación de Cartas")]
    [SerializeField] private Transform zonaCartaJugada;
    [SerializeField] private Transform zonaBanca;

    private ControladorTurnos sistemaTurnos;
    private bool juegoTerminado = false;

    // Control de turnos global compartido por la red
    private NetworkVariable<int> turnosTotalesTranscurridos = new NetworkVariable<int>(0);
    private bool _ultimoEstadoTurno;

    void Start()
    {
        if (objetoVictoria != null) objetoVictoria.SetActive(false);
        if (objetoDerrota != null) objetoDerrota.SetActive(false);
        if (botonRegresar != null) botonRegresar.SetActive(false);

        sistemaTurnos = Object.FindFirstObjectByType<ControladorTurnos>();

        if (sistemaTurnos != null)
        {
            _ultimoEstadoTurno = sistemaTurnos.EsMiTurno();
        }

        StartCoroutine(BucleMonitoreoPartida());
    }

    void Update()
    {
        if (!IsSpawned || sistemaTurnos == null || juegoTerminado) return;

        // Solo el dueño del turno actual actualiza el contador de red cuando pasa su turno
        bool turnoActual = sistemaTurnos.EsMiTurno();
        if (turnoActual != _ultimoEstadoTurno)
        {
            _ultimoEstadoTurno = turnoActual;

            // Si el turno cambió, le sumamos 1 al contador global de la partida
            if (IsServer)
            {
                turnosTotalesTranscurridos.Value++;
            }
            else
            {
                ActualizarTurnoServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActualizarTurnoServerRpc()
    {
        turnosTotalesTranscurridos.Value++;
    }

    private IEnumerator BucleMonitoreoPartida()
    {
        // REGLA DE ORO: Esperamos hasta que hayan pasado al menos 2 cambios de turno en la red LAN.
        // Esto garantiza que el Jugador 1 y el Jugador 2 ya tuvieron su oportunidad de bajar cartas de la mano.
        yield return new WaitUntil(() => turnosTotalesTranscurridos.Value >= 2);

        Debug.Log("Fase de preparación terminada. Activando escáner de condiciones de victoria/derrota.");

        // Bucle de escaneo activo una vez pasada la preparación
        while (!juegoTerminado)
        {
            if (IsOwner)
            {
                VerificarCondicionDeVida();
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    public void VerificarCondicionDeVida()
    {
        if (juegoTerminado) return;

        int pokemonEnActivo = ContarPokemonesEnContenedor(zonaCartaJugada);
        int pokemonEnBanca = ContarPokemonesEnContenedor(zonaBanca);

        // Si ya pasó el tiempo inicial de gracia y te quedas en 0 Pokémon...
        if (pokemonEnActivo == 0 && pokemonEnBanca == 0)
        {
            DefinirResultadoLocal(false); // Derrota local
            NotificarResultadoAlRivalServerRpc(); // Informa victoria al rival por LAN
        }
    }

    private int ContarPokemonesEnContenedor(Transform contenedor)
    {
        if (contenedor == null) return 0;
        int contador = 0;
        foreach (Transform hijo in contenedor)
        {
            CardUI cardUI = hijo.GetComponent<CardUI>();
            if (cardUI != null && cardUI.cardData != null)
            {
                string categoria = cardUI.cardData.category.ToString().Trim();
                if (categoria.Equals("Pokemon", System.StringComparison.OrdinalIgnoreCase))
                {
                    contador++;
                }
            }
        }
        return contador;
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotificarResultadoAlRivalServerRpc()
    {
        NotificarResultadoAlRivalClientRpc();
    }

    [ClientRpc]
    private void NotificarResultadoAlRivalClientRpc()
    {
        if (!IsOwner)
        {
            DefinirResultadoLocal(true); // Victoria para el rival
        }
    }

    public void DefinirResultadoLocal(bool ganeLaPartida)
    {
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
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("MainMenu");
    }
}