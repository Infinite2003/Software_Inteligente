using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nombreTexto;
    [SerializeField] private TextMeshProUGUI hpTexto;
    [SerializeField] private TextMeshProUGUI tipoTexto;

    public void SetData(TCGPCard carta)
    {
        nombreTexto.text = carta.name;
        hpTexto.text = "HP: " + carta.hp;
        tipoTexto.text = carta.type;
    }
}
