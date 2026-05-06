using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DeckListManager : MonoBehaviour
{
    public TCGPDeck selectedDeck;
    public List<TCGPDeck> deckList;

    private void Start()
    {
        deckList = DeckSaveSystem.LoadAllDecks();
    }

    public void SaveDeck(TCGPDeck delectedDeck)
    {
        if (selectedDeck != null && !inDeckList(selectedDeck))
            DeckSaveSystem.SaveDeck(selectedDeck);
    }

    public bool inDeckList(TCGPDeck delectedDeck)
    {
        foreach(TCGPDeck deck in deckList)
        {
            if(deck ==  delectedDeck)
                return true;
        }
        return false;
    }
}
