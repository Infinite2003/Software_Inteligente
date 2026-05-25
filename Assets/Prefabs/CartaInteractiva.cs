using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections; // REQUISITO: Necesario para usar Corrutinas (WaitForSeconds)

public class CartaInteractiva : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 escalaOriginal;
    private bool estaSobreLaCarta = false;
    private bool estaEnTablero = false;
    private bool estaEnCementerio = false;
    private bool estaSiendoArrastrada = false;

    private Transform padreOriginal;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvasGlobal;

    private Canvas canvasInterno;
    private UnityEngine.UI.GraphicRaycaster raycasterInterno;

    private CardUI cardUI;

    [SerializeField] private float distanciaDeteccion = 150f;

    private static bool yaHuboPokemonActivo = false;


    public PokemonInstance pokemonInstance;
    void Start()
    {
        escalaOriginal = transform.localScale;
        rectTransform = GetComponent<RectTransform>();
        cardUI = GetComponent<CardUI>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGlobal = GetComponentInParent<Canvas>();

        canvasInterno = GetComponent<Canvas>();
        if (canvasInterno == null)
        {
            canvasInterno = gameObject.AddComponent<Canvas>();
        }

        raycasterInterno = GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycasterInterno == null)
        {
            raycasterInterno = gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        if (GetComponent<UnityEngine.UI.RectMask2D>() == null)
        {
            gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        }
    }

    bool EstaEnMano()
    {
        return transform.parent != null && (transform.parent.name.Contains("Mano") || transform.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null);
    }

    bool EstaEnBanca()
    {
        return transform.parent != null && transform.parent.name == "Banca";
    }

    bool EsCartaPokemon()
    {
        if (cardUI != null && cardUI.cardData != null)
        {
            string categoria = cardUI.cardData.category.ToString().Trim();
            return categoria.Equals("Pokemon", System.StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    // NUEVA FUNCIÓN: Detecta si la carta actual es de categoría Trainer (Entrenador)
    bool EsCartaTrainer()
    {
        if (cardUI != null && cardUI.cardData != null)
        {
            string categoria = cardUI.cardData.category.ToString().Trim();
            return categoria.Equals("Trainer", System.StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private bool VerificarMouseEncimaReal()
    {
        Vector2 posicionMouse = Mouse.current.position.ReadValue();
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, posicionMouse, canvasGlobal.worldCamera);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (estaEnCementerio || estaSiendoArrastrada) return;
        if (transform.parent != null && (transform.parent.name == "CartaJugada" || transform.parent.name == "Apoyo")) return;
        if (!EstaEnMano() && !estaEnTablero) return;

        if (!VerificarMouseEncimaReal()) return;

        estaSobreLaCarta = true;

        canvasInterno.overrideSorting = true;
        canvasInterno.sortingOrder = 100;

        transform.localScale = escalaOriginal * 1.2f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (transform.parent != null && (transform.parent.name == "CartaJugada" || transform.parent.name == "Apoyo") || estaSiendoArrastrada) return;

        estaSobreLaCarta = false;
        canvasInterno.overrideSorting = false;
        canvasInterno.sortingOrder = 0;
        transform.localScale = escalaOriginal;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ControladorTurnos sistemaTurnos = Object.FindFirstObjectByType<ControladorTurnos>();
        if (sistemaTurnos != null && !sistemaTurnos.EsMiTurno())
        {
            Debug.LogWarning("No puedes jugar cartas, ¡es el turno del rival!");
            eventData.pointerDrag = null;
            return;
        }

        if (estaEnCementerio) return;

        // Si la carta ya está en la zona de juego o en Apoyo, bloqueamos que se pueda volver a arrastrar
        if (transform.parent != null && (transform.parent.name == "CartaJugada" || transform.parent.name == "Apoyo"))
        {
            eventData.pointerDrag = null;
            return;
        }

        if (!EstaEnMano() && !EstaEnBanca()) return;

        estaSiendoArrastrada = true;
        estaSobreLaCarta = false;

        padreOriginal = transform.parent;
        transform.SetParent(canvasGlobal.transform);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (estaEnCementerio || padreOriginal == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvasGlobal.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (estaEnCementerio || padreOriginal == null) return;

        estaSiendoArrastrada = false;
        canvasGroup.blocksRaycasts = true;
        transform.localScale = escalaOriginal;

        canvasInterno.overrideSorting = false;
        canvasInterno.sortingOrder = 0;

        GameObject zonaCartaJugada = GameObject.Find("CartaJugada");
        GameObject zonaBanca = GameObject.Find("Banca");
        GameObject zonaApoyo = GameObject.Find("Apoyo"); // Buscamos la zona de Apoyo en el mapa

        bool cartaJugadaEstaVacia = (zonaCartaJugada != null && zonaCartaJugada.transform.childCount == 0);

        // Calculamos distancias para ver dónde se soltó la carta
        float distanciaAActivo = zonaCartaJugada != null ? Vector3.Distance(transform.position, zonaCartaJugada.transform.position) : float.MaxValue;
        float distanciaABanca = zonaBanca != null ? Vector3.Distance(transform.position, zonaBanca.transform.position) : float.MaxValue;
        float distanciaAApoyo = zonaApoyo != null ? Vector3.Distance(transform.position, zonaApoyo.transform.position) : float.MaxValue;

        // --- FILTRO 1: ¿SE DETECTÓ EN LA ZONA DE APOYO? ---
        if (distanciaAApoyo <= distanciaDeteccion && distanciaAApoyo < distanciaAActivo && distanciaAApoyo < distanciaABanca)
        {
            if (zonaApoyo != null)
            {
                // REGLA: ¡Solo cartas Trainer (Entrenador) pueden ir a Apoyo!
                if (!EsCartaTrainer())
                {
                    Debug.LogWarning("¡Acción inválida! Solo cartas de tipo Trainer pueden usarse en Apoyo.");
                    RegresarAPadreOriginal();
                    return;
                }

                if (zonaApoyo.transform.childCount > 0)
                {
                    Debug.LogWarning("La zona de Apoyo ya está ocupada procesando otro efecto.");
                    RegresarAPadreOriginal();
                    return;
                }

                // Colocamos la carta Trainer en la zona de Apoyo
                MoverAContenedor(zonaApoyo.transform);
                estaEnTablero = true;
                canvasGroup.blocksRaycasts = false; // Desactivamos interacción mientras se procesa

                // ¡ACTIVAMOS EL TEMPORIZADOR DE 10 SEGUNDOS AL CEMENTERIO!
                StartCoroutine(TemporizadorMandarAlCementerio(10f));
                return;
            }
        }
        // --- FILTRO 2: CARTA JUGADA (ACTIVO) ---
        else if (distanciaAActivo < distanciaABanca && distanciaAActivo <= distanciaDeteccion)
        {
            if (zonaCartaJugada != null)
            {
                if (!EsCartaPokemon())
                {
                    Debug.LogWarning("¡Acción inválida! Solo cartas de tipo Pokémon pueden ser el Activo.");
                    RegresarAPadreOriginal();
                    return;
                }

                if (!cartaJugadaEstaVacia)
                {
                    Debug.LogWarning("La zona 'CartaJugada' ya está ocupada.");
                    RegresarAPadreOriginal();
                    return;
                }

                bool vieneDeBanca = (padreOriginal.name == "Banca");
                bool vieneDeMano = (padreOriginal.name.Contains("Mano") || padreOriginal.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null);

                if (vieneDeBanca || (vieneDeMano && !yaHuboPokemonActivo))
                {
                    MoverAContenedor(zonaCartaJugada.transform);

                    if(pokemonInstance == null)
                    {

                        pokemonInstance = new PokemonInstance(cardUI.cardData);
                    }

                    estaEnTablero = true;
                    Debug.Log("Pokemon Activo: " + pokemonInstance.data.name);
                    yaHuboPokemonActivo = true;
                    canvasGroup.blocksRaycasts = false;
                    return;
                }
                else
                {
                    Debug.LogWarning("Nuevos atacantes deben venir obligatoriamente de la Banca.");
                    RegresarAPadreOriginal();
                    return;
                }
            }
        }
        // --- FILTRO 3: LA BANCA ---
        else if (distanciaABanca <= distanciaDeteccion)
        {
            if (zonaBanca != null)
            {
                if (!EsCartaPokemon())
                {
                    Debug.LogWarning("¡Acción inválida! Las cartas de Entrenador o Energía no van en la Banca.");
                    RegresarAPadreOriginal();
                    return;
                }

                if (cartaJugadaEstaVacia && !yaHuboPokemonActivo)
                {
                    Debug.LogWarning("Tu primer Pokémon DEBE ser el Activo en 'CartaJugada'.");
                    RegresarAPadreOriginal();
                    return;
                }

                if (padreOriginal.name == "Banca" || zonaBanca.transform.childCount >= 5)
                {
                    RegresarAPadreOriginal();
                    return;
                }

                MoverAContenedor(zonaBanca.transform);

                if(pokemonInstance == null)
                {

                    pokemonInstance = new PokemonInstance(cardUI.cardData);
                }

                estaEnTablero = true;
                Debug.Log("Pokemon enviado a banca: " + pokemonInstance.data.name);
                return;
            }
        }

        RegresarAPadreOriginal();
    }

    // CORRUTINA: Cuenta el tiempo en segundo plano y desecha la carta de apoyo de forma segura
    private IEnumerator TemporizadorMandarAlCementerio(float segundos)
    {
        yield return new WaitForSeconds(segundos);

        GameObject cementerio = GameObject.Find("Cementerio");
        if (cementerio != null)
        {
            Debug.Log($"El efecto de {gameObject.name} terminó. Moviendo al cementerio.");

            canvasGroup.blocksRaycasts = true; // Devolvemos la capacidad de interactuar por si revive
            canvasInterno.overrideSorting = false;
            canvasInterno.sortingOrder = 0;

            MoverAContenedor(cementerio.transform);
            estaEnTablero = false;
            estaEnCementerio = true;
            estaSobreLaCarta = false;
            transform.localScale = escalaOriginal;
        }
        else
        {
            Debug.LogError("No se encontró el objeto 'Cementerio' en la escena para descartar el Apoyo.");
            RegresarAPadreOriginal();
        }
    }

    void RegresarAPadreOriginal()
    {
        MoverAContenedor(padreOriginal);
    }

    void Update()
    {
        if (estaSiendoArrastrada) return;

        bool actualmenteEncima = VerificarMouseEncimaReal();

        // Bloqueamos animaciones de escala si está en CartaJugada o Apoyo
        if (transform.parent != null && (transform.parent.name == "CartaJugada" || transform.parent.name == "Apoyo"))
        {
            if (actualmenteEncima && !estaSobreLaCarta)
            {
                estaSobreLaCarta = true;
                transform.localScale = escalaOriginal * 1.2f;
            }
            else if (!actualmenteEncima && estaSobreLaCarta)
            {
                estaSobreLaCarta = false;
                transform.localScale = escalaOriginal;
            }
        }
        else if (estaEnTablero || EstaEnMano())
        {
            if (!actualmenteEncima && estaSobreLaCarta)
            {
                estaSobreLaCarta = false;
                canvasInterno.overrideSorting = false;
                canvasInterno.sortingOrder = 0;
                transform.localScale = escalaOriginal;
            }
        }

        if (estaEnTablero && estaSobreLaCarta && Keyboard.current.yKey.wasPressedThisFrame)
        {
            GameObject cementerio = GameObject.Find("Cementerio");
            if (cementerio != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasInterno.overrideSorting = false;
                canvasInterno.sortingOrder = 0;
                MoverAContenedor(cementerio.transform);
                estaEnTablero = false;
                estaEnCementerio = true;
                estaSobreLaCarta = false;
                transform.localScale = escalaOriginal;
            }
        }
    }

    void MoverAContenedor(Transform nuevoPadre)
    {
        transform.SetParent(nuevoPadre);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, -0.2f);

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
    }
}