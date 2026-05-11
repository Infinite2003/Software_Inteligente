using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardGameManager : MonoBehaviour
{
    private Dictionary<string, TCGPCard> cardDatabase;

    public List<TCGPCard> currentDeck;
    public static CardGameManager _instance;
    private List <TCGPCard> pool = new List<TCGPCard>();
    private DeckBuilder deckBuilder;
    public InputAction interactiveKey;

    //Accesible desde cualquier script
    public List<TCGPCard> miMazo;

    [SerializeField]
    private DeckPreferences deckPreferences;
    // Es buena práctica exponer el singleton mediante una propiedad pública si planeas accederlo desde otros scripts.
    public static CardGameManager Instance => _instance;

    private void Awake()
    {
        if(_instance == null)
        {  
            _instance = this; 
            // Opcional: si quieres que el Game Manager persista entre escenas.
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Habilitar la acción del Input System si fue asignada desde el inspector
        if (interactiveKey != null)
        {
            interactiveKey.Enable();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
  
    void Start()
    {
        deckBuilder = new DeckBuilder();
        LoadCards("tcg_pocket_card_unity");
    }

    public void CreateDeck()
    {
        // Llamada ajustada al método que implementamos en DeckBuilder, usando pool y un targetSize
        if(miMazo != null)
        {
            miMazo.Clear();
        }

        miMazo = deckBuilder.BuildDeck(pool, 20, deckPreferences);
        Debug.Log($"Mazo creado con {miMazo.Count} cartas.");

    }

    private void Update()
    {
        // Revisamos que interactiveKey no sea null y esté activa antes de invocarla.
        if (interactiveKey != null && interactiveKey.triggered)
        {
            CreateDeck();
            foreach (var card in miMazo)
            {
                Debug.Log($"Carta en el mazo: {card.name}");
            }
        }
        // Fallback rápido con teclado tradicional para que no te estanques si InputSystem no está configurado
        else if (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            CreateDeck();
            foreach (var card in miMazo)
            {
                Debug.Log($"Carta en el mazo: {card.name}");
            }
        }
        else if (Keyboard.current != null && Keyboard.current.enterKey.wasReleasedThisFrame)
        {
            // Busca una carta en el 'pool' (toda la base de datos descargada del JSON)
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
        {
            interactiveKey.Disable();
        }
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
        {
            cardDatabase[card.id] = card;
        }

        Debug.Log($"Se cargaron {pool.Count} cartas desde {jsonFileName}");
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
                name = raw.name,
                hp = raw.hp,
                retreat_cost = raw.retreat_cost,
                description = raw.description,
                effect = raw.effect, // Mapeado el efecto del trainer

                category = ParseCategory(raw.category),

                // Mapear Stage, tu JSON muestra la key "stage" en vez de sub_category
                sub_category = ParseStage(string.IsNullOrEmpty(raw.stage) ? raw.sub_category : raw.stage),

                type = ParseType(raw.type),

                moves = ConvertMoves(raw.moves),

                weakness = raw.weakness != null ? new Weakness { type = ParseType(raw.weakness.type), value = raw.weakness.value } : null,
                ability = ConvertAbilities(raw.ability)
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
            "pokémon" => CardCategory.Pokemon,   // ← con tilde
            "trainer" => CardCategory.Trainer,
            "entrenador" => CardCategory.Trainer,   // ← español
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