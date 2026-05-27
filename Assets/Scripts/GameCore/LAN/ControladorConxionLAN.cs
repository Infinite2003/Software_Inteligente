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

    [Header("Contenedor del Menú a Ocultar")]
    [SerializeField] private GameObject contenedorMenuBotones;

    [Header("Input de IP (Opcional)")]
    [SerializeField] private TMP_InputField inputIP;

    private UnityTransport transporte;

    void Start()
    {
        // [2] Validación de null antes de acceder al Singleton
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager no encontrado en escena.");
            return;
        }

        transporte = NetworkManager.Singleton.GetComponent<UnityTransport>();

        btnHost.onClick.AddListener(IniciarHost);
        btnCliente.onClick.AddListener(IniciarCliente);

        // [4] Suscribimos el callback de fallo de conexión
        NetworkManager.Singleton.OnClientDisconnectCallback += OnConexionFallida;
    }

    void IniciarHost()
    {
        // [1] Evita doble clic o llamadas duplicadas
        if (NetworkManager.Singleton.IsListening) return;

        transporte.ConnectionData.Address = "0.0.0.0";
        NetworkManager.Singleton.StartHost();
        DesactivarMenuUI();
        Debug.Log("Partida LAN iniciada como HOST.");
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
                Debug.LogWarning($"IP inválida introducida: '{ip}'. Abortando conexión.");
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

    // [4] Se dispara si la conexión falla o el host la rechaza
    void OnConexionFallida(ulong clientId)
    {
        Debug.LogWarning("Conexión fallida o rechazada. Volviendo al menú.");

        // Reactivamos el menú para que el jugador pueda intentarlo de nuevo
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