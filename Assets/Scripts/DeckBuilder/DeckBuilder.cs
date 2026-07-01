using System;
using System.Collections.Generic;
using UnityEngine;

public class DeckBuilder
{
    private DeckPreferences preferences;

    public List<TCGPCard> BuildDeck(List<TCGPCard> pool, int targetSize, DeckPreferences prefs)
    {
        preferences = prefs;

        List<TCGPCard> bestDeck = null;
        float bestScore = float.MinValue;

        int passes = 5;
        TCGPCard originalAnchor = preferences.anchorCard;

        for (int i = 0; i < passes; i++)
        {
            preferences.anchorCard = originalAnchor;
            List<TCGPCard> shuffledPool = ShufflePool(pool, i);

            List<TCGPCard> candidate = BuildDeckSingle(shuffledPool, targetSize);
            float candidateScore = EvaluateDeckScore(candidate, shuffledPool);

            Debug.Log($"[Pasada {i + 1}] Score: {candidateScore}");

            if (candidateScore > bestScore)
            {
                bestScore = candidateScore;
                bestDeck = candidate;
            }
        }

        List<string> validationErrors = ValidateDeck(bestDeck, targetSize);
        return bestDeck;
    }
    private List<TCGPCard> ShufflePool(List<TCGPCard> pool, int seed)
    {
        List<TCGPCard> shuffled = new List<TCGPCard>(pool);
        System.Random rng = new System.Random(System.Guid.NewGuid().GetHashCode() + seed);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            TCGPCard temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        return shuffled;
    }
    private List<TCGPCard> BuildDeckSingle(List<TCGPCard> pool, int targetSize)
    {
        List<TCGPCard> filteredPool = pool.FindAll(card =>
        {
            if (card.category != CardCategory.Pokemon) return true;
            return card.type == preferences.preferredType;
        });

        ValidateAnchor(filteredPool);
        Dictionary<string, float> preScores = BuildPreScores(filteredPool);

        List<TCGPCard> currentDeck = new List<TCGPCard>();
        currentDeck.AddRange(SelectCoreCards(filteredPool, preScores));

        Dictionary<string, int> available = BuildAvailablePool(filteredPool);

        foreach (var card in currentDeck)
        {
            if (available.ContainsKey(card.name))
            {
                available[card.name]--;
                if (available[card.name] <= 0)
                    available.Remove(card.name);
            }
        }

        while (currentDeck.Count < targetSize && available.Count > 0)
        {
            TCGPCard bestCard = null;
            float bestScore = float.MinValue;

            foreach (var card in filteredPool)
            {
                if (!available.ContainsKey(card.name)) continue;

                float score = EvaluateCardContribution(card, currentDeck, preScores, filteredPool);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCard = card;
                }
            }

            if (bestCard != null)
            {
                currentDeck.Add(bestCard);
                available[bestCard.name]--;
                if (available[bestCard.name] <= 0)
                    available.Remove(bestCard.name);
            }
            else break;
        }

        return currentDeck;
    }

    private Dictionary<string, int> BuildAvailablePool(List<TCGPCard> pool)
    {
        Dictionary<string, int> available = new Dictionary<string, int>();

        foreach (var card in pool)
        {
            if (!available.ContainsKey(card.name))
                available[card.name] = 2;
        }

        return available;
    }

    private bool IsTrainerCard(TCGPCard card)
    {
        return card.category == CardCategory.Trainer || 
               card.category == CardCategory.Supporter || 
               card.category == CardCategory.Item;
    }

    private List<TCGPCard> SelectCoreCards(List<TCGPCard> pool, Dictionary<string, float> preScores)
    {
        List<TCGPCard> core = new List<TCGPCard>();

        List<TCGPCard> basics = pool.FindAll(c =>
            c.category == CardCategory.Pokemon &&
            c.sub_category == PokemonStage.Basic &&
            c.type == preferences.preferredType);

        if (preferences.anchorCard != null)
        {
            TCGPCard current = preferences.anchorCard;
            while (!string.IsNullOrEmpty(current.evolve_from))
            {
                TCGPCard prev = pool.Find(c =>
                    c.name.Equals(current.evolve_from, StringComparison.OrdinalIgnoreCase));
                if (prev == null) break;
                current = prev;
            }

            TCGPCard anchorBasic = basics.Find(c =>
                c.name.Equals(current.name, StringComparison.OrdinalIgnoreCase));

            if (anchorBasic != null)
            {
                core.Add(anchorBasic);
                basics.Remove(anchorBasic);
            }
        }

        basics.Sort((a, b) =>
        {
            float scoreA = preScores.ContainsKey(a.name) ? preScores[a.name] : 0f;
            float scoreB = preScores.ContainsKey(b.name) ? preScores[b.name] : 0f;
            return scoreB.CompareTo(scoreA);
        });

        foreach (var card in basics)
        {
            if (core.Count >= 2) break;
            core.Add(card);
        }

        bool trainerFound = false;
        foreach (var card in pool)
        {
            if (IsTrainerCard(card) && !core.Exists(c => c.name == card.name))
            {
                TrainerEffectParser.EffectScore effectScore = TrainerEffectParser.Evaluate(card, core, preferences);
                if (effectScore.baseValue >= 10f)
                {
                    core.Add(card);
                    trainerFound = true;
                    break;
                }
            }
        }

        if (!trainerFound)
        {
            foreach (var card in pool)
            {
                if (IsTrainerCard(card) && !core.Exists(c => c.name == card.name))
                {
                    core.Add(card);
                    break;
                }
            }
        }

        return core;
    }

    private float EvaluateCardContribution(TCGPCard candidate, List<TCGPCard> deck, Dictionary<string, float> preScores, List<TCGPCard> pool)
    {
        if (!IsValid(candidate, deck))
            return float.MinValue;

        float score = 0f;

        float preScore = preScores.ContainsKey(candidate.name) ? preScores[candidate.name] : 0f;
        score += preScore * 0.4f;

        score += AnchorChainBonus(candidate, pool);

        score += CalculateBaseValue(candidate) * 0.5f;
        score += EvaluateByPlaystyle(candidate, deck) * 1.5f;
        score += SupportNeedScore(candidate, deck) * 15f;
        score += SynergyScore(candidate, deck) * 1.5f;
        score += ResourceBalanceScore(candidate, deck);
        score += CurveScore(candidate);
        score -= RedundancyPenalty(candidate, deck);

        if (IsTrainerCard(candidate))
        {
            TrainerEffectParser.EffectScore trainerScore = TrainerEffectParser.Evaluate(candidate, deck, preferences);
            score += (trainerScore.baseValue + trainerScore.contextBonus) * 1.2f;
        }

        if (candidate.category == CardCategory.Pokemon)
        {
            float abilityScore = AbilityEvaluator.Evaluate(candidate, deck, preferences);
            score += abilityScore * 1.2f;
        }

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
                    if (cost > 0)
                        score += (float)dmg / cost;

                    score += dmg * 0.2f;

                    if (cost > 3)
                        score -= 3f;
                }
            }
        }
        if (card.category != CardCategory.Pokemon)
        {
            if (card.effect != null && card.effect.Contains("draw", StringComparison.OrdinalIgnoreCase))
                score += 5f;
        }

        return score;
    }

    private float EvaluateControl(TCGPCard card)
    {
        float score = 0f;

        if (card.category == CardCategory.Pokemon)
        {
            score += card.hp * 0.15f;

            score += AbilityEvaluator.Evaluate(card, new List<TCGPCard>(), preferences);
        }
        else
        {
            TrainerEffectParser.EffectScore effectScore = TrainerEffectParser.Evaluate(card, new List<TCGPCard>(), preferences);
            score += effectScore.baseValue;

            if (effectScore.category == EffectCategory.Disruption) score += 4f;
            if (effectScore.category == EffectCategory.DamageReduction) score += 3f;
            if (effectScore.category == EffectCategory.BenchManipulation) score += 2f;
        }

        return score;
    }

    private float EvaluateCombo(TCGPCard card, List<TCGPCard> deck)
    {
        float score = 0f;

        float synergy = SynergyScore(card, deck);
        score += synergy * 0.5f;
        if (synergy == 0f) score -= 3f;

        if (card.category == CardCategory.Pokemon)
        {
            float abilityScore = AbilityEvaluator.Evaluate(card, deck, preferences);
            score += abilityScore * 0.8f; 
        }

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
        else
        {
            TrainerEffectParser.EffectScore effectScore = TrainerEffectParser.Evaluate(card, new List<TCGPCard>(), preferences);
            value = effectScore.baseValue;
        }

        return value;
    }

    private bool IsValid(TCGPCard candidate, List<TCGPCard> deck)
    {
        int count = deck.FindAll(c => c.name == candidate.name).Count;
        if (count >= 2) return false;

        if (candidate.category == CardCategory.Pokemon &&
            !string.IsNullOrEmpty(candidate.evolve_from))
        {
            bool prevExists = deck.Exists(c => c.name.Equals(candidate.evolve_from, StringComparison.OrdinalIgnoreCase));
            if (!prevExists) return false;
        }

        return true;
    }

    private float SynergyScore(TCGPCard candidate, List<TCGPCard> deck)
    {
        float score = 0f;

        foreach (var card in deck)
        {
            if (card.type == candidate.type && card.category == CardCategory.Pokemon && candidate.category == CardCategory.Pokemon)
                score += 2f;

            if (!string.IsNullOrEmpty(candidate.evolve_from) && candidate.evolve_from.Equals(card.name, StringComparison.OrdinalIgnoreCase))
            {
                score += 3f;
            }

            if (card.weakness != null && candidate.type == card.weakness.type) {
                score += 1.5f; 
            }

            if (candidate.ability != null && candidate.ability.Count > 0 && !string.IsNullOrEmpty(card.name))
            {
                foreach(var ab in candidate.ability)
                {
                    if(!string.IsNullOrEmpty(ab.effect) && ab.effect.Contains(card.name, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 5f;
                    }
                }
            }
            if (candidate.category != CardCategory.Pokemon && !string.IsNullOrEmpty(candidate.effect) && candidate.effect.Contains(card.name, StringComparison.OrdinalIgnoreCase))
            {
                score += 5f;
            }
        }

        return score;
    }

    private float ResourceBalanceScore(TCGPCard candidate, List<TCGPCard> deck)
    {
        int pokemonCount = deck.FindAll(c => c.category == CardCategory.Pokemon).Count;
        int trainerCount = deck.Count - pokemonCount;

        if (candidate.category == CardCategory.Pokemon && pokemonCount >= 18) return -50f;
        if (candidate.category != CardCategory.Pokemon && trainerCount >= 18) return -50f;

        return 0f;
    }

    private float CurveScore(TCGPCard card)
    {
        if (card.category == CardCategory.Pokemon && card.moves != null && card.moves.Count > 0)
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
        return deck.Exists(c => c.name == card.name) ? 15f : 0f;
    }

    private float SupportNeedScore(TCGPCard candidate, List<TCGPCard> deck)
    {
        int pokemon = deck.FindAll(c => c.category == CardCategory.Pokemon).Count;
        int trainer = deck.Count - pokemon;

        float score = 0f;
        int currentSize = deck.Count;

        if (currentSize > 0)
        {
            float targetTrainerRatio = 0.40f;
            float currentTrainerRatio = (float)trainer / currentSize;

            if (currentTrainerRatio < targetTrainerRatio && candidate.category != CardCategory.Pokemon)
            {
                float deficit = targetTrainerRatio - currentTrainerRatio;
                float urgencyFactor = Mathf.Clamp(1f + (10f - currentSize) / 10f, 1f, 2f); 
                score += deficit * 40f * urgencyFactor;
            }
        }

        return score;
    }
    private bool IsEX(TCGPCard card)
    {
        return !string.IsNullOrEmpty(card.name) &&
               card.name.EndsWith(" ex", StringComparison.OrdinalIgnoreCase);
    }
    private List<string> ValidateDeck(List<TCGPCard> deck, int targetSize)
    {
        List<string> errors = new List<string>();

        // Regla 1: tamaño exacto
        if (deck.Count != targetSize)
            errors.Add($"El mazo tiene {deck.Count} cartas, se esperaban {targetSize}.");

        // Regla 2: mínimo 1 Básico
        bool hasBasic = deck.Exists(c =>
            c.category == CardCategory.Pokemon &&
            c.sub_category == PokemonStage.Basic);

        if (!hasBasic)
            errors.Add("El mazo no tiene ningún Pokémon Básico. El jugador perdería al inicio.");

        // Regla 3: máximo 2 copias
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var card in deck)
        {
            if (!counts.ContainsKey(card.name)) counts[card.name] = 0;
            counts[card.name]++;
            if (counts[card.name] > 2)
                errors.Add($"'{card.name}' aparece {counts[card.name]} veces (máximo 2).");
        }

        // Regla 4: cadenas huérfanas
        foreach (var card in deck)
        {
            if (card.category == CardCategory.Pokemon &&
                !string.IsNullOrEmpty(card.evolve_from))
            {
                bool prevExists = deck.Exists(c =>
                    c.name.Equals(card.evolve_from, StringComparison.OrdinalIgnoreCase));

                if (!prevExists)
                    errors.Add($"'{card.name}' está en el mazo pero '{card.evolve_from}' no.");
            }
        }

        return errors;
    }
    private Dictionary<string, float> BuildPreScores(List<TCGPCard> pool)
    {
        Dictionary<string, float> preScores = new Dictionary<string, float>();

        foreach (var card in pool)
        {
            float score = 0f;

            if (card.category == CardCategory.Pokemon)
            {
                score += card.hp * 0.1f;

                if (card.moves != null)
                {
                    foreach (var move in card.moves)
                    {
                        int cost = move.cost?.Count ?? 0;
                        if (int.TryParse(move.damage, out int dmg))
                        {
                            score += dmg * 0.2f;
                            if (cost > 0) score += (float)dmg / cost;
                            if (cost > 3) score -= 3f;
                        }
                    }
                }

                score += AbilityEvaluator.Evaluate(card, new List<TCGPCard>(), preferences);

                if (IsEX(card))
                    score += 10f;

                if (preferences.anchorCard != null)
                {
                    if (card.name.Equals(preferences.anchorCard.name, StringComparison.OrdinalIgnoreCase))
                        score += 20f;

                    if (IsInAnchorChain(card, pool))
                        score += 15f;
                }
            }
            else
            {
                TrainerEffectParser.EffectScore effectScore = TrainerEffectParser.Evaluate(card, new List<TCGPCard>(), preferences);
                score += effectScore.baseValue;

                if (effectScore.category == EffectCategory.EnergyAcceleration) score += 3f;
                if (effectScore.category == EffectCategory.Fossil) score += 2f;
                if (effectScore.category == EffectCategory.Disruption) score += 4f;
            }

            preScores[card.name] = score;
        }

        return preScores;
    }
    private bool IsInAnchorChain(TCGPCard card, List<TCGPCard> pool)
    {
        if (preferences.anchorCard == null) return false;

        if (!string.IsNullOrEmpty(preferences.anchorCard.evolve_from) &&
            preferences.anchorCard.evolve_from.Equals(card.name, StringComparison.OrdinalIgnoreCase))
            return true;

        TCGPCard middleEvolution = pool.Find(c =>
            !string.IsNullOrEmpty(preferences.anchorCard.evolve_from) &&
            c.name.Equals(preferences.anchorCard.evolve_from, StringComparison.OrdinalIgnoreCase));

        if (middleEvolution != null &&
            !string.IsNullOrEmpty(middleEvolution.evolve_from) &&
            middleEvolution.evolve_from.Equals(card.name, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
    private void ValidateAnchor(List<TCGPCard> pool)
    {
        if (preferences.anchorCard == null) return;

        if (!IsEX(preferences.anchorCard))
        {
            preferences.anchorCard = null;
            return;
        }

        TCGPCard current = preferences.anchorCard;

        while (!string.IsNullOrEmpty(current.evolve_from))
        {
            TCGPCard prev = pool.Find(c =>
                c.name.Equals(current.evolve_from, StringComparison.OrdinalIgnoreCase));

            if (prev == null)
            {
                preferences.anchorCard = null;
                return;
            }

            current = prev;
        }
    }
    private float AnchorChainBonus(TCGPCard candidate, List<TCGPCard> pool)
    {
        if (preferences.anchorCard == null) return 0f;

        if (candidate.name.Equals(preferences.anchorCard.name, StringComparison.OrdinalIgnoreCase))
            return 25f;

        if (IsInAnchorChain(candidate, pool))
            return 15f;

        return 0f;
    }
    private float EvaluateDeckScore(List<TCGPCard> deck, List<TCGPCard> pool)
    {
        float score = 0f;

        int pokemonCount = deck.FindAll(c => c.category == CardCategory.Pokemon).Count;
        int trainerCount = deck.Count - pokemonCount;

        float trainerRatio = (float)trainerCount / deck.Count;
        if (trainerRatio >= 0.30f && trainerRatio <= 0.45f)
            score += 20f;
        else
            score -= Mathf.Abs(trainerRatio - 0.375f) * 40f;

        foreach (var card in deck)
        {
            if (card.category == CardCategory.Pokemon && !string.IsNullOrEmpty(card.evolve_from))
            {
                bool prevExists = deck.Exists(c =>
                    c.name.Equals(card.evolve_from, StringComparison.OrdinalIgnoreCase));

                score += prevExists ? 10f : -15f;
            }
        }

        if (preferences.anchorCard != null)
        {
            bool anchorExists = deck.Exists(c =>
                c.name.Equals(preferences.anchorCard.name, StringComparison.OrdinalIgnoreCase));

            score += anchorExists ? 30f : -30f;

            TCGPCard current = preferences.anchorCard;
            while (!string.IsNullOrEmpty(current.evolve_from))
            {
                TCGPCard prev = pool.Find(c =>
                    c.name.Equals(current.evolve_from, StringComparison.OrdinalIgnoreCase));

                if (prev == null) break;

                bool chainExists = deck.Exists(c =>
                    c.name.Equals(prev.name, StringComparison.OrdinalIgnoreCase));

                score += chainExists ? 15f : -15f;
                current = prev;
            }
        }


        bool hasImpactTrainer = deck.Exists(c =>
        {
            if (!IsTrainerCard(c)) return false;
            TrainerEffectParser.EffectScore effectScore = TrainerEffectParser.Evaluate(c, deck, preferences);
            return effectScore.baseValue >= 10f;
        });

        score += hasImpactTrainer ? 15f : -10f;

        int cheapMoves = 0;
        int expensiveMoves = 0;
        foreach (var card in deck)
        {
            if (card.category == CardCategory.Pokemon && card.moves != null)
            {
                foreach (var move in card.moves)
                {
                    int cost = move.cost?.Count ?? 0;
                    if (cost <= 2) cheapMoves++;
                    else expensiveMoves++;
                }
            }
        }

        if (cheapMoves >= expensiveMoves) score += 10f;
        else score -= 10f;

        return score;
    }
}

[Serializable]
public class DeckPreferences
{
    public PokemonType preferredType;
    public BattleType playstyle;
    public TCGPCard anchorCard;
}