using System;
using System.Collections.Generic;
using UnityEditor.Rendering;

public class DeckBuilder
{
    private DeckPreferences preferences;

    public List<TCGPCard> BuildDeck(List<TCGPCard> pool, int targetSize, DeckPreferences prefs)
    {
        preferences = prefs;

        List<TCGPCard> filteredPool = pool.FindAll(card =>
        {
            if (card.category == CardCategory.Pokemon)
                return card.type == preferences.preferredType;

            return true;
        });

        List<TCGPCard> currentDeck = new List<TCGPCard>();

        currentDeck.AddRange(SelectCoreCards(filteredPool));

        List<TCGPCard> availableCards = new List<TCGPCard>(filteredPool);

        while (currentDeck.Count < targetSize && availableCards.Count > 0)
        {
            TCGPCard bestCard = null;
            float bestScore = float.MinValue;

            foreach (var card in availableCards)
            {
                float score = EvaluateCardContribution(card, currentDeck);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCard = card;
                }
            }

            if (bestCard != null)
            {
                currentDeck.Add(bestCard);
                availableCards.Remove(bestCard);
            }
            else break;
        }

        return currentDeck;
    }

    private List<TCGPCard> SelectCoreCards(List<TCGPCard> pool)
    {
        List<TCGPCard> core = new List<TCGPCard>();

        foreach (var card in pool)
        {
            if (card.category == CardCategory.Pokemon &&
                card.sub_category == PokemonStage.Basic &&
                card.type == preferences.preferredType)
            {
                core.Add(card);

                if (core.Count >= 3)
                    break;
            }
        }

        return core;
    }

    private float EvaluateCardContribution(TCGPCard candidate, List<TCGPCard> deck)
    {
        if (!IsValid(candidate, deck))
            return float.MinValue;

        float score = 0f;

        score += CalculateBaseValue(candidate);
        score += SynergyScore(candidate, deck) * 2f;
        score += ConsistencyScore(candidate) * 1.5f;
        score += ResourceBalanceScore(candidate, deck) * 1.5f;
        score += CurveScore(candidate);
        score -= RedundancyPenalty(candidate, deck);
        score += ApplyPlaystyle(candidate, deck);

        return score;
    }

    private float ApplyPlaystyle(TCGPCard card, List<TCGPCard> deck)
    {
        switch (preferences.playstyle)
        {
            case BattleType.Aggro: return AggroScore(card);
            case BattleType.Control: return ControlScore(card);
            case BattleType.Combo: return SynergyScore(card, deck) * 2f;
        }
        return 0f;
    }

    private float AggroScore(TCGPCard card)
    {
        float score = 0f;

        if (card.category == CardCategory.Pokemon && card.moves != null)
        {
            foreach (var move in card.moves)
            {
                if (int.TryParse(move.damage, out int dmg))
                    score += dmg * 0.3f;
            }
        }

        return score;
    }

    private float ControlScore(TCGPCard card)
    {
        if (!string.IsNullOrEmpty(card.description) &&
            card.description.Contains("discard", StringComparison.OrdinalIgnoreCase))
        {
            return 5f;
        }

        return 0f;
    }

    private float CalculateBaseValue(TCGPCard card)
    {
        float value = 0f;

        if (card.category == CardCategory.Pokemon)
        {
            value += card.hp * 0.1f;

            if (card.moves != null)
            {
                foreach (var move in card.moves)
                {
                    if (int.TryParse(move.damage, out int dmg))
                        value += dmg * 0.2f;
                }
            }
        }
        else value += 10f;

        return value;
    }

    private bool IsValid(TCGPCard candidate, List<TCGPCard> deck)
    {
        int count = deck.FindAll(c => c.name == candidate.name).Count;
        return count < 2;
    }

    private float SynergyScore(TCGPCard candidate, List<TCGPCard> deck)
    {
        float score = 0f;

        foreach (var card in deck)
        {
            if (card.category == CardCategory.Pokemon &&
                candidate.category == CardCategory.Pokemon &&
                card.type == candidate.type)
            {
                score += 2f;
            }
        }

        return score;
    }

    private float ConsistencyScore(TCGPCard card)
    {
        if (card.category == CardCategory.Supporter || card.category == CardCategory.Item)
        {
            if (!string.IsNullOrEmpty(card.description) &&
                (card.description.Contains("draw", StringComparison.OrdinalIgnoreCase) ||
                 card.description.Contains("search", StringComparison.OrdinalIgnoreCase)))
            {
                return 5f;
            }
        }

        return 0f;
    }

    private float ResourceBalanceScore(TCGPCard candidate, List<TCGPCard> deck)
    {
        int pokemon = deck.FindAll(c => c.category == CardCategory.Pokemon).Count;
        int trainer = deck.Count - pokemon;

        if (candidate.category == CardCategory.Pokemon)
            return pokemon > 12 ? -5f : 2f;
        else
            return trainer > 8 ? -5f : 2f;
    }

    private float CurveScore(TCGPCard card)
    {
        if (card.category == CardCategory.Pokemon && card.moves != null)
        {
            float avg = 0f;

            foreach (var move in card.moves)
                avg += move.cost?.Count ?? 0;

            avg /= card.moves.Count;

            if (avg <= 1.5f) return 3f;
            if (avg > 3f) return -2f;
        }

        return 0f;
    }

    private float RedundancyPenalty(TCGPCard card, List<TCGPCard> deck)
    {
        return deck.Exists(c => c.name == card.name) ? 5f : 0f;
    }
}

public class DeckPreferences
{
    public PokemonType preferredType;
    public BattleType playstyle;
}