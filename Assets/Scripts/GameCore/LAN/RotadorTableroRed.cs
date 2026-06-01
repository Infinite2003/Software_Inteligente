using Unity.Netcode;
using UnityEngine;

public class RotadorTableroRed : NetworkBehaviour
{
    [Header("Contenedores a Rotar")]
    [SerializeField] private RectTransform canvasPrincipal;

    // [7] Flag para evitar rotar dos veces si el cliente reconecta
    private bool yaRotado = false;

    public override void OnNetworkSpawn()
    {
        // [5] Validamos que el canvas esté asignado antes de cualquier lógica
        if (canvasPrincipal == null)
        {
            Debug.LogError("Canvas Principal no asignado en RotadorTableroRed.");
            return;
        }

        if (IsHost)
        {
            Debug.Log("Jugador 1 (Host) detectado. Tablero en posición original.");
            return;
        }

        // [6] Simplificado: cualquier no-servidor es el cliente que debe rotar
        if (!IsServer)
        {
            Debug.Log("Jugador 2 (Cliente) detectado. Rotando el tablero 180 grados.");
            RotarTablero();
        }
    }

    private void RotarTablero()
    {
        // [7] Si ya fue rotado antes (ej: reconexión), no volvemos a rotar
        if (yaRotado) return;

        canvasPrincipal.localRotation = Quaternion.Euler(0, 0, 180);
        yaRotado = true;
    }
}