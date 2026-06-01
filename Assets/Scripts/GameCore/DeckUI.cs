using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class DeckUI : MonoBehaviour
{
    public TCGPDeck deck;

    [SerializeField] private TMP_Text deckNameText;
    [SerializeField] private Button button;

    public void Setup(TCGPDeck deck, Action<TCGPDeck> onSelected)
    {
        deckNameText.text = deck.name;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected(deck));
    }
}
