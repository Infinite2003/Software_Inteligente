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

        // 1. Base mínima
        score += CalculateBaseValue(candidate) * 0.5f;

        // 2. ESTRATEGIA (principal)
        score += EvaluateByPlaystyle(candidate, deck) * 2.5f;
        score += SupportNeedScore(candidate, deck) * 2f;

        // 3. Ajustes secundarios
        score += SynergyScore(candidate, deck) * 1.5f;
        score += ConsistencyScore(candidate) * 1.2f;
        score += ResourceBalanceScore(candidate, deck);
        score += CurveScore(candidate);

        // 4. Penalización
        score -= RedundancyPenalty(candidate, deck);

        return score;
    }

    private float EvaluateByPlaystyle(TCGPCard card, List<TCGPCard> deck)
    {
        return preferences.playstyle switch
        {
            BattleType.Aggro => EvaluateAggro(card),
            BattleType.Control => EvaluateControl(card),
            BattleType.Combo => EvaluateCombo(card, deck),
            _ => 0f
        };
    }

    private float EvaluateAggro(TCGPCard card)
    {
        float score = 0f;

        if (card.category == CardCategory.Pokemon && card.moves != null)
        {
            foreach (var move in card.moves)
            {
                int cost = move.cost?.Count ?? 0;

                if (int.TryParse(move.damage, out int dmg))
                {
                    // daño eficiente (daño/costo)
                    if (cost > 0)
                        score += (float)dmg / cost;

                    // daño directo
                    score += dmg * 0.2f;

                    // castigar ataques caros
                    if (cost > 3)
                        score -= 3f;
                }
            }
        }
        if (card.category != CardCategory.Pokemon)
        {
            if (card.description != null && card.description.Contains("draw"))
                score += 5f;
        }

        return score;
    }

    private float EvaluateControl(TCGPCard card)
    {
        float score = 0f;

        if (!string.IsNullOrEmpty(card.description))
        {
            string desc = card.description.ToLower();

            if (desc.Contains("discard")) score += 6f;
            if (desc.Contains("draw")) score += 4f;
            if (desc.Contains("heal")) score += 4f;
            if (desc.Contains("switch")) score += 3f;
        }

        // premiar resistencia
        if (card.category == CardCategory.Pokemon)
            score += card.hp * 0.15f;

        return score;
    }

    private float EvaluateCombo(TCGPCard card, List<TCGPCard> deck)
    {
        float score = 0f;

        // sinergia fuerte
        score += SynergyScore(card, deck) * 2f;

        // penalizar cartas aisladas
        if (SynergyScore(card, deck) == 0)
            score -= 3f;

        // bonus si tiene habilidad (ahora es una lista)
        if (card.ability != null && card.ability.Count > 0)
            score += 4f;

        return score;
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
            // tipo
            if (card.type == candidate.type && card.category == CardCategory.Pokemon && candidate.category == CardCategory.Pokemon)
                score += 2f;

            // evolución básica → stage
            if (card.sub_category == PokemonStage.Basic &&
                candidate.sub_category != PokemonStage.Basic)
            {
                score += 3f;
            }

            // debilidad cruzada (Cubre la debilidad del otro)
            if (card.weakness != null && candidate.type == card.weakness.type) {
                score += 1.5f; 
            }

            // habilidad + texto relacionado (Ahora iterando entre las múltiples habilidades si las tiene)
            if (card.ability != null && card.ability.Count > 0 && candidate.description != null)
            {
                if (candidate.description.Contains(card.name, StringComparison.OrdinalIgnoreCase))
                    score += 5f;
            }

            // Sinergia de Entrenadores (Si el efecto del candidato menciona al Pokémon actual en el mazo)
            if (!string.IsNullOrEmpty(candidate.effect) && candidate.effect.Contains(card.name, StringComparison.OrdinalIgnoreCase))
            {
                score += 5f;
            }
        }

        return score;
    }

    private float ConsistencyScore(TCGPCard card)
    {
        if (card.category == CardCategory.Supporter || card.category == CardCategory.Item)
        {
            if (string.IsNullOrEmpty(card.description)) return 0f;

            string desc = card.description.ToLower();
            float score = 0f;

            if (desc.Contains("draw")) score += 6f;
            if (desc.Contains("search")) score += 6f;
            if (desc.Contains("deck")) score += 4f;
            if (desc.Contains("energy")) score += 3f;

            return score;
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

    private float SupportNeedScore(TCGPCard candidate, List<TCGPCard> deck)
    {
        int pokemon = deck.FindAll(c => c.category == CardCategory.Pokemon).Count;
        int trainer = deck.Count - pokemon;

        float score = 0f;

        // Si hay pocos trainers → premiar fuerte
        if (trainer < 5)
        {
            if (candidate.category != CardCategory.Pokemon)
                score += 15f;
        }

        // Si hay demasiados pokemon → castigar
        if (pokemon > 12)
        {
            if (candidate.category == CardCategory.Pokemon)
                score -= 10f;
        }

        return score;
    }
}

[Serializable]
public class DeckPreferences
{
    public PokemonType preferredType;
    public BattleType playstyle;
}