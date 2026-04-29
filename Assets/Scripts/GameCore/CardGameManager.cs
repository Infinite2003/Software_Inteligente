using UnityEngine;
using System.Collections.Generic;

public class CardGameManager : MonoBehaviour
{
    private List<TCGPCard> pool = new List<TCGPCard>();
    private DeckBuilder deckBuilder;

    void Start()
    {
        deckBuilder = new DeckBuilder();
        LoadCards("tcg_pocket_card_unity");
    }

    public List<TCGPCard> CreateDeck()
    {
        DeckPreferences prefs = new DeckPreferences
        {
            preferredType = PokemonType.Agua,
            playstyle = BattleType.Aggro
        };

        var deck = deckBuilder.BuildDeck(pool, 20, prefs);
        Debug.Log($"Mazo creado: {deck.Count} cartas");
        return deck;
    }

    private void LoadCards(string jsonFileName)
    {
        TextAsset json = Resources.Load<TextAsset>(jsonFileName);

        CardDatabaseRaw db = JsonUtility.FromJson<CardDatabaseRaw>(json.text);

        pool = ConvertToGameCards(db.cards);
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

                moves = ConvertMoves(raw.moves)
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
        return value.ToLower() switch
        {
            "grass" => PokemonType.Planta,
            "fire" => PokemonType.Fuego,
            "water" => PokemonType.Agua,
            "electric" => PokemonType.Rayo,
            "psychic" => PokemonType.Psiquico,
            "fighting" => PokemonType.Lucha,
            "dark" => PokemonType.Oscuro,
            "metal" => PokemonType.Metalico,
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