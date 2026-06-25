using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class GeneradorMazo : MonoBehaviour
{
    [Header("Configuración del Mazo")]
    public GameObject cartaPrefab;
    public Transform contenedorMazo;
    public Transform contenedorMano;
    public TextMeshProUGUI textoContador;

    [Header("Configuración")]
    public int totalCartasMazo = 20;

    private List<GameObject> listaDeCartas = new List<GameObject>();

    [Header("Apariencia")]
    public float desfaseEntreCartas = 0.5f;

    void Start()
    {
        CrearMazoInicial();
        ActualizarContador();
        RobarManoInicialConBasico(5);
    }

    void CrearMazoInicial()
    {
        if (CardGameManager._instance == null)
        {
            Debug.LogError("No se encontró el CardGameManager en la escena.");
            return;
        }

        List<TCGPCard> miMazoReal = CardGameManager._instance.currentDeck;

        if (miMazoReal == null || miMazoReal.Count == 0)
        {
            Debug.LogError("El mazo actual está vacío. Asegúrate de seleccionar un mazo antes de entrar a la partida.");
            return;
        }

        int cartasACrear = Mathf.Min(totalCartasMazo, miMazoReal.Count);

        for (int i = 0; i < cartasACrear; i++)
        {
            // FIX: Instantiate sin padre primero para respetar escala del prefab
            GameObject nuevaCarta = Instantiate(cartaPrefab);
            nuevaCarta.transform.SetParent(contenedorMazo, false); // ← false mantiene escala local

            CardTablero componenteUI = nuevaCarta.GetComponent<CardTablero>();

            if (componenteUI != null)
            {
                componenteUI.SetData(miMazoReal[i]);
                Debug.Log($"Datos inyectados con éxito a la carta clonada: {miMazoReal[i].name}");
            }
            else
            {
                Debug.LogWarning($"El prefab de la carta no tiene el componente 'CardTablero' asignado.");
            }

            RectTransform rect = nuevaCarta.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-i * desfaseEntreCartas, i * desfaseEntreCartas);

            listaDeCartas.Add(nuevaCarta);
        }
    }

    void ActualizarContador()
    {
        if (textoContador != null)
            textoContador.text = listaDeCartas.Count.ToString();
    }

    public void RobarManoInicialConBasico(int cantidad)
    {
        bool manoValida = false;
        int intentos = 0;

        List<GameObject> cartasPropuestas = new List<GameObject>();

        while (!manoValida)
        {
            intentos++;
            Debug.Log($"--- Evaluando intento de mano inicial número: {intentos} ---");

            if (listaDeCartas.Count < cantidad)
            {
                Debug.LogError("No hay suficientes cartas en el mazo para completar el robo inicial.");
                break;
            }

            for (int i = 0; i < cantidad; i++)
            {
                GameObject carta = listaDeCartas[listaDeCartas.Count - 1];
                listaDeCartas.Remove(carta);
                cartasPropuestas.Add(carta);
            }

            bool tieneBasico = false;
            foreach (GameObject cartaGO in cartasPropuestas)
            {
                CardTablero ui = cartaGO.GetComponent<CardTablero>();

                if (ui != null && ui.cardData != null)
                {
                    string categoria = ui.cardData.category.ToString().Trim();
                    string subCategoria = ui.cardData.sub_category.ToString().Trim();

                    if (categoria.Equals("Pokemon", System.StringComparison.OrdinalIgnoreCase) &&
                        subCategoria.Equals("Basic", System.StringComparison.OrdinalIgnoreCase))
                    {
                        tieneBasico = true;
                        Debug.Log($"¡Pokémon básico encontrado!: {ui.cardData.name}");
                        break;
                    }
                }
            }

            if (tieneBasico)
            {
                manoValida = true;
                foreach (GameObject cartaAceptada in cartasPropuestas)
                {
                    // FIX: false mantiene la escala local del prefab
                    cartaAceptada.transform.SetParent(contenedorMano, false);

                    RectTransform rect = cartaAceptada.GetComponent<RectTransform>();
                    rect.localPosition = Vector3.zero;
                    rect.localRotation = Quaternion.identity;
                }
                Debug.Log($"Mano inicial aceptada tras {intentos} intentos.");
            }
            else
            {
                Debug.LogWarning($"Intento {intentos} fallido (No hay Pokémon Básico). Rebotando al mazo.");

                foreach (GameObject cartaRechazada in cartasPropuestas)
                {
                    // FIX: false mantiene la escala local del prefab
                    cartaRechazada.transform.SetParent(contenedorMazo, false);
                    listaDeCartas.Add(cartaRechazada);
                }
                cartasPropuestas.Clear();

                BarajarMazo();
            }
        }

        ActualizarContador();
    }

    void BarajarMazo()
    {
        for (int i = 0; i < listaDeCartas.Count; i++)
        {
            GameObject temporal = listaDeCartas[i];
            int randomIndex = Random.Range(i, listaDeCartas.Count);
            listaDeCartas[i] = listaDeCartas[randomIndex];
            listaDeCartas[randomIndex] = temporal;
        }

        for (int i = 0; i < listaDeCartas.Count; i++)
        {
            RectTransform rect = listaDeCartas[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-i * desfaseEntreCartas, i * desfaseEntreCartas);
        }
    }

    public void RobarCartas(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            if (listaDeCartas.Count > 0)
            {
                GameObject cartaARobar = listaDeCartas[listaDeCartas.Count - 1];
                listaDeCartas.Remove(cartaARobar);

                // FIX: false mantiene la escala local del prefab
                cartaARobar.transform.SetParent(contenedorMano, false);

                RectTransform rect = cartaARobar.GetComponent<RectTransform>();
                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
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
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            RobarCartas(1);
        }
    }
}