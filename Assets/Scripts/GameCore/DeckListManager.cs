using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class DeckListManager : MonoBehaviour
{
    public List<LightweightDeck> lightweightDecks;
    public List<TCGPDeck> deckList;

    [SerializeField] private TMP_InputField deckNameInput;
    [SerializeField] private GameObject deckGenerationWindow;

    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject cardItemPrefab;


    private void Start()
    {
        lightweightDecks = DeckSaveSystem.LoadAllDecks();
        deckList = new List<TCGPDeck>();
        rebuildDecks();
    }

    private void rebuildDecks()
    {
        if(lightweightDecks == null || CardGameManager._instance.cardDatabase == null)
            return;

        foreach(LightweightDeck deck in lightweightDecks)
        {
            deckList.Add(BuildRuntimeDecks(deck));
        }
    }

    private TCGPDeck BuildRuntimeDecks(LightweightDeck savedDeck)
    {
        TCGPDeck deck = new TCGPDeck();
        deck.name = savedDeck.name;
        deck.cards = new List<TCGPCard>();

        foreach(string id in savedDeck.cardIDs)
        {
            if(CardGameManager._instance.cardDatabase.TryGetValue(id, out TCGPCard card))
            {
                deck.cards.Add(card);
            }
        }

        return deck;
    }

    public void GenerateDeck()
    {
        CardGameManager._instance.CreateDeck();

        deckGenerationWindow.SetActive(true);
        PopulateView(CardGameManager._instance.miMazo);
    }

    public void CloseGeneratedDeckWindow()
    {
        deckGenerationWindow.SetActive(false);
    }

    public void SaveCurrentDeck()
    {
        string deckName = deckNameInput.text;

        if (string.IsNullOrWhiteSpace(deckName))
        {
            Debug.LogWarning("Deck name is empty");
            return;
        }

        deckName = SanitizeFileName(deckName);

        deckName = DeckSaveSystem.GetUniqueDeckName(deckName);

        LightweightDeck savedDeck = new LightweightDeck();
        
        savedDeck.name = deckName;
        savedDeck.cardIDs = new List<string>();

        foreach(var card in CardGameManager._instance.miMazo)
        {
            savedDeck.cardIDs.Add(card.id);
        }

        DeckSaveSystem.SaveDeck(savedDeck);

        Debug.Log("Deck saved");
    }

    private string SanitizeFileName(string fileName)
    {
        foreach(char c in System.IO.Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

    public void PopulateView(List<TCGPCard> cards)
    {
        foreach(Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (TCGPCard card in cards)
        {
            GameObject obj = Instantiate(cardItemPrefab, contentParent);

            CardUI cardui = obj.GetComponent<CardUI>();
            cardui.SetData(card);
        }
    }
}
