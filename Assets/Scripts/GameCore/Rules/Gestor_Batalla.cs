using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Gestor_Batalla : MonoBehaviour
{
    [Header("UI - Botones")]
    [SerializeField] private Button btnAtacar;
    [SerializeField] private Button btnEnergia;
    [SerializeField] private Button btnRetirada;

    [Header("UI - Feedback")]
    [SerializeField] private TextMeshProUGUI textoResultado;

    // ZonaTablero se busca en runtime, no desde el Inspector,
    // porque depende de quién es el jugador local (Host o Cliente)
    private ZonaTablero zonaActivaLocal;
    private ZonaTablero zonaActivaRival;

    private ControladorTurnos controladorTurnos;
    private bool energiaUsadaEsteTurno = false;

    // ── Singleton de escena ───────────────────────────────────────────────────
    public static Gestor_Batalla Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        controladorTurnos = FindFirstObjectByType<ControladorTurnos>();

        btnAtacar.onClick.AddListener(IntentarAtaque);
        btnEnergia.onClick.AddListener(IntentarAdjuntarEnergia);
        btnRetirada.onClick.AddListener(IntentarRetirada);

        // Desactivamos botones hasta que la red esté lista y haya Pokémon en juego
        DesactivarTodosBotones();

        // Esperamos a que la red esté lista para buscar las zonas correctamente
        StartCoroutine(EsperarRedYBuscarZonas());
    }

    // ── Inicialización de zonas ───────────────────────────────────────────────

    private IEnumerator EsperarRedYBuscarZonas()
    {
        // Esperamos a que NetworkManager esté activo y escuchando
        yield return new WaitUntil(() =>
            Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsListening);

        // Un frame extra para que LocalClientId esté asignado
        yield return new WaitForSeconds(0.5f);

        BuscarZonas();
    }

    private void BuscarZonas()
    {
        ZonaTablero[] todasLasZonas = FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None);

        foreach (var zona in todasLasZonas)
        {
            // Solo nos interesan las zonas activas (no la banca)
            if (!zona.EsActivo()) continue;

            if (zona.EsMiZona())
                zonaActivaLocal = zona;
            else
                zonaActivaRival = zona;
        }

        Debug.Log($"[Gestor_Batalla] Local={zonaActivaLocal?.gameObject.name ?? "NULL"} " +
                  $"| Rival={zonaActivaRival?.gameObject.name ?? "NULL"}");

        ActualizarBotones();
    }

    // ── Llamado por ControladorTurnos al inicio de cada turno ────────────────

    public void AlIniciarTurno()
    {
        energiaUsadaEsteTurno = false;

        // Si las zonas no se encontraron aún, intentamos de nuevo
        if (zonaActivaLocal == null || zonaActivaRival == null)
            BuscarZonas();

        ActualizarBotones();
        MostrarFeedback("¡Es tu turno!");
    }

    // ── ATACAR ────────────────────────────────────────────────────────────────

    private void IntentarAtaque()
    {
        if (!PuedoActuar()) return;

        PokemonInstance atacante = zonaActivaLocal.pokemonEnZona;
        PokemonInstance defensor = zonaActivaRival.pokemonEnZona;

        if (atacante == null)
        {
            MostrarFeedback("No tienes Pokémon activo.");
            return;
        }

        if (defensor == null)
        {
            MostrarFeedback("El rival no tiene Pokémon activo.");
            return;
        }

        if (atacante.data.moves == null || atacante.data.moves.Count == 0)
        {
            MostrarFeedback($"{atacante.data.name} no tiene ataques.");
            return;
        }

        // Por ahora usamos el primer ataque disponible con suficiente energía.
        // Cuando tengas AttackSelectionUI, reemplaza esto por attackUI.ShowAttacks(...)
        Move movimiento = null;
        foreach (var mov in atacante.data.moves)
        {
            if (AttackSystem.CanUseMove(atacante, mov))
            {
                movimiento = mov;
                break;
            }
        }

        if (movimiento == null)
        {
            MostrarFeedback("No tienes energía suficiente para ningún ataque.");
            return;
        }

        EjecutarAtaque(atacante, defensor, movimiento);
    }

    private void EjecutarAtaque(PokemonInstance atacante, PokemonInstance defensor, Move movimiento)
    {
        AttackSystem.UseMove(atacante, defensor, movimiento);
        MostrarFeedback($"{atacante.data.name} usó {movimiento.name}!");

        if (KoSystem.CheckKO(defensor))
        {
            MostrarFeedback($"{atacante.data.name} usó {movimiento.name}!\n" +
                            $"¡{defensor.data.name} fue derrotado!");
            zonaActivaRival.LiberarZona();
        }

        // En TCG Pocket atacar termina el turno automáticamente
        // Si quieres ese comportamiento descomenta la línea siguiente:
        // controladorTurnos?.SolicitarCambiarTurno();

        ActualizarBotones();
    }

    // ── ENERGÍA ───────────────────────────────────────────────────────────────

    private void IntentarAdjuntarEnergia()
    {
        if (!PuedoActuar()) return;

        if (energiaUsadaEsteTurno)
        {
            MostrarFeedback("Ya adjuntaste energía este turno.");
            return;
        }

        PokemonInstance objetivo = zonaActivaLocal.pokemonEnZona;
        if (objetivo == null)
        {
            MostrarFeedback("No hay Pokémon activo para darle energía.");
            return;
        }

        EnergySystem.AttachEnergy(objetivo);
        energiaUsadaEsteTurno = true;
        MostrarFeedback($"Energía adjuntada a {objetivo.data.name}. Total: {objetivo.attachedEnergy}");
        ActualizarBotones();
    }

    // ── RETIRADA ──────────────────────────────────────────────────────────────

    private void IntentarRetirada()
    {
        if (!PuedoActuar()) return;

        PokemonInstance activo = zonaActivaLocal.pokemonEnZona;
        if (activo == null)
        {
            MostrarFeedback("No tienes Pokémon activo.");
            return;
        }

        if (!RetreatSystem.CanRetreat(activo))
        {
            MostrarFeedback($"Necesitas {activo.data.retreat_cost} energía para retirar.");
            return;
        }

        // Buscamos la carta visual que está en la zona activa
        GameObject zonaBanca = GameObject.Find("Banca");
        if (zonaBanca == null)
        {
            MostrarFeedback("No se encontró la Banca.");
            return;
        }

        // Movemos la carta visual del activo a la banca
        Transform zonaActivaTransform = zonaActivaLocal.transform;
        if (zonaActivaTransform.childCount > 0)
        {
            Transform cartaVisual = zonaActivaTransform.GetChild(0);
            cartaVisual.SetParent(zonaBanca.transform);

            // Reposicionamos la carta en la banca
            RectTransform rect = cartaVisual.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, -0.2f);
                rect.anchoredPosition = Vector2.zero;
                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            // Reactivamos el drag de la carta para que pueda volver a arrastrarse
            CartaInteractiva ci = cartaVisual.GetComponent<CartaInteractiva>();
            if (ci != null)
            {
                CanvasGroup cg = cartaVisual.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = true;
            }
        }

        // Descuentan la energía del costo de retirada
        RetreatSystem.Retreat(activo);

        // Liberamos la zona activa
        zonaActivaLocal.LiberarZona();

        MostrarFeedback($"{activo.data.name} se retiró a la banca. Arrastra un Pokémon de la banca al activo.");
        ActualizarBotones();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool PuedoActuar()
    {
        if (controladorTurnos != null && !controladorTurnos.EsMiTurno())
        {
            MostrarFeedback("No es tu turno.");
            return false;
        }
        return true;
    }

    private void ActualizarBotones()
    {
        bool esMiTurno = controladorTurnos != null && controladorTurnos.EsMiTurno();
        bool hayLocal = zonaActivaLocal != null && zonaActivaLocal.EstaOcupada();
        bool hayRival = zonaActivaRival != null && zonaActivaRival.EstaOcupada();

        btnAtacar.interactable = esMiTurno && hayLocal && hayRival;
        btnEnergia.interactable = esMiTurno && hayLocal && !energiaUsadaEsteTurno;
        btnRetirada.interactable = esMiTurno && hayLocal;
    }

    private void DesactivarTodosBotones()
    {
        btnAtacar.interactable = false;
        btnEnergia.interactable = false;
        btnRetirada.interactable = false;
    }

    private void MostrarFeedback(string mensaje)
    {
        if (textoResultado != null)
            textoResultado.text = mensaje;
        Debug.Log($"[Gestor_Batalla] {mensaje}");
    }
}