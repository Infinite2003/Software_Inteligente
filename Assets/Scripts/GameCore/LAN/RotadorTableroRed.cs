using Unity.Netcode;
using UnityEngine;

public class RotadorTableroRed : NetworkBehaviour
{
    [Header("Contenedores a Rotar")]
    [SerializeField] private RectTransform canvasPrincipal;

    // Este método de Netcode se ejecuta automáticamente cuando el objeto se activa en la red
    public override void OnNetworkSpawn()
    {
        // Si soy el Host (Jugador 1), no hacemos nada. Él ve el tablero normal.
        if (IsHost)
        {
            Debug.Log("Jugador 1 (Host) detectado. Tablero en posición original.");
            return;
        }

        // Si soy el Cliente (Jugador 2), rotamos la pantalla para invertir la perspectiva
        if (IsClient && !IsHost)
        {
            Debug.Log("Jugador 2 (Cliente) detectado. Rotando el tablero 180 grados.");
            RotarTablero();
        }
    }

    private void RotarTablero()
    {
        if (canvasPrincipal != null)
        {
            // Rotamos el Canvas completo 180 grados en el eje Z
            canvasPrincipal.localRotation = Quaternion.Euler(0, 0, 180);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el Canvas Principal en el script RotadorTableroRed.");
        }
    }
}