using UnityEngine;
using Unity.Netcode;

public class NetworkShutdownFix : MonoBehaviour
{
    private void OnApplicationQuit()
    {
        // Forzamos el apagado controlado antes de que la escena se desmantele
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log("Cerrando NetworkManager de forma segura...");
            NetworkManager.Singleton.Shutdown();
        }
    }
}