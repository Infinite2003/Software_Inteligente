using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections; // REQUISITO: Necesario para usar Corrutinas (WaitForSeconds)

public static class EstadoTableroLocal
{
    public static System.Collections.Generic.Dictionary<ulong, bool> yaHuboPokemonActivo = new System.Collections.Generic.Dictionary<ulong, bool>();
}

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

    private CardTablero cardtablero;

    [SerializeField] private float distanciaDeteccion = 150f;

    public PokemonInstance pokemonInstance;

    void Start()
    {
        escalaOriginal = transform.localScale;
        rectTransform = GetComponent<RectTransform>();
        cardtablero = GetComponent<CardTablero>();

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
        if (cardtablero != null && cardtablero.cardData != null)
        {
            string categoria = cardtablero.cardData.category.ToString().Trim();
            return categoria.Equals("Pokemon", System.StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    // Detecta si la carta actual es de categoría Trainer (Entrenador)
    bool EsCartaTrainer()
    {
        if (cardtablero != null && cardtablero.cardData != null)
        {
            string categoria = cardtablero.cardData.category.ToString().Trim();
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

        ZonaTablero zonaActivaLocal = null;

        foreach (var z in FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None))
        {
            if (z.EsMiZona() && z.EsActivo())
            {
                zonaActivaLocal = z;
                break;
            }
        }

        GameObject zonaCartaJugada =
            zonaActivaLocal != null ? zonaActivaLocal.gameObject : null;
        GameObject zonaBanca = GameObject.Find("Banca");
        GameObject zonaApoyo = GameObject.Find("Apoyo");

        bool cartaJugadaEstaVacia = (zonaCartaJugada != null && zonaCartaJugada.transform.childCount == 0);

        // ── Distancias a cada zona ────────────────────────────────────────────
        float distanciaAActivo = zonaCartaJugada != null
            ? Vector3.Distance(transform.position, zonaCartaJugada.transform.position)
            : float.MaxValue;
        float distanciaABanca = zonaBanca != null
            ? Vector3.Distance(transform.position, zonaBanca.transform.position)
            : float.MaxValue;
        float distanciaAApoyo = zonaApoyo != null
            ? Vector3.Distance(transform.position, zonaApoyo.transform.position)
            : float.MaxValue;

        Debug.Log($"[Drag] zonaCartaJugada={(zonaCartaJugada != null ? zonaCartaJugada.name : "NULL")} | " +
          $"zonaBanca={(zonaBanca != null ? zonaBanca.name : "NULL")} | " +
          $"zonaApoyo={(zonaApoyo != null ? zonaApoyo.name : "NULL")} | " +
          $"distActivo={distanciaAActivo:F0} | distBanca={distanciaABanca:F0} | distApoyo={distanciaAApoyo:F0} | " +
          $"deteccion={distanciaDeteccion}");

        // ── ZONA DE APOYO ─────────────────────────────────────────────────────
        if (distanciaAApoyo <= distanciaDeteccion &&
            distanciaAApoyo < distanciaAActivo &&
            distanciaAApoyo < distanciaABanca)
        {
            if (!EsCartaTrainer())
            {
                // REGLA: �Solo cartas Trainer (Entrenador) pueden ir a Apoyo!
                if (!EsCartaTrainer())
                {
                    Debug.LogWarning("�Acci�n inv�lida! Solo cartas de tipo Trainer pueden usarse en Apoyo.");
                    RegresarAPadreOriginal();
                    return;
                }

                if (zonaApoyo.transform.childCount > 0)
                {
                    Debug.LogWarning("La zona de Apoyo ya est� ocupada procesando otro efecto.");
                    RegresarAPadreOriginal();
                    return;
                }
                Debug.LogWarning("¡Solo cartas Trainer pueden usarse en Apoyo!");
                RegresarAPadreOriginal();
                return;
            }

            if (zonaApoyo.transform.childCount > 0)
            {
                Debug.LogWarning("La zona de Apoyo ya está ocupada.");
                RegresarAPadreOriginal();
                return;
            }

            MoverAContenedor(zonaApoyo.transform);
            estaEnTablero = true;
            canvasGroup.blocksRaycasts = false;
            StartCoroutine(TemporizadorMandarAlCementerio(10f));
            return;
        }

        // ── ZONA ACTIVA (CartaJugada) ─────────────────────────────────────────
        if (distanciaAActivo < distanciaABanca && distanciaAActivo <= distanciaDeteccion)
        {
            if (zonaCartaJugada == null)
            {
                RegresarAPadreOriginal();
                return;
            }

            if (!EsCartaPokemon())
            {
                Debug.LogWarning("¡Solo Pokémon pueden ser el Activo!");
                RegresarAPadreOriginal();
                return;
            }

            bool zonaOcupada = zonaActivaLocal != null && zonaActivaLocal.EstaOcupada();



            if (zonaOcupada)
            {
                Debug.LogWarning("La zona Activa ya está ocupada.");
                RegresarAPadreOriginal();
                return;
            }

            bool vieneDeBanca = (padreOriginal.name == "Banca");
            bool vieneDeMano = (padreOriginal.name.Contains("Mano") ||
                                 padreOriginal.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null);

            // 🔴 CAMBIO 3: Eliminada condición !yaHuboPokemonActivo
            bool puedeIrAlActivo = vieneDeMano || vieneDeBanca;

            if (puedeIrAlActivo)
            {
                MoverAContenedor(zonaCartaJugada.transform);

                // 🔴 CAMBIO 4: Crear PokemonInstance ANTES de usarlo
                pokemonInstance = new PokemonInstance(cardtablero.cardData);

                if (zonaActivaLocal != null)
                    zonaActivaLocal.ColocarPokemon(cardtablero.cardData);

                estaEnTablero = true;
                canvasGroup.blocksRaycasts = false; // Desactivamos interacci�n mientras se procesa

                // �ACTIVAMOS EL TEMPORIZADOR DE 10 SEGUNDOS AL CEMENTERIO!
                //StartCoroutine(TemporizadorMandarAlCementerio(10f));
                canvasGroup.blocksRaycasts = false;
                Debug.Log("Pokémon Activo colocado: " + pokemonInstance.data.name);
                return;
            }
            else
            {
                Debug.LogWarning("Nuevos atacantes deben venir de la Banca.");
                RegresarAPadreOriginal();
                return;
            }
        }

        // ── BANCA ─────────────────────────────────────────────────────────────
        if (distanciaABanca <= distanciaDeteccion)
        {
            if (zonaBanca == null)
            {
                if (!EsCartaPokemon())
                {
                    Debug.LogWarning("Acción invalida! Solo cartas de tipo Pokémon pueden ser el Activo.");
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

                if (vieneDeBanca || (vieneDeMano && !MiYaHuboPokemonActivo()))
                {
                    MoverAContenedor(zonaCartaJugada.transform);

                    if(pokemonInstance == null)
                    {

                        pokemonInstance = new PokemonInstance(cardtablero.cardData);
                    }

                    estaEnTablero = true;
                    Debug.Log("Pokemon Activo: " + pokemonInstance.data.name);
                    MarcarYaHuboPokemonActivo();
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
        // LA BANCA
        else if (distanciaABanca <= distanciaDeteccion)
        {
            if (zonaBanca != null)
            {
                if (!EsCartaPokemon())
                {
                    Debug.LogWarning("�Acci�n inv�lida! Las cartas de Entrenador o Energ�a no van en la Banca.");
                    RegresarAPadreOriginal();
                    return;
                }

                if (cartaJugadaEstaVacia && !MiYaHuboPokemonActivo())
                {
                    Debug.LogWarning("Tu primer Pok�mon DEBE ser el Activo en 'CartaJugada'.");
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

                    pokemonInstance = new PokemonInstance(cardtablero.cardData);
                }

                estaEnTablero = true;
                Debug.Log("Pokemon enviado a banca: " + pokemonInstance.data.name);
                RegresarAPadreOriginal();
                return;
            }

            if (!EsCartaPokemon())
            {
                Debug.LogWarning("¡Solo Pokémon van en la Banca!");
                RegresarAPadreOriginal();
                return;
            }

            bool hayActivo = zonaActivaLocal != null && zonaActivaLocal.EstaOcupada();
            if (!hayActivo)
            {
                Debug.LogWarning("Tu primer Pokémon DEBE ser el Activo.");
                RegresarAPadreOriginal();
                return;
            }

            if (padreOriginal.name == "Banca" || zonaBanca.transform.childCount >= 5)
            {
                RegresarAPadreOriginal();
                return;
            }

            MoverAContenedor(zonaBanca.transform);

            // 🔴 CAMBIO 5: Crear PokemonInstance siempre
            pokemonInstance = new PokemonInstance(cardtablero.cardData);

            // 🔴 CAMBIO 6: Notificar a ZonaTablero del slot de banca
            ZonaTablero[] bancaZonas = FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None);
            Debug.Log($"[Drag] ZonaTablero encontradas: {bancaZonas.Length}");
            foreach (var z in FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None))
            {
                Debug.Log($"[ZonaTablero] '{z.gameObject.name}' | EsMiZona={z.EsMiZona()} | " +
                          $"EsActivo={z.EsActivo()} | EstaOcupada={z.EstaOcupada()} | " +
                          $"IsHost={Unity.Netcode.NetworkManager.Singleton.IsHost}");
            }
            foreach (var z in bancaZonas)
            {
                if (z.EsMiZona() && z.gameObject.name.Contains("Banca") && !z.EstaOcupada())
                {
                    z.ColocarPokemon(cardtablero.cardData);
                    break;
                }
            }

            estaEnTablero = true;
            Debug.Log("Pokémon enviado a banca: " + pokemonInstance.data.name);
            return;
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

    // Helper para leer
    bool MiYaHuboPokemonActivo()
    {
        ulong miId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        return EstadoTableroLocal.yaHuboPokemonActivo.ContainsKey(miId)
               && EstadoTableroLocal.yaHuboPokemonActivo[miId];
    }

    // Helper para escribir
    void MarcarYaHuboPokemonActivo()
    {
        ulong miId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        EstadoTableroLocal.yaHuboPokemonActivo[miId] = true;
    }
}
