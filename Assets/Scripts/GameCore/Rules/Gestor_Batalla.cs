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

    private bool energiaUsadaEsteTurno = false;

    private ControladorTurnos controladorTurnos;

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
        if (!controladorTurnos.EsMiTurno()) return;

        PokemonInstance atacante = zonaActivaLocal.pokemonEnZona;
        PokemonInstance defensor = zonaActivaRival.pokemonEnZona;

        if (atacante == null || defensor == null)
        {
            Debug.Log("zonaActivaLocal = " + zonaActivaLocal);
            Debug.Log("pokemonEnZona = " + zonaActivaLocal?.pokemonEnZona);
            textoResultado.text = "Faltan Pokémon en el tablero.";
            return;
        }

        if (atacante.data.moves == null || atacante.data.moves.Count == 0)
        {
            textoResultado.text = $"{atacante.data.name} no tiene ataques.";
            return;
        }

        // Por ahora usa el primer ataque (luego puedes añadir un selector de movimiento)
        Move movimiento = atacante.data.moves[0];

        if (!AttackSystem.CanUseMove(atacante, movimiento))
        {
            textoResultado.text = "No hay suficiente energía para atacar.";
            return;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void AtacarServerRpc(int dañoCalculado)
        {
            // El servidor le dice a la zona rival que reciba daño
            zonaActivaRival.RecibirDaño(dañoCalculado);

            if (KoSystem.CheckKO(zonaActivaRival.pokemonEnZona))
                ZonaKoClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        void ZonaKoClientRpc()
        {
            textoResultado.text = "¡Pokémon rival derrotado!";
            zonaActivaRival.LiberarZona();
        }
        textoResultado.text = $"{atacante.data.name} usó {movimiento.name}!";

        // Revisar KO inmediatamente después del ataque
        if (KoSystem.CheckKO(defensor))
        {
            textoResultado.text += $"\n¡{defensor.data.name} fue derrotado!";
            zonaActivaRival.LiberarZona();
            // Aquí puedes llamar a tu lógica de puntos de premio
        }

        ActualizarBotones();
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
        energiaUsadaEsteTurno = true;
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
}