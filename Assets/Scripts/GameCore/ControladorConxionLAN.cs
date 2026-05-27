using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorConexionLAN : MonoBehaviour
{
    [Header("UI Botones")]
    [SerializeField] private Button btnHost;
    [SerializeField] private Button btnCliente;

    [Header("Contenedor del Menú a Ocultar")]
    [SerializeField] private GameObject contenedorMenuBotones;

    [Header("Input de IP (Opcional)")]
    [SerializeField] private TMP_InputField inputIP;

    private UnityTransport transporte;

    void Start()
    {
        // 1. Verificamos primero que el NetworkManager exista
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("¡No hay ningún NetworkManager en la escena! Asegúrate de tener uno.");
            return;
        }

        // 2. Intentamos obtener el transporte directamente desde el NetworkManager de forma segura
        transporte = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        // 3. Si aun así es null, lo buscamos como componente por si acaso
        if (transporte == null)
        {
            transporte = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }

        // 4. Si después de todo sigue siendo null, tiramos un aviso claro antes de que el juego rompa
        if (transporte == null)
        {
            Debug.LogError("¡Error Crítico! No se encontró el componente UnityTransport en el NetworkManager.");
        }

        // Asignar los botones
        btnHost.onClick.AddListener(IniciarHost);
        btnCliente.onClick.AddListener(IniciarCliente);

        NetworkManager.Singleton.OnClientConnectedCallback += AlConectarseUnCliente;
        NetworkManager.Singleton.OnClientDisconnectCallback += AlDesconectarseUnCliente;
    }

    private void OnDestroy()
    {
        // Buena práctica: Desuscribirse al destruir el objeto para evitar errores de memoria
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarseUnCliente;
            NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarseUnCliente;
        }
    }

    void IniciarHost()
    {
        if (transporte == null)
        {
            Debug.LogError("No se puede iniciar el Host porque 'transporte' no está asignado.");
            return;
        }

        transporte.ConnectionData.Address = "0.0.0.0";
        NetworkManager.Singleton.StartHost();
        DesactivarMenuUI();
        Debug.Log("Partida LAN iniciada como HOST.");
    }

    void IniciarCliente()
    {
        if (inputIP != null && !string.IsNullOrEmpty(inputIP.text))
        {
            transporte.ConnectionData.Address = inputIP.text.Trim(); // .Trim() elimina espacios fantasma
        }
        else
        {
            transporte.ConnectionData.Address = "127.0.0.1";
        }

        NetworkManager.Singleton.StartClient();
        DesactivarMenuUI();
        Debug.Log($"Intentando conectar al Host en: {transporte.ConnectionData.Address}");
    }

    void DesactivarMenuUI()
    {
        if (contenedorMenuBotones != null)
        {
            contenedorMenuBotones.SetActive(false);
        }
    }

    private void AlConectarseUnCliente(ulong clientId)
    {
        // Esto se ejecuta en el Host cuando entra un cliente, 
        // y en el Cliente cuando él mismo logra conectar con éxito.

        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("¡Tú eres el Host y has iniciado correctamente!");
            return;
        }

        Debug.Log($"¡Un jugador se ha unido a la partida! ID de Cliente: {clientId}");

        // Al ser un juego 1 vs 1, si eres el Host y se conecta el cliente,
        // ¡aquí es el momento exacto para mandar a cargar la escena de juego o repartir cartas!
    }

    private void AlDesconectarseUnCliente(ulong clientId)
    {
        Debug.Log($"Un jugador se ha salido o ha perdido la conexión. ID: {clientId}");

        // Si el cliente se desconecta, aquí podrías regresar al menú principal
        // o mostrar un cartel de "Victoria por abandono".
    }
}
