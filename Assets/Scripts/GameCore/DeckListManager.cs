using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DeckListManager : MonoBehaviour
{
    public List<LightweightDeck> lightweightDecks;
    public List<TCGPDeck> deckList;

    private void Start()
    {
        lightweightDecks = DeckSaveSystem.LoadAllDecks();
        deckList = new List<TCGPDeck>();
        rebuildDecks();
    }

    public void SaveDeck(string deckName)
    {
        LightweightDeck saveDeck = new LightweightDeck();

        saveDeck.name = "MyDeck";
        saveDeck.cardIDs = new List<string>();

        foreach (var card in CardGameManager._instance.miMazo)
        {
            saveDeck.cardIDs.Add(card.id);
        }

        DeckSaveSystem.SaveDeck(saveDeck);
    }

    private void rebuildDecks()
    {
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
}
