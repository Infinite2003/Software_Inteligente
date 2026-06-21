using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class DeckUI : MonoBehaviour
{
    public TCGPDeck deck;

    [SerializeField] private TMP_Text deckNameText;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;

    private Color selectedColor = Color.green;
    private Color deselectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    public void Setup(TCGPDeck deck, Action<TCGPDeck> onSelected)
    {
        this.deck = deck;
        deckNameText.text = deck.name;

        SetSelected(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected(deck));
    }

    public void SetSelected(bool selected)
    {
        if (buttonImage != null)
            buttonImage.color = selected ? selectedColor : deselectedColor;
    }
}