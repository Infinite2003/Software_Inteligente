using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public static void SaveDeck(TCGPDeck deck)
    {
        string json = JsonUtility.ToJson(deck, true);
        string path = Path.Combine(Application.persistentDataPath, deck.name + "json");

        PlayerPrefs.SetString("SavedDeck", json);
        PlayerPrefs.Save();
        Debug.Log("Deck saved successfully.");
    }

    public static TCGPDeck LoadDeck()
    {
        if (PlayerPrefs.HasKey("SavedDeck"))
        {
            string json = PlayerPrefs.GetString("SavedDeck");
            TCGPDeck deck = JsonUtility.FromJson<TCGPDeck>(json);
            Debug.Log("Deck loaded successfully.");
            return deck;
        }
        else
        {
            Debug.LogWarning("No saved deck found.");
            return null;
        }
    }

}
