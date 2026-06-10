using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode;

public class GestorVictoriaDerrota : NetworkBehaviour
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
    private bool juegoTerminado = false;

    private NetworkVariable<int> turnosTotalesTranscurridos = new NetworkVariable<int>(0);
    private bool _ultimoEstadoTurno;

    void Start()
    {
        if (objetoVictoria != null) objetoVictoria.SetActive(false);
        if (objetoDerrota != null) objetoDerrota.SetActive(false);
        if (botonRegresar != null) botonRegresar.SetActive(false);

        sistemaTurnos = Object.FindFirstObjectByType<ControladorTurnos>();

        if (sistemaTurnos != null)
            _ultimoEstadoTurno = sistemaTurnos.EsMiTurno();

        StartCoroutine(BucleMonitoreoPartida());
    }

    void Update()
    {
        if (!IsSpawned || sistemaTurnos == null || juegoTerminado) return;
        if(!IsServer) return;

        bool turnoActual = sistemaTurnos.EsMiTurno();
        if (turnoActual != _ultimoEstadoTurno)
        {
            _ultimoEstadoTurno = turnoActual;

            turnosTotalesTranscurridos.Value++;
        }
    }

    //[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    //private void ActualizarTurnoServerRpc()
    //{
    //    turnosTotalesTranscurridos.Value++;
    //}

    private IEnumerator BucleMonitoreoPartida()
    {
        yield return new WaitUntil(() => turnosTotalesTranscurridos.Value >= 2);

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

        Debug.Log($"[Verificación] ClientId={NetworkManager.Singleton.LocalClientId} | " +
             $"Activo={pokemonEnActivo} | Banca={pokemonEnBanca} | " +
             $"zonaCartaJugada={(zonaCartaJugada != null ? zonaCartaJugada.name : "NULL")} | " +
             $"zonaBanca={(zonaBanca != null ? zonaBanca.name : "NULL")} | " +
             $"turnosTotales={turnosTotalesTranscurridos.Value}");

        if (pokemonEnActivo == 0 && pokemonEnBanca == 0)
        {
            DefinirResultadoLocal(false);
            NotificarResultadoAlRivalServerRpc();
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NotificarResultadoAlRivalServerRpc()
    {
        NotificarResultadoAlRivalClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotificarResultadoAlRivalClientRpc()
    {
        if (NetworkManager.Singleton.LocalClientId != GetComponent<NetworkObject>().OwnerClientId)
            DefinirResultadoLocal(true);
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
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene("MainMenu");
    }
}