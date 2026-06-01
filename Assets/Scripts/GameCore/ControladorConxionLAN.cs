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
        // Validación de seguridad para obtener el transporte de red correctamente
        if (NetworkManager.Singleton != null)
        {
            transporte = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // SUSCRIPCIÓN A EVENTOS: Aquí detectamos las conexiones y desconexiones
            NetworkManager.Singleton.OnClientConnectedCallback += AlConectarseUnCliente;
            NetworkManager.Singleton.OnClientDisconnectCallback += AlDesconectarseUnCliente;
        }
        else
        {
            Debug.LogError("¡No se encontró el NetworkManager en la escena!");
        }

        btnHost.onClick.AddListener(IniciarHost);
        btnCliente.onClick.AddListener(IniciarCliente);
    }

    private void OnDestroy()
    {
        // BUENA PRÁCTICA: Nos desuscribimos de los eventos al destruir el objeto para evitar fugas de memoria
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarseUnCliente;
            NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarseUnCliente;
        }
    }

    void IniciarHost()
    {
        if (transporte == null) return;

        transporte.ConnectionData.Address = "0.0.0.0";
        NetworkManager.Singleton.StartHost();
        DesactivarMenuUI();
        Debug.Log("Partida LAN iniciada como HOST. Esperando al otro jugador...");
    }

    void IniciarCliente()
    {
        if (transporte == null) return;

        if (inputIP != null && !string.IsNullOrEmpty(inputIP.text))
        {
            transporte.ConnectionData.Address = inputIP.text.Trim(); // .Trim() quita espacios vacíos accidentales
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
        // Si el ID conectado coincide con nuestro propio ID local y somos el Host, ignoramos el mensaje
        // para que no diga "alguien se conectó" cuando tú mismo inicias la partida.
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        // Este mensaje saldrá en la pantalla del Host cuando el rival entre.
        // También saldrá en la pantalla del Cliente cuando logre conectar con éxito.
        Debug.Log($"¡Se ha conectado un jugador a la partida! ID del Cliente: {clientId}");
    }

    private void AlDesconectarseUnCliente(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            return; // Ignorar si el Host cierra la partida voluntariamente
        }

        Debug.Log($"Un jugador se ha desconectado o perdió la conexión. ID: {clientId}");
    }
}
