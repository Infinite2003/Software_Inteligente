using UnityEngine;
using Unity.Netcode;

public class SincronizadorRed : NetworkBehaviour
{
    public static SincronizadorRed Instancia { get; private set; }

    private NetworkVariable<int> numeroDeTurnoGlobal = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<ulong> jugadorActualTurno = new NetworkVariable<ulong>(
        ulong.MaxValue, // Inválido hasta que IniciarPrimerTurno() lo asigne
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> turnosTotalesTranscurridos = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event System.Action<ulong, ulong> OnTurnoCambiado;
    public event System.Action OnRivalPerdio;

    public int NumeroDeTurno => numeroDeTurnoGlobal.Value;
    public ulong JugadorActualTurno => jugadorActualTurno.Value;
    public int TurnosTotales => turnosTotalesTranscurridos.Value;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    public override void OnNetworkSpawn()
    {
        jugadorActualTurno.OnValueChanged += (anterior, nuevo) =>
        {
            OnTurnoCambiado?.Invoke(anterior, nuevo);
        };
    }

    public override void OnNetworkDespawn()
    {
        jugadorActualTurno.OnValueChanged -= (anterior, nuevo) =>
        {
            OnTurnoCambiado?.Invoke(anterior, nuevo);
        };
    }

    public void IniciarPrimerTurno()
    {
        if (!IsServer) return;

        var clientes = NetworkManager.Singleton.ConnectedClientsIds;
        if (clientes.Count < 2)
        {
            Debug.LogWarning("[SincronizadorRed] IniciarPrimerTurno llamado con menos de 2 jugadores.");
            return;
        }

        jugadorActualTurno.Value = clientes[0];
        Debug.Log($"[Servidor] Partida iniciada. Primer turno: jugador {clientes[0]}");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CambiarTurnoServerRpc()
    {
        var clientes = NetworkManager.Singleton.ConnectedClientsIds;
        if (clientes.Count < 2)
        {
            Debug.LogWarning("[SincronizadorRed] No hay 2 jugadores para cambiar turno.");
            return;
        }

        numeroDeTurnoGlobal.Value += 1;
        Debug.Log($"[Servidor] Turno global: {numeroDeTurnoGlobal.Value}");

        jugadorActualTurno.Value = jugadorActualTurno.Value == clientes[0]
            ? clientes[1]
            : clientes[0];
    }

    public void IncrementarTurnosTranscurridos()
    {
        if (!IsServer) return;
        turnosTotalesTranscurridos.Value++;
        Debug.Log($"[Servidor] Turnos transcurridos: {turnosTotalesTranscurridos.Value}");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NotificarDerrotaServerRpc()
    {
        NotificarVictoriaRivalClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotificarVictoriaRivalClientRpc()
    {
        // Solo el que NO envió la notificación recibe la victoria
        if (NetworkManager.Singleton.LocalClientId != jugadorActualTurno.Value)
            OnRivalPerdio?.Invoke();
    }
}
