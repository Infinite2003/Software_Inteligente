using System;
using System.Collections.Generic;
using UnityEngine;

public class DeckBuilder
{
    /// <summary>
    /// Builds a deck using an additive algorithm.
    /// It starts with an empty deck and iteratively evaluates and adds the best card from the pool 
    /// until the target deck size is reached.
    /// </summary>
    /// <param name="pool">The pool of available cards.</param>
    /// <param name="targetSize">The desired size of the deck.</param>
    /// <returns>A list of cards representing the final deck.</returns>
    public List<TCGPCard> BuildDeck(List<TCGPCard> pool, int targetSize)
    {
        List<TCGPCard> currentDeck = new List<TCGPCard>();

        // Copy the pool so we can safely remove cards if we don't want duplicates
        List<TCGPCard> availableCards = new List<TCGPCard>(pool);

        while (currentDeck.Count < targetSize && availableCards.Count > 0)
        {
            TCGPCard bestCard = null;
            float bestScore = float.MinValue;

            // Find the best card to add to the deck
            foreach (TCGPCard card in availableCards)
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
                // Add the best card found to the deck
                currentDeck.Add(bestCard);

                // Remove it from the available pool so it's not chosen again
                // (Omit this if you allow multiple copies of the same card)
                availableCards.Remove(bestCard);
            }
            else
            {
                // No valid card found, break to avoid infinite loop
                break;
            }
        }

        return currentDeck;
    }

    /// <summary>
    /// Evaluates how much value a card adds to the current deck.
    /// Replace this logic with your specific game heuristics.
    /// </summary>
    private float EvaluateCardContribution(TCGPCard candidate, List<TCGPCard> currentDeck)
    {
        // 1. Validación dura
        if (!IsValid(candidate, currentDeck))
            return float.MinValue;

        float score = 0f;

        // 2. Valor base (Calculado según TCGPCard ya que no tiene propiedad 'Value')
        score += CalculateBaseValue(candidate);

        // 3. Sinergia
        score += SynergyScore(candidate, currentDeck) * 2.0f;

        // 4. Consistencia
        score += ConsistencyScore(candidate, currentDeck) * 1.5f;

        // 5. Balance recursos
        score += ResourceBalanceScore(candidate, currentDeck) * 1.5f;

        // 6. Curva
        score += CurveScore(candidate, currentDeck);

        // 7. Penalización por redundancia
        score -= RedundancyPenalty(candidate, currentDeck);

        return score;

    }
    private float CalculateBaseValue(TCGPCard candidate)
    {
        float value = 0f;

        if (candidate.category == "Pokemon")
        {
            value += candidate.hp * 0.1f; // Bonificación base por HP
            
            // Evaluar los movimientos
            if (candidate.moves != null)
            {
                foreach (var move in candidate.moves)
                {
                    if (int.TryParse(move.damage, out int dmg))
                    {
                        value += dmg * 0.2f; // Mientras más daño pueda hacer, mejor valor base
                    }
                }
            }
        }
        else if (candidate.category == "Trainer" || candidate.category == "Item" || candidate.category == "Supporter")
        {
            // Las cartas de Trainer / Item tienen un valor base fijo por su utilidad
            value += 10f; 
        }

        return value;
    }

    private bool IsValid(TCGPCard candidate, List<TCGPCard> currentDeck)
    {
        // Regla oficial de TCG POCKET: Máximo 2 copias de la misma carta por mazo (por nombre/ID)
        int copyCount = 0;
        foreach (var card in currentDeck)
        {
            if (card.name == candidate.name) copyCount++;
        }

        if (copyCount >= 2)
            return false;

        return true; 
    }
    private float SynergyScore(TCGPCard candidate, List<TCGPCard> currentDeck)
    {
        float synergy = 0f;

        if (currentDeck.Count == 0) return 0f;

        // 1. Sinergia de Tipos de Pokémon
        if (candidate.category == "Pokemon" && !string.IsNullOrEmpty(candidate.type))
        {
            foreach (var card in currentDeck)
            {
                if (card.category == "Pokemon" && card.type == candidate.type)
                {
                    synergy += 2.0f; // Premiar compartir el mismo tipo de energía/Pokemon
                }
            }
        }

        // 2. Sinergia de Evolución (Si el candidato es Fase 1/2 "Stage 1", buscar su Básico)
        // Nota: esto es una simplificación, requeriría la lógica real de "Evoluciona de..."
        if (candidate.category == "Pokemon" && candidate.sub_category != "Basic")
        {
            bool hasBasic = false;
            foreach (var card in currentDeck)
            {
                if (card.category == "Pokemon" && card.sub_category == "Basic")
                {
                    // Asumimos que si hay básicos y estamos evaluando un Stage 1/2, le damos sinergia
                    hasBasic = true;
                    break;
                }
            }
            if (!hasBasic) synergy -= 10f; // Penalizar severamente si metemos evolución sin básicos
            else synergy += 5f;            // Premiar si hay básicos para evolucionarlo
        }

        return synergy;
    }
    private float ConsistencyScore(TCGPCard candidate, List<TCGPCard> currentDeck)
    {
        // Premiar añadir Trainers de robo / búsqueda de Pokemon ("Supporter")
        if (candidate.category == "Supporter" || candidate.category == "Item")
        {
            // Podrías analizar la 'descripción' buscando palabras como "Draw" o "Search"
            if (!string.IsNullOrEmpty(candidate.description) && 
               (candidate.description.Contains("Draw", System.StringComparison.OrdinalIgnoreCase) || 
                candidate.description.Contains("Search", System.StringComparison.OrdinalIgnoreCase)))
            {
                return 5.0f; 
            }
        }
        return 0f; 
    }
    private float ResourceBalanceScore(TCGPCard candidate, List<TCGPCard> currentDeck)
    {
        int pokemonCount = 0;
        int trainerCount = 0;

        foreach (var card in currentDeck)
        {
            if (card.category == "Pokemon") pokemonCount++;
            else trainerCount++;
        }

        // Regla típica en mazos de 20 cartas: buscar un balance como 12 Pokemon / 8 Trainers
        if (candidate.category == "Pokemon")
        {
            if (pokemonCount > 12) return -5f; // Ya hay muchos Pokemon
            return 2f;
        }
        else 
        {
            if (trainerCount > 8) return -5f; // Ya hay muchos Trainers
            return 2f;
        }
    }
    private float CurveScore(TCGPCard candidate, List<TCGPCard> currentDeck)
    {
        // Penalizar costes de ataque muy altos si no hay suficientes ataques baratos
        if (candidate.category == "Pokemon" && candidate.moves != null && candidate.moves.Count > 0)
        {
            float avgCost = 0f;
            foreach (var move in candidate.moves)
            {
                avgCost += (move.costs != null) ? move.costs.Count : 0;
            }
            avgCost /= candidate.moves.Count;

            // Premiar un coste promedio bajo para los primeros turnos
            if (avgCost <= 1.5f) return 3f;  
            if (avgCost > 3f) return -2f;   
        }
        return 0f; 
    }
    private float RedundancyPenalty(TCGPCard candidate, List<TCGPCard> currentDeck)
    {
        float penalty = 0f;
        // Si ya tengo una copia en el mazo, le damos menos prioridad a la segunda
        foreach (var card in currentDeck)
        {
            if (card.name == candidate.name)
            {
                penalty += 5f; // Penaliza intentar meter copias repetidas a no ser que sea muuuuy buena
            }
        }
        return penalty; 
    }
}

