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

    public void SetData(TCGPCard carta)
    {
        nombreTexto.text = carta.name;
        hpTexto.text = "HP: " + carta.hp;
        tipoTexto.text = carta.type.ToString();

        imageLoader.LoadImage(carta.image_url, cardImage);
    }
}