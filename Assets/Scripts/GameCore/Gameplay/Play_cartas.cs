using System.Collections.Generic;
using UnityEngine;

public class Play_cartas : MonoBehaviour
{
    private List<TCGPCard> mano = new List<TCGPCard>();
    [SerializeField] private List<TCGPCard> miMazo;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform manoParent;
    [SerializeField] private Transform deckParent;

    void Start()
    {
        RobarManoInicial();
        MostrarMano();
    }
    void RobarManoInicial()
    {
        for (int i = 0; i < 5; i++)
        {
            if (miMazo.Count == 0) break;

            TCGPCard carta = miMazo[0];
            miMazo.RemoveAt(0);
            mano.Add(carta);
        }
        if(CardGameManager._instance.miMazo!=null)
        {
            var currentDeck = CardGameManager._instance.miMazo;
        }
    }

    void MostrarMano()
    {
        for (int i = 0; i < mano.Count; i++)
        {
            GameObject obj = Instantiate(cardPrefab, manoParent);

            obj.GetComponent<CardTablero>().SetData(mano[i]);

            RectTransform rt = obj.GetComponent<RectTransform>();

            // Mantener la escala del prefab; solo ajustar posición
            rt.anchoredPosition = new Vector2(i * 120, 0);
        }
    }
}
    