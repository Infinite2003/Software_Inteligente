using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public static class EstadoTableroLocal
{
    public static System.Collections.Generic.Dictionary<ulong, bool> yaHuboPokemonActivo =
        new System.Collections.Generic.Dictionary<ulong, bool>();
}

public class CartaInteractiva : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
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

    // ── Inicio ────────────────────────────────────────────────────────────────

    void Start()
    {
        escalaOriginal = transform.localScale;
        rectTransform = GetComponent<RectTransform>();
        cardtablero = GetComponent<CardTablero>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGlobal = GetComponentInParent<Canvas>();

        canvasInterno = GetComponent<Canvas>();
        if (canvasInterno == null)
            canvasInterno = gameObject.AddComponent<Canvas>();

        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        if (GetComponent<UnityEngine.UI.RectMask2D>() == null)
            gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
    }

    // ── Helpers de estado ─────────────────────────────────────────────────────

    bool EstaEnMano()
    {
        return transform.parent != null &&
               (transform.parent.name.Contains("Mano") ||
                transform.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null);
    }

    bool EstaEnBanca() => transform.parent != null && transform.parent.name == "Banca";

    bool EsCartaPokemon()
    {
        if (cardtablero?.cardData == null) return false;
        return cardtablero.cardData.category.ToString().Trim()
               .Equals("Pokemon", System.StringComparison.OrdinalIgnoreCase);
    }

    bool EsCartaTrainer()
    {
        if (cardtablero?.cardData == null) return false;
        return cardtablero.cardData.category.ToString().Trim()
               .Equals("Trainer", System.StringComparison.OrdinalIgnoreCase);
    }

    bool MiYaHuboPokemonActivo()
    {
        ulong miId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        return EstadoTableroLocal.yaHuboPokemonActivo.ContainsKey(miId) &&
               EstadoTableroLocal.yaHuboPokemonActivo[miId];
    }

    void MarcarYaHuboPokemonActivo()
    {
        ulong miId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        EstadoTableroLocal.yaHuboPokemonActivo[miId] = true;
    }

    private bool VerificarMouseEncimaReal()
    {
        Vector2 posicionMouse = Mouse.current.position.ReadValue();
        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform, posicionMouse, canvasGlobal.worldCamera);
    }

    /// <summary>
    /// Devuelve true si el jugador local ya tiene un Pokémon en su zona activa.
    /// Usa ZonaTablero si está disponible; cae a childCount como fallback.
    /// </summary>
    private bool HayPokemonActivo()
    {
        // Intento con ZonaTablero
        foreach (var z in FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None))
        {
            if (z.EsActivo() && z.EsMiZona() && z.EstaOcupada())
                return true;
        }

        // Fallback por nombre de GameObject
        GameObject j1 = GameObject.Find("CartaJugada_J1");
        GameObject j2 = GameObject.Find("CartaJugada_J2");
        if (j1 != null && j1.transform.childCount > 0) return true;
        if (j2 != null && j2.transform.childCount > 0) return true;

        return false;
    }

    // ── Eventos de puntero ────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (canvasInterno == null) return; // guarda de seguridad
        if (estaEnCementerio || estaSiendoArrastrada) return;
        if (transform.parent != null &&
            (transform.parent.name.Contains("CartaJugada") || transform.parent.name == "Apoyo")) return;
        if (!EstaEnMano() && !estaEnTablero) return;
        if (!VerificarMouseEncimaReal()) return;

        estaSobreLaCarta = true;
        canvasInterno.overrideSorting = true;
        canvasInterno.sortingOrder = 100;
        transform.localScale = escalaOriginal * 1.2f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (transform.parent != null &&
            (transform.parent.name.Contains("CartaJugada") || transform.parent.name == "Apoyo") ||
            estaSiendoArrastrada) return;

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

        if (transform.parent != null &&
            (transform.parent.name.Contains("CartaJugada") || transform.parent.name == "Apoyo"))
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

        // ── Buscar la zona activa del jugador local via ZonaTablero ──────────
        ZonaTablero zonaActivaLocal = null;
        foreach (var z in FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None))
        {
            if (z.EsMiZona() && z.EsActivo())
            {
                zonaActivaLocal = z;
                break;
            }
        }

        GameObject zonaCartaJugada = zonaActivaLocal != null ? zonaActivaLocal.gameObject : null;
        GameObject zonaBanca = GameObject.Find("Banca");
        GameObject zonaApoyo = GameObject.Find("Apoyo");

        // ── Distancias ───────────────────────────────────────────────────────
        float distanciaAActivo = zonaCartaJugada != null
            ? Vector3.Distance(transform.position, zonaCartaJugada.transform.position)
            : float.MaxValue;
        float distanciaABanca = zonaBanca != null
            ? Vector3.Distance(transform.position, zonaBanca.transform.position)
            : float.MaxValue;
        float distanciaAApoyo = zonaApoyo != null
            ? Vector3.Distance(transform.position, zonaApoyo.transform.position)
            : float.MaxValue;

        Debug.Log($"[Drag] activo={(zonaCartaJugada?.name ?? "NULL")} " +
                  $"dActivo={distanciaAActivo:F0} dBanca={distanciaABanca:F0} " +
                  $"dApoyo={distanciaAApoyo:F0} deteccion={distanciaDeteccion}");

        // ── ZONA DE APOYO ────────────────────────────────────────────────────
        if (distanciaAApoyo <= distanciaDeteccion &&
            distanciaAApoyo < distanciaAActivo &&
            distanciaAApoyo < distanciaABanca)
        {
            if (!EsCartaTrainer())
            {
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

        // ── ZONA ACTIVA ──────────────────────────────────────────────────────
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

            if (zonaActivaLocal != null && zonaActivaLocal.EstaOcupada())
            {
                Debug.LogWarning("La zona Activa ya está ocupada.");
                RegresarAPadreOriginal();
                return;
            }

            bool vieneDeBanca = padreOriginal.name == "Banca";
            bool vieneDeMano = padreOriginal.name.Contains("Mano") ||
                                padreOriginal.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null;

            if (vieneDeMano || vieneDeBanca)
            {
                MoverAContenedor(zonaCartaJugada.transform);
                pokemonInstance = new PokemonInstance(cardtablero.cardData);
                zonaActivaLocal?.ColocarPokemon(cardtablero.cardData);
                MarcarYaHuboPokemonActivo();
                estaEnTablero = true;
                canvasGroup.blocksRaycasts = false;
                Debug.Log("Pokémon Activo colocado: " + pokemonInstance.data.name);
                return;
            }

            Debug.LogWarning("Nuevos atacantes deben venir de la Banca.");
            RegresarAPadreOriginal();
            return;
        }

        // ── BANCA ────────────────────────────────────────────────────────────
        if (distanciaABanca <= distanciaDeteccion)
        {
            if (zonaBanca == null)
            {
                RegresarAPadreOriginal();
                return;
            }

            if (!EsCartaPokemon())
            {
                Debug.LogWarning("¡Solo Pokémon van en la Banca!");
                RegresarAPadreOriginal();
                return;
            }

            // Debe haber un Pokémon activo antes de poder poner en banca
            if (!HayPokemonActivo())
            {
                Debug.LogWarning("Tu primer Pokémon DEBE ser el Activo.");
                RegresarAPadreOriginal();
                return;
            }

            // No se puede mover una carta que ya está en banca, ni superar 5
            if (padreOriginal.name == "Banca" || zonaBanca.transform.childCount >= 5)
            {
                RegresarAPadreOriginal();
                return;
            }

            MoverAContenedor(zonaBanca.transform);
            pokemonInstance = new PokemonInstance(cardtablero.cardData);

            // Notificar a ZonaTablero de banca si existe
            foreach (var z in FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None))
            {
                if (z.EsMiZona() && !z.EsActivo() && !z.EstaOcupada())
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

    // ── Corrutina Apoyo ───────────────────────────────────────────────────────

    private IEnumerator TemporizadorMandarAlCementerio(float segundos)
    {
        yield return new WaitForSeconds(segundos);

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
        else
        {
            Debug.LogError("No se encontró 'Cementerio' en la escena.");
            RegresarAPadreOriginal();
        }
    }

    void RegresarAPadreOriginal() => MoverAContenedor(padreOriginal);

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (estaSiendoArrastrada) return;

        bool actualmenteEncima = VerificarMouseEncimaReal();

        if (transform.parent != null &&
            (transform.parent.name.Contains("CartaJugada") || transform.parent.name == "Apoyo"))
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

    // ── MoverAContenedor ──────────────────────────────────────────────────────

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