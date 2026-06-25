using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class GeneradorMazo : MonoBehaviour
{
    [Header("Configuración del Mazo")]
    public GameObject cartaPrefab; // Arrastra aquí el prefab de tu carta
    public Transform contenedorMazo; // El objeto "Mazo" abajo a la derecha
    public Transform contenedorMano; // El objeto de la Mano con Horizontal Layout Group
    public TextMeshProUGUI textoContador;

    [Header("Configuración")]
    public int totalCartasMazo = 20;

    private List<GameObject> listaDeCartas = new List<GameObject>();

    [Header("Apariencia")]
    public float desfaseEntreCartas = 0.5f; // Para el efecto visual de cartas apiladas

    void Start()
    {
        CrearMazoInicial();
        ActualizarContador();

        // Ejecutamos la regla del juego al iniciar: Robar 5 cartas buscando un básico
        RobarManoInicialConBasico(5);
    }

    void CrearMazoInicial()
    {
        // 1. Verificación de seguridad por si no encuentra el mánager
        if (CardGameManager._instance == null)
        {
            Debug.LogError("No se encontró el CardGameManager en la escena.");
            return;
        }

        // 2. Tomamos la lista real de cartas del mánager
        List<TCGPCard> miMazoReal = CardGameManager._instance.miMazo;

        // Ajustamos por seguridad el bucle si hay menos cartas en la lista que 'totalCartasMazo'
        int cartasACrear = Mathf.Min(totalCartasMazo, miMazoReal.Count);

        for (int i = 0; i < cartasACrear; i++)
        {
            // 3. Instanciamos el Prefab de la carta en el contenedor del Mazo
            GameObject nuevaCarta = Instantiate(cartaPrefab, contenedorMazo);

            // 4. Buscamos tu componente CardUI en este clon específico
            CardTablero componenteUI = nuevaCarta.GetComponent<CardTablero>();

            if (componenteUI != null)
            {
                // 5. ¡AQUÍ TRASPASAMOS LA INFORMACIÓN! 
                componenteUI.SetData(miMazoReal[i]);
                Debug.Log($"Datos inyectados con éxito a la carta clonada: {miMazoReal[i].name}");
            }
            else
            {
                Debug.LogWarning($"El prefab de la carta no tiene el componente 'CardUI' asignado.");
            }

            // Posicionamiento apilado visual en el mazo
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

    // --- SISTEMA DE MULLIGAN AUTOMÁTICO ---
    public void RobarManoInicialConBasico(int cantidad)
    {
        bool manoValida = false;
        int intentos = 0;

        // Lista temporal para examinar el intento de robo sin alterar la mano visual aún
        List<GameObject> cartasPropuestas = new List<GameObject>();

        while (!manoValida)
        {
            intentos++;
            Debug.Log($"--- Evaluando intento de mano inicial número: {intentos} ---");

            // Protección: Evitar bucle infinito si el mazo se queda sin cartas suficientes
            if (listaDeCartas.Count < cantidad)
            {
                Debug.LogError("No hay suficientes cartas en el mazo para completar el robo inicial.");
                break;
            }

            // 1. Simulamos sacar las X cartas superiores del mazo
            for (int i = 0; i < cantidad; i++)
            {
                GameObject carta = listaDeCartas[listaDeCartas.Count - 1];
                listaDeCartas.Remove(carta);
                cartasPropuestas.Add(carta);
            }

            // 2. Buscamos si hay al menos un Pokémon Básico en el robo actual
            bool tieneBasico = false;
            foreach (GameObject cartaGO in cartasPropuestas)
            {
                CardTablero ui = cartaGO.GetComponent<CardTablero>();

                if (ui != null && ui.cardData != null)
                {
                    // Convertimos a texto limpio (por si usas Enums) e ignoramos mayúsculas/minúsculas
                    string categoria = ui.cardData.category.ToString().Trim();
                    string subCategoria = ui.cardData.sub_category.ToString().Trim();

                    if (categoria.Equals("Pokemon", System.StringComparison.OrdinalIgnoreCase) &&
                        subCategoria.Equals("Basic", System.StringComparison.OrdinalIgnoreCase))
                    {
                        tieneBasico = true;
                        Debug.Log($"¡Pokémon básico encontrado!: {ui.cardData.name}");
                        break; // Saltamos el bucle, ya encontramos la condición de éxito
                    }
                }
            }

            // 3. Procesamos el resultado de la mano
            if (tieneBasico)
            {
                // ÉXITO: Las cartas pasan oficialmente a la interfaz de la mano del jugador
                manoValida = true;
                foreach (GameObject cartaAceptada in cartasPropuestas)
                {
                    cartaAceptada.transform.SetParent(contenedorMano);

                    // Reseteos posicionales para que el Horizontal Layout Group actúe perfectamente
                    RectTransform rect = cartaAceptada.GetComponent<RectTransform>();
                    rect.localPosition = Vector3.zero;
                    rect.localRotation = Quaternion.identity;
                    // Mantener la escala definida en el prefab
                    // rect.localScale se deja intacta
                }
                Debug.Log($"Mano inicial aceptada tras {intentos} intentos.");
            }
            else
            {
                // MULLIGAN: No hubo básico. Devolvemos las cartas propuestas al mazo original
                Debug.LogWarning($"Intento {intentos} fallido (No hay Pokémon Básico). Rebotando al mazo.");

                foreach (GameObject cartaRechazada in cartasPropuestas)
                {
                    // Regresan al mazo
                    cartaRechazada.transform.SetParent(contenedorMazo);
                    listaDeCartas.Add(cartaRechazada);
                }
                cartasPropuestas.Clear(); // Vaciamos la lista temporal

                // Barajamos todo el mazo de nuevo antes del próximo ciclo del 'while'
                BarajarMazo();
            }
        }

        ActualizarContador();
    }

    // Mezcla el orden del mazo de forma aleatoria y reordena su posición visual
    void BarajarMazo()
    {
        // Algoritmo de barajado Fisher-Yates
        for (int i = 0; i < listaDeCartas.Count; i++)
        {
            GameObject temporal = listaDeCartas[i];
            int randomIndex = Random.Range(i, listaDeCartas.Count);
            listaDeCartas[i] = listaDeCartas[randomIndex];
            listaDeCartas[randomIndex] = temporal;
        }

        // Volvemos a acomodar las cartas físicamente de forma apilada en el "contenedorMazo"
        for (int i = 0; i < listaDeCartas.Count; i++)
        {
            RectTransform rect = listaDeCartas[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-i * desfaseEntreCartas, i * desfaseEntreCartas);
            // No tocar localScale para conservar la escala del prefab
        }
    }

    // Método para robar cartas normales durante el resto de la partida (sin reglas especiales)
    public void RobarCartas(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            if (listaDeCartas.Count > 0)
            {
                // 1. Tomamos la última carta del mazo visual
                GameObject cartaARobar = listaDeCartas[listaDeCartas.Count - 1];
                listaDeCartas.Remove(cartaARobar);

                // 2. La movemos al contenedor de la mano
                cartaARobar.transform.SetParent(contenedorMano);

                // 3. Reseteamos la transformación para que el Horizontal Layout Group de la mano la acomode al lado de las otras
                RectTransform rect = cartaARobar.GetComponent<RectTransform>();
                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                // No tocar localScale para conservar la escala del prefab
            }
            else
            {
                Debug.LogWarning("¡Te has quedado sin cartas en el mazo!");
                break;
            }
        }
        ActualizarContador();
    }

    void Update()
    {
        // Presiona la tecla O para robar cartas de forma común durante el juego
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            RobarCartas(1);
        }
    }
}