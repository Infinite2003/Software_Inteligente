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
    // Agregamos esta variable para arrastrar el grupo de botones
    [SerializeField] private GameObject contenedorMenuBotones;

    [Header("Input de IP (Opcional)")]
    [SerializeField] private TMP_InputField inputIP;

    private UnityTransport transporte;

    void Start()
    {
        transporte = NetworkManager.Singleton.GetComponent<UnityTransport>();

        btnHost.onClick.AddListener(IniciarHost);
        btnCliente.onClick.AddListener(IniciarCliente);
    }

    void IniciarHost()
    {
        transporte.ConnectionData.Address = "0.0.0.0";
        NetworkManager.Singleton.StartHost();
        DesactivarMenuUI();
        Debug.Log("Partida LAN iniciada como HOST.");
    }

    void IniciarCliente()
    {
        if (inputIP != null && !string.IsNullOrEmpty(inputIP.text))
        {
            transporte.ConnectionData.Address = inputIP.text;
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
        // CORRECCIÓN: Ahora solo apagamos el contenedor de los botones, NO todo el Canvas
        if (contenedorMenuBotones != null)
        {
            contenedorMenuBotones.SetActive(false);
        }
    }
}
