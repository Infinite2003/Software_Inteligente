using UnityEngine;
using Unity.Netcode;

public class NetworkShutdownFix : MonoBehaviour
{
    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log("Cerrando NetworkManager de forma segura...");

            // [8] Desuscribimos eventos activos antes del Shutdown
            // para evitar que se disparen callbacks sobre objetos ya destruidos
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnConexionFallidaPlaceholder;

            NetworkManager.Singleton.Shutdown();
        }
    }

    // [8] Placeholder vacío por si no hay referencia directa al método original.
    // En un proyecto real, aquí irían todos los callbacks que hayas suscrito.
    private void OnConexionFallidaPlaceholder(ulong clientId) { }
}