using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using static Unity.Netcode.RpcAttribute;

public class Gestor_Batalla : MonoBehaviour
{
    [Header("Zonas del Tablero")]
    [SerializeField] private ZonaTablero zonaActivaLocal;
    [SerializeField] private ZonaTablero zonaActivaRival;

    [Header("UI")]
    [SerializeField] private Button btnAtacar;
    [SerializeField] private Button btnEnergia;
    [SerializeField] private Button btnRetirada;
    [SerializeField] private TextMeshProUGUI textoResultado;

    [SerializeField] private AttackSelectionUI attackUI;
    
    private bool energiaUsadaEsteTurno = false;

    private ControladorTurnos controladorTurnos;

    private bool esperandoSeleccionEnergia = false;


    void Start()
    {
        ZonaTablero[] todasLasZonas = FindObjectsByType<ZonaTablero>(FindObjectsSortMode.None);

        foreach (var zona in todasLasZonas)
        {
            if (zona.EsMiZona())
                zonaActivaLocal = zona;
            else
                zonaActivaRival = zona;
        }

        controladorTurnos = FindFirstObjectByType<ControladorTurnos>();
        btnAtacar.onClick.AddListener(IntentarAtaque);
        btnEnergia.onClick.AddListener(IntentarAdjuntarEnergia);
        btnRetirada.onClick.AddListener(IntentarRetirada);
    }

    public void AlIniciarTurno()
    {
        energiaUsadaEsteTurno = false;
        ActualizarBotones();
    }

    void IntentarAtaque()
    {
        void IntentarAtaque()
        {
            if (!controladorTurnos.EsMiTurno())
                return;

            PokemonInstance atacante = zonaActivaLocal.pokemonEnZona;
            PokemonInstance defensor = zonaActivaRival.pokemonEnZona;

            if (atacante == null || defensor == null)
            {
                textoResultado.text = "Faltan Pokémon.";
                return;
            }

            attackUI.ShowAttacks(atacante, movimiento =>
            {
                EjecutarAtaque(atacante, defensor, movimiento);
            });
        }
    }



    void IntentarAdjuntarEnergia()
    {
        if (!controladorTurnos.EsMiTurno()) return;
        if (energiaUsadaEsteTurno)
        {
            textoResultado.text = "Ya adjuntaste energía este turno.";
            return;
        }

        PokemonInstance objetivo = zonaActivaLocal.pokemonEnZona;
        if (objetivo == null)
        {
            textoResultado.text = "No hay Pokémon activo para darle energía.";
            return;
        }

        Debug.Log("zonaActivaLocal = " + zonaActivaLocal);
        Debug.Log("pokemonEnZona = " + zonaActivaLocal?.pokemonEnZona);
        EnergySystem.AttachEnergy(objetivo);
        //energiaUsadaEsteTurno = true;
        textoResultado.text = $"Energía adjuntada a {objetivo.data.name}. Total: {objetivo.attachedEnergy}";
        ActualizarBotones();
    }

    void IntentarRetirada()
    {
        if (!controladorTurnos.EsMiTurno()) return;

        PokemonInstance activo = zonaActivaLocal.pokemonEnZona;
        if (activo == null) return;

        if (!RetreatSystem.CanRetreat(activo))
        {
            textoResultado.text = $"Necesitas {activo.data.retreat_cost} energía para retirar.";
            return;
        }

        RetreatSystem.Retreat(activo);
        textoResultado.text = $"{activo.data.name} se retiró.";
        // Aquí luego agregas la lógica para promover desde la banca
        ActualizarBotones();
    }

    void ActualizarBotones()
    {
        bool esMiTurno = controladorTurnos != null && controladorTurnos.EsMiTurno();
        btnAtacar.interactable = esMiTurno && zonaActivaLocal.EstaOcupada() && zonaActivaRival.EstaOcupada();
        btnEnergia.interactable = esMiTurno && !energiaUsadaEsteTurno && zonaActivaLocal.EstaOcupada();
        btnRetirada.interactable = esMiTurno && zonaActivaLocal.EstaOcupada();
    }

    private void EjecutarAtaque(PokemonInstance atacante, PokemonInstance defensor, Move movimiento)
    {
        if (!AttackSystem.CanUseMove(atacante, movimiento))
        {
            textoResultado.text = "No hay suficiente energía.";
            return;
        }

        AttackSystem.UseMove(atacante, defensor, movimiento);

        textoResultado.text =
            $"{atacante.data.name} usó {movimiento.name}!";

        if (KoSystem.CheckKO(defensor))
        {
            textoResultado.text +=
                $"\n¡{defensor.data.name} fue derrotado!";

            zonaActivaRival.LiberarZona();
        }

        ActualizarBotones();
    }
}