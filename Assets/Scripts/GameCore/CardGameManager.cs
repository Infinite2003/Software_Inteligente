using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardGameManager : MonoBehaviour
{
    private List<TCGPCard> pool = new List<TCGPCard>();
    private DeckBuilder deckBuilder;
    private InputAction interactiveKey;

    [SerializeField]
    private DeckPreferences prefs;
    void Start()
    {
        deckBuilder = new DeckBuilder();
        LoadCards("tcg_pocket_card_unity");

        // Intentar encontrar la acción si el InputActionAsset existe en InputSystem
        if (InputSystem.actions != null)
        {
            interactiveKey = InputSystem.actions.FindAction("Interact");
            if (interactiveKey == null)
            {
                Debug.LogWarning("No se encontró la acción 'Interact' en InputSystem.actions. Asegúrate de tenerla configurada.");
            }
        }
        else
        {
            Debug.LogWarning("InputSystem.actions es null. Asegúrate de tener un InputActionAsset activo en tu proyecto.");
        }
    }

    private void Update()
    {
        // Revisamos que interactiveKey no sea null antes de invocarlo para no romper el juego.
        if (interactiveKey != null && interactiveKey.WasReleasedThisFrame())
        {
            CreateDeck();
        }
        // Fallback rápido con teclado tradicional para que no te estanques si InputSystem no está configurado
        else if (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            CreateDeck();
        }
    }

    public List<TCGPCard> CreateDeck()
    {
        

        var deck = deckBuilder.BuildDeck(pool, 20, prefs);
        Debug.Log($"Mazo creado: {deck.Count} cartas");

        foreach( var card in deck )
            Debug.Log($"Carta: {card.name}");

        return deck;
    }

    private void LoadCards(string jsonFileName)
    {
        TextAsset json = Resources.Load<TextAsset>(jsonFileName);

        CardDatabaseRaw db = JsonUtility.FromJson<CardDatabaseRaw>(json.text);

        pool = ConvertToGameCards(db.cards);
        Debug.Log($"Se cargaron {pool.Count} cartas desde {jsonFileName}");
    }

    private List<TCGPCard> ConvertToGameCards(List<TCGPCardRaw> rawCards)
    {
        List<TCGPCard> result = new List<TCGPCard>();

        foreach (var raw in rawCards)
        {
            TCGPCard card = new TCGPCard
            {
                id = raw.id,
                name = raw.name,
                hp = raw.hp,
                retreat_cost = raw.retreat_cost,
                description = raw.description,

                category = ParseCategory(raw.category),
                sub_category = ParseStage(raw.sub_category),
                type = ParseType(raw.type),

                moves = ConvertMoves(raw.moves),

                // Map the newly added fields!
                ability = raw.ability != null ? new Ability { name = raw.ability.name, description = raw.ability.description } : null,
                weakness = raw.weakness != null ? new Weakness { type = ParseType(raw.weakness.type), value = raw.weakness.value } : null
            };

            result.Add(card);
        }

        return result;
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
                cost = m.cost
            });
        }

        return moves;
    }

    private CardCategory ParseCategory(string value)
    {
        if (string.IsNullOrEmpty(value)) return CardCategory.Pokemon;

        return value.ToLower() switch
        {
            "pokemon" => CardCategory.Pokemon,
            "trainer" => CardCategory.Trainer,
            "item" => CardCategory.Item,
            "supporter" => CardCategory.Supporter,
            _ => CardCategory.Pokemon
        };
    }

    private PokemonStage ParseStage(string value)
    {
        if (string.IsNullOrEmpty(value)) return PokemonStage.Basic;

        return value.ToLower() switch
        {
            "basic" => PokemonStage.Basic,
            "stage1" => PokemonStage.Stage1,
            "stage2" => PokemonStage.Stage2,
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
            "metálico" => PokemonType.Metalico,
            "metalico" => PokemonType.Metalico,
            "dragón" => PokemonType.Dragon,
            "dragon" => PokemonType.Dragon,
            _ => PokemonType.Incolora
        };
    }
}

[System.Serializable]
public class CardDatabaseRaw
{
    public List<TCGPCardRaw> cards;
}