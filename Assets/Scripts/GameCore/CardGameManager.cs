using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;

public class CardGameManager : MonoBehaviour
{
    private static CardGameManager _instance;
    private List <TCGPCard> pool = new List<TCGPCard>();
    private DeckBuilder deckBuilder;

    // Es buena práctica exponer el singleton mediante una propiedad pública si planeas accederlo desde otros scripts.
    public static CardGameManager Instance => _instance;

    private void Awake()
    {
        if(_instance == null)
        {  
            _instance = this; 
            // Opcional: si quieres que el Game Manager persista entre escenas.
            // DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        deckBuilder = new DeckBuilder();

        // Cargar cartas desde json suponiendo que el archivo se llame "cards_data.json" en Assets/Resources/
        LoadCards("cards_data");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            CreateDeck();
        }
    }

    public void CreateDeck()
    {
        // Llamada ajustada al método que implementamos en DeckBuilder, usando pool y un targetSize
        var miMazo = deckBuilder.BuildDeck(pool, 20);
        Debug.Log($"Mazo creado con {miMazo.Count} cartas.");
    }

    private void LoadCards(string jsonFileName)
    {
        // 1. Cargamos el archivo JSON desde la carpeta Resources de Unity
        // Es necesario que el json esté dentro de una carpeta llamada "Resources" (ej: Assets/Resources/cards_data.json)
        TextAsset jsonTextFile = Resources.Load<TextAsset>(jsonFileName);

        if (jsonTextFile != null)
        {
            // Unity JsonUtility requiere que los arrays JSON estén envueltos en un objeto.
            // Formato esperado de tu JSON: { "cards": [ { "id": 1, ... }, { "id": 2, ... } ] }
            CardDatabase database = JsonUtility.FromJson<CardDatabase>(jsonTextFile.text);

            if (database != null && database.cards != null)
            {
                pool = database.cards;
                Debug.Log($"Se cargaron {pool.Count} cartas exitosamente desde {jsonFileName}");
            }
            else
            {
                Debug.LogError("El JSON fue leído, pero la estructura no coincide con CardDatabase.");
            }
        }
        else
        {
            Debug.LogError($"No se pudo cargar el archivo JSON: {jsonFileName}");
        }
    }
}

// Wrapper necesario para que Unity's JsonUtility pueda leer listas en la raíz del objeto JSON
[System.Serializable]
public class CardDatabase
{
    public List<TCGPCard> cards;
}
