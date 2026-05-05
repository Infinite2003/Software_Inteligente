using UnityEngine;
using UnityEngine.EventSystems;

public class CartaInteractiva : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        transform.SetAsLastSibling();
        transform.localScale = escalaOriginal * 1.1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        estaSobreLaCarta = false;
        transform.localScale = escalaOriginal;
    }

    // 2. Al hacer clic, la carta sube al centro (CartaJugada)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (EstaEnMano())
        {
            // Buscamos el contenedor por nombre exacto
            GameObject zonaJuego = GameObject.Find("CartaJugada");

            if (zonaJuego != null)
            {
                // 1. Cambiamos el padre
                transform.SetParent(zonaJuego.transform);

                // 2. IMPORTANTE: Resetear el RectTransform manualmente
                RectTransform rect = GetComponent<RectTransform>();

                // Esto obliga a la carta a ir al centro exacto del nuevo padre
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
                Debug.LogError("No se encontró el objeto 'CartaJugada'. Revisa el nombre en la jerarquía.");
            }
        }
    }

    void Update()
    {
        // 3. Solo si la carta está en el centro y presionas Y, se va al cementerio
        if (estaEnTablero && estaSobreLaCarta && Input.GetKeyDown(KeyCode.Y))
        {
            GameObject cementerio = GameObject.Find("Cementerio");
            if (cementerio != null)
            {
                MoverAContenedor(cementerio.transform);
                estaEnTablero = false; // Ya no está en el tablero, está descartada
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
