using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Necesario para usar Listas
using TMPro;
using UnityEngine.InputSystem;

public class GeneradorMazo : MonoBehaviour
{
    [Header("Configuración del Mazo")]
    public GameObject cartaPrefab; // Arrastra aquí el prefab de tu carta
    public Transform contenedorMazo; // El objeto "Mazo" que posicionaste abajo a la derecha
    public Transform contenedorMano;
    public TextMeshProUGUI textoContador;

    [Header("Configuración")]
    public int totalCartasMazo = 20;

    private List<GameObject> listaDeCartas = new List<GameObject>();
    //CardGameManager._instance.miMazo;

    [Header("Apariencia")]
    public float desfaseEntreCartas = 0.5f; // Para que se vean un poco amontonadas

    void Start()
    {
        CrearMazoInicial();
        ActualizarContador();
    }

    void CrearMazoInicial()
    {
        var miMazoReal = CardGameManager._instance.miMazo;

        // Usamos el conteo real de la lista miMazo
        for (int i = 0; i < totalCartasMazo; i++)
        {
            GameObject nuevaCarta = Instantiate(cartaPrefab, contenedorMazo);

            // 1. Buscamos el componente visual
            CartaVisual visual = nuevaCarta.GetComponent<CartaVisual>();

            if (visual != null)
            {
                // 2. Le pasamos el objeto de datos actual de la lista
                visual.Configurar(miMazoReal[i]);
            }

            // Posicionamiento visual en el mazo (amontonadas)
            RectTransform rect = nuevaCarta.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-i * desfaseEntreCartas, i * desfaseEntreCartas);

            listaDeCartas.Add(nuevaCarta);
        }
    }

    // Método para actualizar el número visual
    void ActualizarContador()
    {
        if (textoContador != null)
            textoContador.text = listaDeCartas.Count.ToString();
    }

    // Esta función la puedes llamar con un Botón o una tecla
    public void RobarCartas(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            if (listaDeCartas.Count > 0)
            {
                // 1. Tomamos la última carta
                GameObject cartaARobar = listaDeCartas[listaDeCartas.Count - 1];
                listaDeCartas.Remove(cartaARobar);

                // 2. La movemos al contenedor de la mano
                cartaARobar.transform.SetParent(contenedorMano);

                // 3. ¡ESTO ES LO IMPORTANTE! 
                // Reseteamos todo para que el Horizontal Layout Group tome el control total
                RectTransform rect = cartaARobar.GetComponent<RectTransform>();
                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one; // Evita que la carta se estire o encoja
            }
        }
        ActualizarContador();
    }

    void Update()
    {
        // Ejemplo: Presiona Espacio para sacar 5 cartas
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            RobarCartas(5);
        }
    }
}