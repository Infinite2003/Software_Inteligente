using System;
using System.Collections.Generic;
using UnityEngine;

public class DeckBuilder
{
    private DeckPreferences preferences;

    public List<TCGPCard> BuildDeck(List<TCGPCard> pool, int targetSize, DeckPreferences prefs)
    {
        preferences = prefs;

        List<TCGPCard> filteredPool = pool.FindAll(card =>
        {
            // Trainers siempre pasan independientemente del tipo
            if (card.category != CardCategory.Pokemon) return true;

            // Pokémon solo del tipo preferido
            return card.type == preferences.preferredType;
        });

        List<TCGPCard> currentDeck = new List<TCGPCard>();
        currentDeck.AddRange(SelectCoreCards(filteredPool));

        // FIX #1: diccionario de usos disponibles en lugar de lista de referencias
        Dictionary<string, int> available = BuildAvailablePool(filteredPool);

        // Descontamos lo que ya metió SelectCoreCards
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
                if (!available.ContainsKey(card.name))
                    continue;

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

    private List<TCGPCard> SelectCoreCards(List<TCGPCard> pool)
    {
        List<TCGPCard> core = new List<TCGPCard>();

        // 1. Hasta 2 Pokémons Básicos — priorizamos los que tienen cadena evolutiva
        //    (es decir, algún otro Pokémon en el pool tiene evolve_from apuntando a ellos)
        List<TCGPCard> basics = pool.FindAll(c =>
            c.category == CardCategory.Pokemon &&
            c.sub_category == PokemonStage.Basic &&
            c.type == preferences.preferredType);

        basics.Sort((a, b) =>
        {
            bool aHasEvolution = pool.Exists(c => !string.IsNullOrEmpty(c.evolve_from) &&
                c.evolve_from.Equals(a.name, StringComparison.OrdinalIgnoreCase));
            bool bHasEvolution = pool.Exists(c => !string.IsNullOrEmpty(c.evolve_from) &&
                c.evolve_from.Equals(b.name, StringComparison.OrdinalIgnoreCase));

            return bHasEvolution.CompareTo(aHasEvolution); // primero los que tienen evolución
        });

        foreach (var card in basics)
        {
            if (core.Count >= 2) break;
            core.Add(card);
        }

        // 2. Trainer con efecto de robo — verificamos que no esté ya en el core
        bool trainerFound = false;
        foreach (var card in pool)
        {
            if (IsTrainerCard(card) && !core.Exists(c => c.name == card.name))
            {
                if (card.effect != null && (card.effect.Contains("draw", StringComparison.OrdinalIgnoreCase) ||
                                            card.effect.Contains("roba", StringComparison.OrdinalIgnoreCase)))
                {
                    core.Add(card);
                    trainerFound = true;
                    break;
                }
            }
        }

        // 3. Fallback — cualquier Trainer no duplicado
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

    private float EvaluateCardContribution(TCGPCard candidate, List<TCGPCard> deck)
    {
        if (!IsValid(candidate, deck))
            return float.MinValue;

        float score = 0f;

        // 1. Base mínima
        score += CalculateBaseValue(candidate) * 0.5f;

        // 2. ESTRATEGIA (principal)
        score += EvaluateByPlaystyle(candidate, deck) * 1.5f; // Bajado de 2.5f a 1.5f para no asfixiar a los Trainers

        // Multiplicador general gigantezado para forzar matemáticamente los trainers debido a que Playstyle daba puntos altisimos
        score += SupportNeedScore(candidate, deck) * 15f; 

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
            if (card.effect != null && card.effect.Contains("draw", StringComparison.OrdinalIgnoreCase))
                score += 5f;
        }

        return score;
    }

    private float EvaluateControl(TCGPCard card)
    {
        float score = 0f;

        // Leer de Ability (si es Pokemon), Effect (Si es Trainer) o Description
        string desc = string.Empty;
        if (!string.IsNullOrEmpty(card.effect)) desc += card.effect.ToLower() + " ";
        if (!string.IsNullOrEmpty(card.description)) desc += card.description.ToLower() + " ";
        if (card.ability != null)
        {
            foreach(var ab in card.ability)
                if (!string.IsNullOrEmpty(ab.effect)) desc += ab.effect.ToLower() + " ";
        }

        if (!string.IsNullOrEmpty(desc))
        {
            if (desc.Contains("discard") || desc.Contains("descarta")) score += 6f;
            if (desc.Contains("draw") || desc.Contains("roba")) score += 4f;
            if (desc.Contains("heal") || desc.Contains("cura")) score += 4f;
            if (desc.Contains("switch") || desc.Contains("cambia")) score += 3f;
        }

        // premiar resistencia
        if (card.category == CardCategory.Pokemon)
            score += card.hp * 0.15f;

        return score;
    }

    private float EvaluateCombo(TCGPCard card, List<TCGPCard> deck)
    {
        float score = 0f;

        float synergy = SynergyScore(card, deck);  // una sola llamada

        score += synergy * 0.5f;                   // bonus leve adicional
        if (synergy == 0f) score -= 3f;            // penalizar cartas aisladas

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
        else
        {
            // Base según impacto del efecto
            if (string.IsNullOrEmpty(card.effect))
            {
                value += 5f;
            }
            else
            {
                string e = card.effect.ToLower();
                if (e.Contains("draw") || e.Contains("roba")) value += 14f;
                if (e.Contains("search") || e.Contains("busca")) value += 12f;
                if (e.Contains("heal") || e.Contains("cura")) value += 8f;
                if (e.Contains("energy") || e.Contains("energía")) value += 8f;
                if (e.Contains("damage") || e.Contains("daño")) value += 6f;
                if (value == 0f) value += 5f; // fallback si no matchea nada
            }
        }

        return value;
    }

    private bool IsValid(TCGPCard candidate, List<TCGPCard> deck)
    {
        // Regla 1: máximo 2 copias
        int count = deck.FindAll(c => c.name == candidate.name).Count;
        if (count >= 2) return false;

        // Regla 2: un Stage 1 o Stage 2 no puede entrar si su preevolución no está en el mazo
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
            // tipo
            if (card.type == candidate.type && card.category == CardCategory.Pokemon && candidate.category == CardCategory.Pokemon)
                score += 2f;

            // evolución básica → stage
            if (!string.IsNullOrEmpty(candidate.evolve_from) && candidate.evolve_from.Equals(card.name, StringComparison.OrdinalIgnoreCase))
            {
                score += 3f;
            }

            // debilidad cruzada (Cubre la debilidad del otro)
            if (card.weakness != null && candidate.type == card.weakness.type) {
                score += 1.5f; 
            }

            // habilidad + texto relacionado (Ahora iterando entre las múltiples habilidades si las tiene)
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

            // Sinergia de Entrenadores (Si el efecto del candidato menciona al Pokémon actual en el mazo)
            if (candidate.category != CardCategory.Pokemon && !string.IsNullOrEmpty(candidate.effect) && candidate.effect.Contains(card.name, StringComparison.OrdinalIgnoreCase))
            {
                score += 5f;
            }
        }

        return score;
    }

    private float ConsistencyScore(TCGPCard card)
    {
        if (IsTrainerCard(card))
        {
            // Usamos 'effect' ya que así viene de tu JSON para Entrenadores
            if (string.IsNullOrEmpty(card.effect)) return 0f;

            string desc = card.effect.ToLower();
            float score = 0f;

            // Busca palabras clave sin importar el idioma (ej: "roba" vs "draw")
            if (desc.Contains("draw") || desc.Contains("roba")) score += 6f;
            if (desc.Contains("search") || desc.Contains("busca")) score += 6f;
            if (desc.Contains("deck") || desc.Contains("mazo")) score += 4f;
            if (desc.Contains("energy") || desc.Contains("energía")) score += 3f;

            return score;
        }

        return 0f;
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
                float urgencyFactor = Mathf.Clamp(1f + (10f - currentSize) / 10f, 1f, 2f); // antes llegaba a 4.4f+
                score += deficit * 40f * urgencyFactor; // antes era 150f
            }
        }

        return score;
    }
}

[Serializable]
public class DeckPreferences
{
    public PokemonType preferredType;
    public BattleType playstyle;
    public TCGPCard anchorCard; // opcional, para decks centrados en una carta específica
}