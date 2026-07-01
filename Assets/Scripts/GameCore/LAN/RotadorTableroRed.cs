using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class RotadorTableroRed : MonoBehaviour
{
    [Header("Elementos a intercambiar para el J2")]
    [SerializeField] private RectTransform zonaActivaJ1;
    [SerializeField] private RectTransform zonaActivaJ2;
    [SerializeField] private RectTransform manoJugador;
    [SerializeField] private RectTransform bancaJugador;

    private bool yaIntercambiado = false;

    void Start()
    {
        StartCoroutine(EsperarRedYReordenar());
    }

    private IEnumerator EsperarRedYReordenar()
    {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening);

        if (yaIntercambiado) yield break;
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[RotadorTableroRed] Host: sin cambios.");
            yield break;
        }

        // Es cliente: intercambiar posiciones de las zonas activas
        if (zonaActivaJ1 != null && zonaActivaJ2 != null)
        {
            Vector2 posJ1 = zonaActivaJ1.anchoredPosition;
            Vector2 posJ2 = zonaActivaJ2.anchoredPosition;

            zonaActivaJ1.anchoredPosition = posJ2;
            zonaActivaJ2.anchoredPosition = posJ1;

            Debug.Log($"[RotadorTableroRed] Zonas activas intercambiadas: " +
                      $"J1→{zonaActivaJ1.anchoredPosition} | J2→{zonaActivaJ2.anchoredPosition}");
        }

        // También intercambiar Mano y Banca si están asignadas
        if (manoJugador != null && bancaJugador != null)
        {
            Vector2 posMano = manoJugador.anchoredPosition;
            Vector2 posBanca = bancaJugador.anchoredPosition;

            manoJugador.anchoredPosition = posBanca;
            bancaJugador.anchoredPosition = posMano;

            Debug.Log($"[RotadorTableroRed] Mano y Banca intercambiadas.");
        }

        yaIntercambiado = true;
        Debug.Log("[RotadorTableroRed] Reordenamiento completado para el cliente.");
    }
}