using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net; // [3] Necesario para IPAddress.TryParse

public class ControladorConexionLAN : MonoBehaviour
{
    [Header("UI Botones")]
    [SerializeField] private Button btnHost;
    [SerializeField] private Button btnCliente;

    [Header("Contenedor del Men� a Ocultar")]
    [SerializeField] private GameObject contenedorMenuBotones;

    [Header("Input de IP (Opcional)")]
    [SerializeField] private TMP_InputField inputIP;

    private UnityTransport transporte;

    void Start()
    {
        // Validaci�n de seguridad para obtener el transporte de red correctamente
        if (NetworkManager.Singleton != null)
        {
            transporte = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // SUSCRIPCI�N A EVENTOS: Aqu� detectamos las conexiones y desconexiones
            NetworkManager.Singleton.OnClientConnectedCallback += AlConectarseUnCliente;
            NetworkManager.Singleton.OnClientDisconnectCallback += AlDesconectarseUnCliente;
        }
        else
        {
            Debug.LogError("�No se encontr� el NetworkManager en la escena!");
        }

        btnHost.onClick.AddListener(IniciarHost);
        btnCliente.onClick.AddListener(IniciarCliente);

        // [4] Suscribimos el callback de fallo de conexi�n
        NetworkManager.Singleton.OnClientDisconnectCallback += OnConexionFallida;
    }

    private void OnDestroy()
    {
        // BUENA PR�CTICA: Nos desuscribimos de los eventos al destruir el objeto para evitar fugas de memoria
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarseUnCliente;
            NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarseUnCliente;
        }
    }

    void IniciarHost()
    {
        // [1] Evita doble clic o llamadas duplicadas
        if (NetworkManager.Singleton.IsListening) return;

        transporte.ConnectionData.Address = "0.0.0.0";
        NetworkManager.Singleton.StartHost();
        DesactivarMenuUI();
        Debug.Log("Partida LAN iniciada como HOST. Esperando al otro jugador...");
    }

    void IniciarCliente()
    {
        // [1] Evita doble clic o llamadas duplicadas
        if (NetworkManager.Singleton.IsListening) return;

        string ip = inputIP != null ? inputIP.text.Trim() : "";

        // [3] Validamos que la IP tenga formato correcto antes de conectar
        if (!string.IsNullOrEmpty(ip))
        {
            if (!EsIPValida(ip))
            {
                Debug.LogWarning($"IP inv�lida introducida: '{ip}'. Abortando conexi�n.");
                return;
            }
            transporte.ConnectionData.Address = ip;
        }
        else
        {
            transporte.ConnectionData.Address = "127.0.0.1";
        }

        NetworkManager.Singleton.StartClient();
        DesactivarMenuUI();
        Debug.Log($"Intentando conectar al Host en: {transporte.ConnectionData.Address}");
    }

    // [3] Valida que el string sea una IP con formato correcto
    bool EsIPValida(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }

    // [4] Se dispara si la conexi�n falla o el host la rechaza
    void OnConexionFallida(ulong clientId)
    {
        Debug.LogWarning("Conexi�n fallida o rechazada. Volviendo al men�.");

        // Reactivamos el men� para que el jugador pueda intentarlo de nuevo
        if (contenedorMenuBotones != null)
            contenedorMenuBotones.SetActive(true);
    }

    void DesactivarMenuUI()
    {
        if (contenedorMenuBotones != null)
            contenedorMenuBotones.SetActive(false);
    }

    // [4] Desuscribimos el callback al destruir el objeto para evitar memory leaks
    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnConexionFallida;
    }
}
    private void AlConectarseUnCliente(ulong clientId)
    {
        // Si el ID conectado coincide con nuestro propio ID local y somos el Host, ignoramos el mensaje
        // para que no diga "alguien se conect�" cuando t� mismo inicias la partida.
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        // Este mensaje saldr� en la pantalla del Host cuando el rival entre.
        // Tambi�n saldr� en la pantalla del Cliente cuando logre conectar con �xito.
        Debug.Log($"�Se ha conectado un jugador a la partida! ID del Cliente: {clientId}");
    }

    private void AlDesconectarseUnCliente(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            return; // Ignorar si el Host cierra la partida voluntariamente
        }

        Debug.Log($"Un jugador se ha desconectado o perdi� la conexi�n. ID: {clientId}");
    }
}
