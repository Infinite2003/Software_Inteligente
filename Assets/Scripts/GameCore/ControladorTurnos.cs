using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorTurnos : NetworkBehaviour
{
    // Cambiamos el entero simple por una variable de red para que se sincronice en ambos jugadores
    private NetworkVariable<int> numeroDeTurnoGlobal = new NetworkVariable<int>(
        1, // Comienza en la ronda 1
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Sincroniza el ID del jugador que tiene el turno actual (ulong)
    private NetworkVariable<ulong> jugadorActualTurno = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("UI Componentes")]
    [SerializeField] private Button btnPasarTurno;
    [SerializeField] private TextMeshProUGUI textoIndicadorTurno;

    void Start()
    {
        // El botón llamará a una función que le pide al servidor cambiar el turno
        if (btnPasarTurno != null)
        {
            btnPasarTurno.onClick.AddListener(SolicitarCambiarTurno);
        }
    }

    public override void OnNetworkSpawn()
    {
        // Nos suscribimos al evento para que avise a la UI cada vez que el turno cambie
        jugadorActualTurno.OnValueChanged += AlCambiarElTurno;

        // Configuración inicial del primer turno
        ActualizarInterfazTurno();
    }

    public override void OnNetworkDespawn()
    {
        jugadorActualTurno.OnValueChanged -= AlCambiarElTurno;
    }

    // Devuelve 'true' si es el turno de la persona que está mirando esta pantalla
    public bool EsMiTurno()
    {
        return NetworkManager.Singleton.LocalClientId == jugadorActualTurno.Value;
    }

    // NUEVA FUNCIÓN: Permite que el script "CartaInteractiva" consulte si estamos en el primer turno de la partida
    public bool EsPrimerTurno()
    {
        // Si el valor global es 1, significa que nadie ha completado su primer turno
        return numeroDeTurnoGlobal.Value == 1;
    }

    private void SolicitarCambiarTurno()
    {
        // Seguridad: Solo puedes pasar el turno si actualmente es tu turno
        if (!EsMiTurno()) return;

        // Le pedimos al servidor que pase el turno
        CambiarTurnoServerRpc();
    }

    // Un ServerRpc es una función que el cliente presiona, pero SE EJECUTA EN EL SERVIDOR (Host)
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CambiarTurnoServerRpc()
    {
        // Obtenemos una lista de todos los jugadores conectados actualmente
        var clientesConectados = NetworkManager.Singleton.ConnectedClientsIds;

        if (clientesConectados.Count < 2)
        {
            Debug.LogWarning("Esperando a que se conecte el segundo jugador para alternar turnos.");
            return;
        }

       
        // Aumentamos el contador global de turnos. Solo el servidor puede editar este valor.
        numeroDeTurnoGlobal.Value += 1;
        Debug.Log($"[Servidor] Avanzando al turno global número: {numeroDeTurnoGlobal.Value}");


        // Alternamos el ID entre el Jugador 1 y el Jugador 2
        if (jugadorActualTurno.Value == clientesConectados[0])
        {
            jugadorActualTurno.Value = clientesConectados[1];
        }
        else
        {
            jugadorActualTurno.Value = clientesConectados[0];
        }
    }

    private void AlCambiarElTurno(ulong turnoAnterior, ulong turnoNuevo)
    {
        if (EsMiTurno() && !EsPrimerTurno())
        {
            // Buscamos el GeneradorMazo en la escena actual
            GeneradorMazo generadorMazo = Object.FindFirstObjectByType<GeneradorMazo>();

            if (generadorMazo != null)
            {
                Debug.Log("¡Inicio de turno! Robando 1 carta automáticamente.");
                generadorMazo.RobarCartas(1);
            }
            else
            {
                Debug.LogWarning("No se encontró el GeneradorMazo en la escena para efectuar el robo automático.");
            }
        }
        ActualizarInterfazTurno();
    }

    private void ActualizarInterfazTurno()
    {
        if (EsMiTurno())
        {
            // Modificamos el texto si es su primer turno para darle feedback visual al jugador
            if (textoIndicadorTurno != null)
            {
                if (EsPrimerTurno())
                {
                    textoIndicadorTurno.text = "TU TURNO (Fase Inicial: Solo Básicos)";
                }
                else
                {
                    textoIndicadorTurno.text = "TU TURNO";
                }
            }

            if (btnPasarTurno != null) btnPasarTurno.interactable = true; // Activa el botón
            Debug.Log("Es tu turno. Puedes jugar.");
        }
        else
        {
            if (textoIndicadorTurno != null) textoIndicadorTurno.text = "TURNO DEL RIVAL";
            if (btnPasarTurno != null) btnPasarTurno.interactable = false; // Desactiva el botón (se vuelve gris)
            Debug.Log("Es turno del rival. Bloqueado.");
        }
    }
}