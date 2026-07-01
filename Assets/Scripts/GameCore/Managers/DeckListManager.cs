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

    [SerializeField] private Transform generatedCardContentParent;
    [SerializeField] private Transform cardContentParent; 
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
            new List<string>(
                Enum.GetNames(typeof(BattleType))));

        typeDropdown.ClearOptions();
        typeDropdown.AddOptions(
            new List<string>(
                Enum.GetNames(typeof(PokemonType))));

        OnTypeChanged(typeDropdown.value);
        SetDropdownFontSize(fightStyleDropdown, 40f);
        SetDropdownFontSize(typeDropdown, 40f);
        SetDropdownFontSize(exDropDown, 40f);
    }

    private void rebuildDecks()
    {
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
        AudioManager._instance.PlaySFX("AButton");
        CardGameManager._instance.CreateDeck();

        deckGenerationWindow.SetActive(true);
        PopulateCardView(CardGameManager._instance.miMazo, generatedCardContentParent);
    }

    public void CloseGeneratedDeckWindow()
    {
        AudioManager._instance.PlaySFX("ReturnButton");
        deckGenerationWindow.SetActive(false);
    }

    public void SaveCurrentDeck()
    {
        string deckName = deckNameInput.text;

        if (string.IsNullOrWhiteSpace(deckName))
        {
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

        AudioManager._instance.PlaySFX("AButton");
        DeckSaveSystem.SaveDeck(savedDeck);


        deckList.Add(BuildRuntimeDecks(savedDeck));
        PopulateDeckView(deckList, deckContentParent);

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
        AudioManager._instance.PlaySFX("DeckSelected");

        PopulateCardView(selectedDeck.cards, cardContentParent);
    }


    public void OnFightStyleChanged(int index)
    {
        BattleType selectedBattleType = (BattleType)index;
        CardGameManager._instance.deckPreferences.playstyle = selectedBattleType;


    }

    public void OnTypeChanged(int index)
    {
        PokemonType selectedType = (PokemonType)index;
        CardGameManager._instance.deckPreferences.preferredType = selectedType;
        CardGameManager._instance.deckPreferences.anchorCard = null;

        exDropDown.ClearOptions();

        List<TCGPCard> exCards = CardGameManager._instance.GetEXCardsByType(selectedType);

        if (exCards.Count == 0)
        {
            exDropDown.AddOptions(new List<string> { "No hay cartas EX" });
            exDropDown.interactable = false;
            CardGameManager._instance.deckPreferences.anchorCard = null;
        }
        else
        {
            exDropDown.interactable = true;
            List<string> options = new List<string> { "Ninguno" };
            options.AddRange(exCards.Select(card => card.name).Distinct());
            exDropDown.AddOptions(options);
            exDropDown.value = 0;
        }

        exDropDown.RefreshShownValue();
    }

    public void OnExChanged(int index)
    {
        if (index == 0)
        {
            CardGameManager._instance.deckPreferences.anchorCard = null;
            return;
        }

        List<TCGPCard> exCards = CardGameManager._instance.GetEXCardsByType((PokemonType)typeDropdown.value);
        if (exCards == null || exCards.Count == 0)
        {
            CardGameManager._instance.deckPreferences.anchorCard = null;
            return;
        }

        var distinctNames = exCards.Select(card => card.name).Distinct().ToList();
        int adjustedIndex = index - 1;

        if (adjustedIndex >= distinctNames.Count) return;

        string selectedName = distinctNames[adjustedIndex];
        CardGameManager._instance.deckPreferences.anchorCard = exCards.FirstOrDefault(c => c.name == selectedName) ?? exCards[0];

        if (selectedName.Equals("Pikachu ex", StringComparison.OrdinalIgnoreCase))
            AudioManager._instance.PlaySFX("Pikachu");
    }
    public void ClearAllDecks()
    {
        DeckSaveSystem.DeleteAllDecks();
        AudioManager._instance.PlaySFX("ReturnButton");

        lightweightDecks.Clear();
        deckList.Clear();

        PopulateDeckView(deckList, deckContentParent);
        PopulateCardView(new List<TCGPCard>(), cardContentParent);

        Debug.Log("Todos los mazos han sido eliminados.");
    }

    private void SetDropdownFontSize(TMP_Dropdown dropdown, float fontSize, float itemHeight = 50f)
    {
        dropdown.itemText.fontSize = fontSize;

        RectTransform itemRect = dropdown.itemText.transform.parent.GetComponent<RectTransform>();
        if (itemRect != null)
            itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemHeight);
    }   
}
