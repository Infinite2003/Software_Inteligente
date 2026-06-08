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

    [SerializeField] private Transform generatedCardContentParent; //Padre de las cartas generadas, que se ven en la ventana que se abre
    [SerializeField] private Transform cardContentParent; //Padre de las cartas del mazo seleccionado, en la pantalla inicial
    [SerializeField] private Transform deckContentParent;

    [SerializeField] private GameObject cardItemPrefab;
    [SerializeField] private GameObject deckItemPrefab;


    private void Start()
    {
        lightweightDecks = DeckSaveSystem.LoadAllDecks();
        deckList = new List<TCGPDeck>();
        rebuildDecks();
        PopulateDeckView(deckList, deckContentParent);
    }

    private void rebuildDecks()
    {
        Debug.Log("Cantidad de mazos guardados: " + lightweightDecks.Count);
        if (lightweightDecks == null || CardGameManager._instance.cardDatabase == null)
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
        PopulateCardView(CardGameManager._instance.miMazo, generatedCardContentParent);
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

        deckList.Add(BuildRuntimeDecks(savedDeck));
        PopulateDeckView(deckList, deckContentParent);
        CloseGeneratedDeckWindow();
    }

    private string SanitizeFileName(string fileName)
    {
        foreach(char c in System.IO.Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

    public void PopulateCardView(List<TCGPCard> cards, Transform contentParent)
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

    public void PopulateDeckView(List<TCGPDeck> decks, Transform contentParent)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (TCGPDeck deck in decks)
        {
            GameObject obj = Instantiate(deckItemPrefab, contentParent);

            DeckUI deckui = obj.GetComponent<DeckUI>();
            deckui.Setup(deck, SelectDeck);
            
        }
    }

    public void SelectDeck(TCGPDeck selectedDeck)
    {
        PopulateCardView(selectedDeck.cards, cardContentParent);
    }

}
