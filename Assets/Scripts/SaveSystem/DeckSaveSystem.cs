using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DeckSaveSystem
{
    private static string DeckFolder => Path.Combine(Application.persistentDataPath, "Decks");

    public static void SaveDeck(LightweightDeck deck)
    {
        Directory.CreateDirectory(DeckFolder);

        string json = JsonUtility.ToJson(deck, true);
        string path = Path.Combine(DeckFolder, deck.name + ".json");

        File.WriteAllText(path, json);
        
        Debug.Log("Deck saved to: " + path);
    }

    public static List<LightweightDeck> LoadAllDecks()
    {
        Directory.CreateDirectory(DeckFolder);

        string[] files = Directory.GetFiles(DeckFolder, "*.json");
        List<LightweightDeck> decks = new List<LightweightDeck>();

        foreach(string file in files)
        {
            string json = File.ReadAllText(file);
            LightweightDeck deck = JsonUtility.FromJson<LightweightDeck>(json);

            decks.Add(deck);
        }

        return decks;
    }

    public static string GetUniqueDeckName(string baseName)
    {
        string folder = DeckFolder; 

        Directory.CreateDirectory(folder);
        string finalName = baseName;

        int counter = 1;

        while (File.Exists(Path.Combine(folder, finalName + ".json")))
        {
            finalName = $"{baseName} ({counter})";
            counter++;
        }

        return finalName;
    }
}
