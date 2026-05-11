using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DeckListManager : MonoBehaviour
{
    public List<LightweightDeck> deckList;

    private void Start()
    {
        deckList = DeckSaveSystem.LoadAllDecks();
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

    
}
