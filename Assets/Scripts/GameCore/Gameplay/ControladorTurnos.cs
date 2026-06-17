using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorTurnos : MonoBehaviour
{
    [Header("UI Componentes")]
    [SerializeField] private Button btnPasarTurno;
    [SerializeField] private TextMeshProUGUI textoIndicadorTurno;
    [SerializeField] private TextMeshProUGUI contadorDeTurnos;

    private SincronizadorRed red;

    void Start()
    {
        // El botón llamará a una función que le pide al servidor cambiar el turno
        if (btnPasarTurno != null)
        {
            btnPasarTurno.onClick.AddListener(SolicitarCambiarTurno);
        }

        // Esperar a que SincronizadorRed esté listo antes de suscribirse
        StartCoroutine(EsperarSincronizador());
    }

    private System.Collections.IEnumerator EsperarSincronizador()
    {
        // Esperamos hasta que esté disponible.
        yield return new UnityEngine.WaitUntil(() => SincronizadorRed.Instancia != null);

        red = SincronizadorRed.Instancia;
        red.OnTurnoCambiado += AlCambiarElTurno;

        ActualizarInterfazTurno();
    }

    void OnDestroy()
    {
        if (red != null)
            red.OnTurnoCambiado -= AlCambiarElTurno;
    }


    // Devuelve 'true' si es el turno de la persona que está mirando esta pantalla
    public bool EsMiTurno()
    {
        if (red == null) return false;
        return Unity.Netcode.NetworkManager.Singleton.LocalClientId == red.JugadorActualTurno;
    }

    // NUEVA FUNCIÓN: Permite que el script "CartaInteractiva" consulte si estamos en el primer turno de la partida
    public bool EsPrimerTurno()
    {
        if (red == null) return true;
        return red.NumeroDeTurno == 1;
    }

    private void SolicitarCambiarTurno()
    {
        // Seguridad: Solo puedes pasar el turno si actualmente es tu turno
        if (!EsMiTurno()) return;

        // Le pedimos al servidor que pase el turno
        red.CambiarTurnoServerRpc();
    }

    private void AlCambiarElTurno(ulong turnoAnterior, ulong turnoNuevo)
    {
        if(turnoAnterior == ulong.MaxValue)
        {
            ActualizarInterfazTurno();
            return; 
        }

        if (EsMiTurno() && !EsPrimerTurno())
        {
            // Buscamos el GeneradorMazo en la escena actual
            GeneradorMazo generadorMazo = Object.FindFirstObjectByType<GeneradorMazo>();

            if (generadorMazo != null)
            {
                Debug.Log("¡Inicio de turno! Robando 1 carta automáticamente.");
                generadorMazo.RobarCartas(1);

            }
            else
            {
                Debug.LogWarning("No se encontró el GeneradorMazo en la escena para efectuar el robo automático.");
            }

            Gestor_Batalla gestor = Object.FindFirstObjectByType<Gestor_Batalla>();
            if (gestor != null && EsMiTurno())
                gestor.AlIniciarTurno();
        }
        ActualizarInterfazTurno();
    }

    private void ActualizarInterfazTurno()
    {
        // Actualizamos el contador de turnos en ambos jugadores
        if (contadorDeTurnos != null && red != null)
            contadorDeTurnos.text = "Turno: " + red.NumeroDeTurno;

        // Todavía no hay turno asignado
        if (red == null || red.JugadorActualTurno == ulong.MaxValue)
        {
            if (textoIndicadorTurno != null)
                textoIndicadorTurno.text = "Esperando jugadores...";
            if (btnPasarTurno != null)
                btnPasarTurno.interactable = false;
            return;
        }

        if (EsMiTurno())
        {
            // Modificamos el texto si es su primer turno para darle feedback visual al jugador
            if (textoIndicadorTurno != null)
            {
                if (EsPrimerTurno())
                {
                    textoIndicadorTurno.text = "TU TURNO (Fase Inicial: Solo Básicos)";
                }
                else
                {
                    textoIndicadorTurno.text = "TU TURNO";
                }
            }

            if (btnPasarTurno != null) btnPasarTurno.interactable = true; // Activa el botón
            Debug.Log("Es tu turno. Puedes jugar.");
        }
        else
        {
            if (textoIndicadorTurno != null) textoIndicadorTurno.text = "TURNO DEL RIVAL";
            if (btnPasarTurno != null) btnPasarTurno.interactable = false; // Desactiva el botón (se vuelve gris)
            Debug.Log("Es turno del rival. Bloqueado.");
        }
    }
}