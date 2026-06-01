using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;

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
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("¡No se encontró el NetworkManager en la escena!");
            return;
        }

        transporte = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Todas las suscripciones juntas y dentro del bloque seguro
        NetworkManager.Singleton.OnClientConnectedCallback += AlConectarseUnCliente;
        NetworkManager.Singleton.OnClientDisconnectCallback += AlDesconectarseUnCliente;

        btnHost.onClick.AddListener(IniciarHost);
        btnCliente.onClick.AddListener(IniciarCliente);
    }

    void IniciarHost()
    {
        if (NetworkManager.Singleton.IsListening) return;

        transporte.ConnectionData.Address = "0.0.0.0";
        NetworkManager.Singleton.StartHost();
        DesactivarMenuUI();
        Debug.Log("Partida LAN iniciada como HOST. Esperando al otro jugador...");
    }

    void IniciarCliente()
    {
        if (NetworkManager.Singleton.IsListening) return;

        string ip = inputIP != null ? inputIP.text.Trim() : "";

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

    bool EsIPValida(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }

    void DesactivarMenuUI()
    {
        if (contenedorMenuBotones != null)
            contenedorMenuBotones.SetActive(false);
    }

    private void AlConectarseUnCliente(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            return;

        Debug.Log($"¡Se ha conectado un jugador a la partida! ID del Cliente: {clientId}");
    }

    private void AlDesconectarseUnCliente(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            return;

        Debug.Log($"Un jugador se desconectó. ID: {clientId}");

        // Solo reactivamos el menú si somos el cliente que perdió conexión
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Conexión perdida con el Host. Volviendo al menú.");
            if (contenedorMenuBotones != null)
                contenedorMenuBotones.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarseUnCliente;
            NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarseUnCliente;
        }
    }
}