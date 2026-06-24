using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [SerializeField] private TMP_Dropdown fightStyleDropdown;
    [SerializeField] private TMP_Dropdown typeDropdown;
    [SerializeField] private TMP_Dropdown exDropDown;

    private DeckUI selectedDeckUI;


    private void Start()
    {
        lightweightDecks = DeckSaveSystem.LoadAllDecks();
        deckList = new List<TCGPDeck>();
        rebuildDecks();
        PopulateDeckView(deckList, deckContentParent);

        fightStyleDropdown.onValueChanged.AddListener(OnFightStyleChanged);
        typeDropdown.onValueChanged.AddListener(OnTypeChanged);
        exDropDown.onValueChanged.AddListener(OnExChanged);

        fightStyleDropdown.ClearOptions();

        fightStyleDropdown.AddOptions(
            new System.Collections.Generic.List<string>(
                Enum.GetNames(typeof(BattleType))));

        typeDropdown.ClearOptions();
        typeDropdown.AddOptions(
            new System.Collections.Generic.List<string>(
                Enum.GetNames(typeof(PokemonType))));

        OnTypeChanged(typeDropdown.value);
        SetDropdownFontSize(fightStyleDropdown, 40f);
        SetDropdownFontSize(typeDropdown, 40f);
        SetDropdownFontSize(exDropDown, 40f);
    }

    private void rebuildDecks()
    {
        Debug.Log("Cantidad de mazos guardados: " + lightweightDecks.Count);
        if (lightweightDecks == null || CardGameManager._instance.cardDatabase == null)
            return;

        foreach (LightweightDeck deck in lightweightDecks)
        {
            deckList.Add(BuildRuntimeDecks(deck));
        }
    }

    private TCGPDeck BuildRuntimeDecks(LightweightDeck savedDeck)
    {
        TCGPDeck deck = new TCGPDeck();
        deck.name = savedDeck.name;
        deck.cards = new List<TCGPCard>();

        foreach (string id in savedDeck.cardIDs)
        {
            if (CardGameManager._instance.cardDatabase.TryGetValue(id, out TCGPCard card))
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

        foreach (var card in CardGameManager._instance.miMazo)
        {
            savedDeck.cardIDs.Add(card.id);
        }

        DeckSaveSystem.SaveDeck(savedDeck);

        Debug.Log("Deck saved");

        deckList.Add(BuildRuntimeDecks(savedDeck));
        PopulateDeckView(deckList, deckContentParent);

        // Limpiamos la entrada para el próximo mazo
        deckNameInput.text = "";

        CloseGeneratedDeckWindow();
    }

    private string SanitizeFileName(string fileName)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

    public void PopulateCardView(List<TCGPCard> cards, Transform contentParent)
    {
        foreach (Transform child in contentParent)
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
        if (selectedDeckUI != null)
            selectedDeckUI.SetSelected(false);

        foreach (Transform child in deckContentParent)
        {
            DeckUI deckUI = child.GetComponent<DeckUI>();
            if (deckUI != null && deckUI.deck == selectedDeck)
            {
                deckUI.SetSelected(true);
                selectedDeckUI = deckUI;
                break;
            }
        }

        CardGameManager._instance.currentDeck = selectedDeck.cards;

        PopulateCardView(selectedDeck.cards, cardContentParent);
    }

    //Seleccion del tipo de juego y carta

    public void OnFightStyleChanged(int index)
    {
        BattleType selectedBattleType = (BattleType)index;
        CardGameManager._instance.deckPreferences.playstyle = selectedBattleType;

        Debug.Log("Battle Type: " + CardGameManager._instance.deckPreferences.playstyle);

    }

    public void OnTypeChanged(int index)
    {
        PokemonType selectedType = (PokemonType)index;
        CardGameManager._instance.deckPreferences.preferredType = selectedType;

        CardGameManager._instance.deckPreferences.anchorCard = null;

        exDropDown.ClearOptions();

        List<TCGPCard> exCards = CardGameManager._instance.GetEXCardsByType(selectedType);
        exDropDown.AddOptions(exCards.Select(card => card.name).Distinct().ToList());
        exDropDown.RefreshShownValue();

        if (exCards.Count > 0)
        {
            CardGameManager._instance.deckPreferences.anchorCard = exCards[0];
            Debug.Log("Anchor por defecto: " + exCards[0].name);
        }
        else
        {
            Debug.LogWarning("No hay cartas EX para el tipo: " + selectedType);
        }

        Debug.Log("Tipo seleccionado: " + selectedType);
    }

    public void OnExChanged(int index)
    {
        List<TCGPCard> exCards = CardGameManager._instance.GetEXCardsByType((PokemonType)typeDropdown.value);
        if (exCards == null || exCards.Count == 0)
        {
            CardGameManager._instance.deckPreferences.anchorCard = null;
            Debug.LogWarning("No hay cartas EX disponibles para este tipo.");
            return;
        }

        // Obtener la lista única de nombres en el mismo orden del dropdown
        var distinctNames = exCards.Select(card => card.name).Distinct().ToList();
        if (index >= distinctNames.Count) return;

        string selectedName = distinctNames[index];
        // Encontrar la primera carta de la DB que coincida con este nombre
        CardGameManager._instance.deckPreferences.anchorCard = exCards.FirstOrDefault(c => c.name == selectedName) ?? exCards[0];

        Debug.Log("EX seleccionado: " + CardGameManager._instance.deckPreferences.anchorCard.name);
    }
    public void ClearAllDecks()
    {
        DeckSaveSystem.DeleteAllDecks();

        lightweightDecks.Clear();
        deckList.Clear();

        PopulateDeckView(deckList, deckContentParent);
        PopulateCardView(new List<TCGPCard>(), cardContentParent);

        Debug.Log("Todos los mazos han sido eliminados.");
    }

    private void SetDropdownFontSize(TMP_Dropdown dropdown, float fontSize, float itemHeight = 50f)
    {
        dropdown.captionText.fontSize = fontSize;
        dropdown.itemText.fontSize = fontSize;

        // Ajustar altura de cada item
        RectTransform itemRect = dropdown.itemText.transform.parent.GetComponent<RectTransform>();
        if (itemRect != null)
            itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemHeight);
    }   
}
