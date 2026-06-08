using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardGameManager : MonoBehaviour
{
    public Dictionary<string, TCGPCard> cardDatabase;

    public List<TCGPCard> currentDeck;
    public static CardGameManager _instance;
    private List<TCGPCard> pool = new List<TCGPCard>();
    private DeckBuilder deckBuilder;
    public InputAction interactiveKey;

    // Accesible desde cualquier script
    public List<TCGPCard> miMazo;

    [SerializeField]
    private DeckPreferences deckPreferences;

    public static CardGameManager Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (interactiveKey != null)
            interactiveKey.Enable();
    }

    void Start()
    {
        deckBuilder = new DeckBuilder();
        LoadCards("tcg_pocket_card_unity");

        // Simulación: elegir la primera EX del tipo preferido como anchor
        List<TCGPCard> exCards = GetEXCardsByType(deckPreferences.preferredType);

        if (exCards != null && exCards.Count > 0)
        {
            deckPreferences.anchorCard = exCards[0];
            Debug.Log($"Anchor seleccionado: {deckPreferences.anchorCard.name}");
        }
        else
        {
            deckPreferences.anchorCard = null;
            Debug.LogWarning("No se encontraron cartas EX para el tipo preferido. Se construirá sin anchor.");
        }
    }

    public void CreateDeck()
    {
        if (miMazo != null)
            miMazo.Clear();

        miMazo = deckBuilder.BuildDeck(pool, 20, deckPreferences);
        Debug.Log($"Mazo creado con {miMazo.Count} cartas.");
    }

    private void Update()
    {
        if (interactiveKey != null && interactiveKey.triggered)
        {
            CreateDeck();
            foreach (var card in miMazo)
                Debug.Log($"Carta en el mazo: {card.name}");
        }
        else if (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            CreateDeck();
            foreach (var card in miMazo)
                Debug.Log($"Carta en el mazo: {card.name}");
        }
        else if (Keyboard.current != null && Keyboard.current.enterKey.wasReleasedThisFrame)
        {
            TCGPCard kogaCard = pool.Find(c => c.name.Equals("Koga", System.StringComparison.OrdinalIgnoreCase));

            if (kogaCard != null)
            {
                Debug.Log($"Carta encontrada con Éxito: {kogaCard.name}");
                Debug.Log($"   | Categoría: {kogaCard.category}");
                Debug.Log($"   | Texto Efecto/Descripción: {(string.IsNullOrEmpty(kogaCard.effect) ? kogaCard.description : kogaCard.effect)}");
            }
            else
            {
                Debug.LogWarning("No se encontró la carta 'Koga' en la base de datos (Pool). Asegúrate de que el JSON la tenga.");
            }
        }
    }

    private void OnDestroy()
    {
        if (interactiveKey != null)
            interactiveKey.Disable();
    }

    private void LoadCards(string jsonFileName)
    {
        TextAsset json = Resources.Load<TextAsset>(jsonFileName);

        if (json == null)
        {
            Debug.LogError($"Error: No se encontró el archivo JSON '{jsonFileName}' en la carpeta Resources.");
            return;
        }

        CardDatabaseRaw db = JsonUtility.FromJson<CardDatabaseRaw>(json.text);

        if (db == null || db.cards == null)
        {
            Debug.LogError("Error: El JSON no pudo ser deserializado o no contiene cartas.");
            return;
        }

        pool = ConvertToGameCards(db.cards);

        cardDatabase = new Dictionary<string, TCGPCard>();

        foreach (var card in pool)
            cardDatabase[card.id] = card;

        Debug.Log($"Se cargaron exitosamente {pool.Count} cartas desde {jsonFileName}");
    }

    private List<TCGPCard> ConvertToGameCards(List<TCGPCardRaw> rawCards)
    {
        List<TCGPCard> result = new List<TCGPCard>();

        foreach (var raw in rawCards)
        {
            TCGPCard card = new TCGPCard
            {
                id = raw.id,
                set_number = raw.set_number,
                name = raw.name,
                hp = raw.hp,
                retreat_cost = raw.retreat_cost,
                description = raw.description,
                effect = raw.effect,
                rarity = raw.rarity,
                evolve_from = raw.evolve_from,

                // FIX 2: ParseCategory recibe trainer_type para mapear Supporter/Item correctamente
                category = ParseCategory(raw.category, raw.trainer_type),

                // FIX 1 & 5: ParseStage directo desde raw.stage (sub_category eliminado del Raw)
                sub_category = ParseStage(raw.stage),

                type = ParseType(raw.type),

                // FIX 3: ConvertMoves ya mapea move.effect
                moves = ConvertMoves(raw.moves),

                weakness = raw.weakness != null
                               ? new Weakness { type = ParseType(raw.weakness.type), value = raw.weakness.value }
                               : null,

                ability = ConvertAbilities(raw.ability),

                // FIX 4: packs ahora se transfieren
                packs = ConvertPacks(raw.packs)
            };

            result.Add(card);
        }

        return result;
    }

    private List<Ability> ConvertAbilities(List<AbilityRaw> rawAbilities)
    {
        List<Ability> abilities = new List<Ability>();
        if (rawAbilities == null) return abilities;

        foreach (var ab in rawAbilities)
        {
            abilities.Add(new Ability
            {
                type = ab.type,
                name = ab.name,
                effect = ab.effect
            });
        }

        return abilities;
    }

    private List<Move> ConvertMoves(List<MoveRaw> rawMoves)
    {
        List<Move> moves = new List<Move>();
        if (rawMoves == null) return moves;

        foreach (var m in rawMoves)
        {
            moves.Add(new Move
            {
                name = m.name,
                damage = m.damage,
                cost = m.cost,
                effect = m.effect  // FIX 3: efecto del movimiento mapeado
            });
        }

        return moves;
    }

    private List<Pack> ConvertPacks(List<PackRaw> rawPacks)
    {
        List<Pack> packs = new List<Pack>();
        if (rawPacks == null) return packs;

        foreach (var p in rawPacks)
        {
            packs.Add(new Pack
            {
                id = p.id,
                name = p.name
            });
        }

        return packs;
    }

    // FIX 2: Recibe trainer_type para distinguir Supporter e Item
    private CardCategory ParseCategory(string category, string trainerType)
    {
        if (string.IsNullOrEmpty(category)) return CardCategory.Pokemon;

        string cat = category.ToLower();

        if (cat is "pokemon" or "pokémon")
            return CardCategory.Pokemon;

        if (cat is "trainer" or "entrenador")
        {
            return (trainerType ?? "").ToLower() switch
            {
                "item" => CardCategory.Item,
                "objeto" => CardCategory.Item,
                "supporter" => CardCategory.Supporter,
                "partidario" => CardCategory.Supporter,
                _ => CardCategory.Trainer
            };
        }

        return CardCategory.Pokemon;
    }

    // FIX 1: Maneja los valores en español del JSON
    private PokemonStage ParseStage(string value)
    {
        if (string.IsNullOrEmpty(value)) return PokemonStage.Basic;

        return value.ToLower() switch
        {
            "basic" => PokemonStage.Basic,
            "básico" => PokemonStage.Basic,
            "stage 1" => PokemonStage.Stage1,
            "stage1" => PokemonStage.Stage1,
            "fase 1" => PokemonStage.Stage1,
            "stage 2" => PokemonStage.Stage2,
            "stage2" => PokemonStage.Stage2,
            "fase 2" => PokemonStage.Stage2,
            _ => PokemonStage.Basic
        };
    }

    private PokemonType ParseType(string value)
    {
        if (string.IsNullOrEmpty(value)) return PokemonType.Incolora;

        return value.ToLower() switch
        {
            "planta" => PokemonType.Planta,
            "fuego" => PokemonType.Fuego,
            "agua" => PokemonType.Agua,
            "rayo" => PokemonType.Rayo,
            "psíquico" => PokemonType.Psiquico,
            "psiquico" => PokemonType.Psiquico,
            "lucha" => PokemonType.Lucha,
            "oscuro" => PokemonType.Oscuro,
            "oscura" => PokemonType.Oscuro,  // variante femenina que usa el JSON en weakness
            "metálico" => PokemonType.Metalico,
            "metalico" => PokemonType.Metalico,
            "dragón" => PokemonType.Dragon,
            "dragon" => PokemonType.Dragon,
            _ => PokemonType.Incolora
        };
    }

    public List<TCGPCard> GetMyDeck()
    {
        if (miMazo == null || miMazo.Count == 0)
        {
            Debug.LogWarning("El mazo está vacío o no ha sido creado aún. Asegúrate de llamar a CreateDeck() antes de obtener el mazo.");
            return null;
        }
        return miMazo;
    }
    public List<TCGPCard> GetEXCardsByType(PokemonType type)
    {
        return pool.FindAll(card =>
         card.type == type &&
         !string.IsNullOrEmpty(card.name) &&
         card.name.EndsWith(" ex", StringComparison.OrdinalIgnoreCase));
    }
}

[System.Serializable]
public class CardDatabaseRaw
{
    public List<TCGPCardRaw> cards;
}