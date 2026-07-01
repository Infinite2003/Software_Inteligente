using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nombreTexto;
    [SerializeField] private TextMeshProUGUI hpTexto;
    [SerializeField] private TextMeshProUGUI tipoTexto;
    [SerializeField] private RawImage cardImage;
    [SerializeField] private CardImageLoader imageLoader;
    [SerializeField] private GameObject loadingOverlay;

    public void SetData(TCGPCard carta)
    {
        nombreTexto.text = carta.name;
        hpTexto.text = "HP: " + carta.hp;
        tipoTexto.text = carta.type.ToString();

        if (loadingOverlay != null)
            loadingOverlay.SetActive(true);

        imageLoader.LoadImage(carta.image_url, cardImage, () =>
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(false);
        });
    }
}