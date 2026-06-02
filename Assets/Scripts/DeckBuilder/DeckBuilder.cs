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
            if (card.category == CardCategory.Pokemon)
                return card.type == preferences.preferredType;
            return true;
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

        // 1. Añadimos primero hasta 2 Pokémons Básicos (motor inicial)
        foreach (var card in pool)
        {
            if (card.category == CardCategory.Pokemon &&
                card.sub_category == PokemonStage.Basic &&
                card.type == preferences.preferredType)
            {
                core.Add(card);

                if (core.Count >= 2) 
                    break;
            }
        }

        // 2. Obligamos a que la 3ra carta fundacional sea un Trainer (Preferiblemente Supporter de robo)
        bool trainerFound = false;
        foreach (var card in pool)
        {
            if (IsTrainerCard(card))
            {
                if (card.effect != null && (card.effect.Contains("draw", StringComparison.OrdinalIgnoreCase) || card.effect.Contains("roba", StringComparison.OrdinalIgnoreCase)))
                {
                    core.Add(card);
                    trainerFound = true;
                    break; 
                }
            }
        }

        // 3. Fallback (Issue 2 fix: No meter duplicado si el paso 2 ya lo encontró)
        if (!trainerFound && core.Count < 3)
        {
            foreach (var card in pool)
            {
                if (IsTrainerCard(card))
                {
                    // Valida que no intentemos meter la misma carta base 2 veces en el motor
                    if (!core.Exists(c => c.name == card.name))
                    {
                        core.Add(card);
                        break;
                    }
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

        // ESTRICTA RESPONSABILIDAD: SOLO Muros de Cristal.
        // Nada de dar puntos positivos aquí. Eso lo hace SupportNeed. Solo penalizar duro si traspasa roles.

        if (candidate.category == CardCategory.Pokemon)
        {
            if (pokemonCount >= 14) return -50f; // Techo duro para Pokemons
        }
        else 
        {
            if (trainerCount >= 10) return -50f; // Techo duro para Trainers
        }

        return 0f;
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
        int currentSize = deck.Count;

        // Evaluación dinámica y urgente basada en el déficit actual de Trainers.
        if (currentSize > 0)
        {
            float targetTrainerRatio = 0.40f; 
            float currentTrainerRatio = (float)trainer / currentSize;

            if (currentTrainerRatio < targetTrainerRatio && candidate.category != CardCategory.Pokemon)
            {
                float deficit = targetTrainerRatio - currentTrainerRatio;

                // INVERSIÓN DEL MULTIPLICADOR (Issue 1)
                // Hacemos que de un bonus MASIVO cuando la lista está escasa de Trainers
                float urgencyFactor = Mathf.Max(1f, 1f + (20f - currentSize) / 5f); // Escala la urgencia inversamente al tamaño actual mucho mas agresiva

                score += deficit * 150f * urgencyFactor; 
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
}