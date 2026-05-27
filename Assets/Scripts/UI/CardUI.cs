using UnityEngine;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("Textos de la Carta")]
    [SerializeField] private TextMeshProUGUI nombreTexto;
    [SerializeField] private TextMeshProUGUI hpTexto;
    [SerializeField] private TextMeshProUGUI tipoTexto;
    [SerializeField] private TextMeshProUGUI idTexto;
    [SerializeField] private TextMeshProUGUI categoriaTexto;
    [SerializeField] private TextMeshProUGUI subCategoriaTexto;
    [SerializeField] private TextMeshProUGUI descripcionTexto;
    [SerializeField] private TextMeshProUGUI efectoTexto;
    [SerializeField] private TextMeshProUGUI costeRetiradaTexto;

    [HideInInspector] public TCGPCard cardData;

    public void SetData(TCGPCard carta)
    {
        cardData = carta;

        // Asignación directa de textos pasarle la información limpia
        if (nombreTexto != null) nombreTexto.text = carta.name;
        if (hpTexto != null) hpTexto.text = carta.hp.ToString();
        if (tipoTexto != null) tipoTexto.text = carta.type.ToString();
        if (idTexto != null) idTexto.text = carta.id;

        if (categoriaTexto != null) categoriaTexto.text = carta.category.ToString();
        if (subCategoriaTexto != null) subCategoriaTexto.text = carta.sub_category.ToString();

        if (descripcionTexto != null) descripcionTexto.text = carta.description;
        if (efectoTexto != null) efectoTexto.text = carta.effect;
        if (costeRetiradaTexto != null) costeRetiradaTexto.text = carta.retreat_cost.ToString();
    }
}