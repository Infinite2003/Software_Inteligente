using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CartaInteractiva : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Vector3 escalaOriginal;
    private bool estaSobreLaCarta = false;
    private bool estaEnTablero = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        escalaOriginal = transform.localScale;
    }
    // Comprobamos si la carta está actualmente en la Mano
    bool EstaEnMano()
    {
        return transform.parent != null && (transform.parent.name.Contains("Mano") || transform.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null);
    }

    // Al pasar el ratón, se pone al frente y crece un poco
    public void OnPointerEnter(PointerEventData eventData)
    {
        // SOLO reacciona si está en la mano o ya en el tablero
        if (!EstaEnMano() && !estaEnTablero) return;
        estaSobreLaCarta = true;

        var layout = transform.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.enabled = false;
        }
        transform.SetAsLastSibling();
        transform.localScale = escalaOriginal * 1.2f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        estaSobreLaCarta = false;
        transform.localScale = escalaOriginal;

        var layout = transform.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.enabled = true;
        }
    }

    // 2. Al hacer clic, la carta sube al centro (CartaJugada)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (EstaEnMano())
        {
            GameObject zonaJuego = GameObject.Find("CartaJugada");

            if (zonaJuego != null)
            {
                // NUEVA VALIDACIÓN: Si el contenedor ya tiene al menos 1 hijo, no hacemos nada
                if (zonaJuego.transform.childCount > 0)
                {
                    Debug.LogWarning("La zona 'CartaJugada' ya está ocupada por otra carta.");
                    return; // Salimos de la función y la carta no se mueve
                }

                // Si llegamos aquí, es que está vacío
                transform.SetParent(zonaJuego.transform);

                RectTransform rect = GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;

                estaEnTablero = true;
                estaSobreLaCarta = false;

                Debug.Log("Movida a CartaJugada");
            }
            else
            {
                Debug.LogError("No se encontró 'CartaJugada'");
            }
        }
    }

    void Update()
    {
        if (estaEnTablero && estaSobreLaCarta && Keyboard.current.yKey.wasPressedThisFrame)
        {
            GameObject cementerio = GameObject.Find("Cementerio");
            if (cementerio != null)
            {
                MoverAContenedor(cementerio.transform);
                estaEnTablero = false;
                Debug.Log("Carta enviada al cementerio");
            }
        }
    }

    // Método genérico para mover y resetear
    void MoverAContenedor(Transform nuevoPadre)
    {
        transform.SetParent(nuevoPadre);
        RectTransform rect = GetComponent<RectTransform>();

        // Reset de posición para que se centre en el nuevo contenedor
        rect.anchoredPosition = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        // Si el nuevo padre NO tiene Layout, esto asegura que se vea en el centro
        if (nuevoPadre.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() == null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
