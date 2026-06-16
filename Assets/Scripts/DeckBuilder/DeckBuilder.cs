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

        int passes = 5; // número de pasadas
        TCGPCard originalAnchor = preferences.anchorCard;

        for (int i = 0; i < passes; i++)
        {
            preferences.anchorCard = originalAnchor;
            // Variamos el orden del pool en cada pasada para explorar distintas combinaciones
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

        Debug.Log($"[BuildDeck] Mejor score obtenido: {bestScore}");

        List<string> validationErrors = ValidateDeck(bestDeck, targetSize);
        if (validationErrors.Count > 0)
            foreach (var error in validationErrors)
                Debug.LogWarning($"[ValidateDeck] {error}");
        else
            Debug.Log("[ValidateDeck] Mazo válido.");

        return bestDeck;
    }
    private List<TCGPCard> ShufflePool(List<TCGPCard> pool, int seed)
    {
        List<TCGPCard> shuffled = new List<TCGPCard>(pool);
        // Garantizamos una semilla distinta en cada intento para generar mazos variados
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

        // Si hay anchor, forzar el Básico de su cadena como primera carta
        if (preferences.anchorCard != null)
        {
            // Buscamos el Básico raíz de la cadena del anchor
            TCGPCard current = preferences.anchorCard;
            while (!string.IsNullOrEmpty(current.evolve_from))
            {
                TCGPCard prev = pool.Find(c =>
                    c.name.Equals(current.evolve_from, StringComparison.OrdinalIgnoreCase));
                if (prev == null) break;
                current = prev;
            }

            // 'current' ahora es el Básico raíz
            TCGPCard anchorBasic = basics.Find(c =>
                c.name.Equals(current.name, StringComparison.OrdinalIgnoreCase));

            if (anchorBasic != null)
            {
                core.Add(anchorBasic);
                basics.Remove(anchorBasic);
            }
        }

        // Ordenar los Básicos restantes por pre-score descendente
        basics.Sort((a, b) =>
        {
            float scoreA = preScores.ContainsKey(a.name) ? preScores[a.name] : 0f;
            float scoreB = preScores.ContainsKey(b.name) ? preScores[b.name] : 0f;
            return scoreB.CompareTo(scoreA);
        });

        // Completar hasta 2 Básicos en el core
        foreach (var card in basics)
        {
            if (core.Count >= 2) break;
            core.Add(card);
        }

        // Trainer con efecto de robo
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

        // Fallback
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

        // CAPA 1: calidad intrínseca (estática)
        float preScore = preScores.ContainsKey(candidate.name) ? preScores[candidate.name] : 0f;
        score += preScore * 0.4f;

        score += AnchorChainBonus(candidate, pool);

        // CAPA 2: valor contextual (dinámico)
        score += CalculateBaseValue(candidate) * 0.5f;
        score += EvaluateByPlaystyle(candidate, deck) * 1.5f;
        score += SupportNeedScore(candidate, deck) * 15f;
        score += SynergyScore(candidate, deck) * 1.5f;
        score += ConsistencyScore(candidate) * 1.2f;
        score += ResourceBalanceScore(candidate, deck);
        score += CurveScore(candidate);
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
                // Calidad base del Pokémon
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

                // Bonus por habilidad
                if (card.ability != null && card.ability.Count > 0)
                    score += 5f;

                // Bonus EX
                if (IsEX(card))
                    score += 10f;

                // Bonus cadena del anchor
                if (preferences.anchorCard != null)
                {
                    // La propia carta anchor
                    if (card.name.Equals(preferences.anchorCard.name, StringComparison.OrdinalIgnoreCase))
                        score += 20f;

                    // Cartas que forman parte de su cadena evolutiva
                    if (IsInAnchorChain(card, pool))
                        score += 15f;
                }
            }
            else
            {
                // Calidad base del Trainer según su efecto
                if (!string.IsNullOrEmpty(card.effect))
                {
                    string e = card.effect.ToLower();
                    if (e.Contains("draw") || e.Contains("roba")) score += 14f;
                    if (e.Contains("search") || e.Contains("busca")) score += 12f;
                    if (e.Contains("heal") || e.Contains("cura")) score += 8f;
                    if (e.Contains("energy") || e.Contains("energía")) score += 8f;
                    if (e.Contains("damage") || e.Contains("daño")) score += 6f;
                }

                if (score == 0f) score += 5f; // fallback
            }

            preScores[card.name] = score;
        }

        return preScores;
    }
    private bool IsInAnchorChain(TCGPCard card, List<TCGPCard> pool)
    {
        if (preferences.anchorCard == null) return false;

        // Caso 1: la carta es la preevolución directa del anchor
        if (!string.IsNullOrEmpty(preferences.anchorCard.evolve_from) &&
            preferences.anchorCard.evolve_from.Equals(card.name, StringComparison.OrdinalIgnoreCase))
            return true;

        // Caso 2: la carta es la preevolución de la preevolución (cadena de 3)
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

        // Condición 1: debe ser EX
        if (!IsEX(preferences.anchorCard))
        {
            Debug.LogWarning($"[Anchor] '{preferences.anchorCard.name}' no es una carta EX. Se construirá sin anchor.");
            preferences.anchorCard = null;
            return;
        }

        // Condición 2: su cadena debe existir en el pool
        // Buscamos recursivamente hasta encontrar el Básico de la cadena
        TCGPCard current = preferences.anchorCard;

        while (!string.IsNullOrEmpty(current.evolve_from))
        {
            TCGPCard prev = pool.Find(c =>
                c.name.Equals(current.evolve_from, StringComparison.OrdinalIgnoreCase));

            if (prev == null)
            {
                Debug.LogWarning($"[Anchor] No se encontró '{current.evolve_from}' en el pool. La cadena de '{preferences.anchorCard.name}' está incompleta. Se construirá sin anchor.");
                preferences.anchorCard = null;
                return;
            }

            current = prev;
        }

        Debug.Log($"[Anchor] '{preferences.anchorCard.name}' validado correctamente. Cadena completa en el pool.");
    }
    private float AnchorChainBonus(TCGPCard candidate, List<TCGPCard> pool)
    {
        if (preferences.anchorCard == null) return 0f;

        // La carta ES el anchor
        if (candidate.name.Equals(preferences.anchorCard.name, StringComparison.OrdinalIgnoreCase))
            return 25f;

        // La carta es parte de la cadena del anchor
        if (IsInAnchorChain(candidate, pool))
            return 15f;

        return 0f;
    }
    private float EvaluateDeckScore(List<TCGPCard> deck, List<TCGPCard> pool)
    {
        float score = 0f;

        int pokemonCount = deck.FindAll(c => c.category == CardCategory.Pokemon).Count;
        int trainerCount = deck.Count - pokemonCount;

        // 1. Balance Trainer/Pokémon
        float trainerRatio = (float)trainerCount / deck.Count;
        if (trainerRatio >= 0.30f && trainerRatio <= 0.45f)
            score += 20f; // rango saludable
        else
            score -= Mathf.Abs(trainerRatio - 0.375f) * 40f; // penalizar desbalance

        // 2. Cadenas evolutivas completas
        foreach (var card in deck)
        {
            if (card.category == CardCategory.Pokemon && !string.IsNullOrEmpty(card.evolve_from))
            {
                bool prevExists = deck.Exists(c =>
                    c.name.Equals(card.evolve_from, StringComparison.OrdinalIgnoreCase));

                score += prevExists ? 10f : -15f; // cadena completa vs huérfano
            }
        }

        // 3. Presencia del anchor y su cadena completa
        if (preferences.anchorCard != null)
        {
            bool anchorExists = deck.Exists(c =>
                c.name.Equals(preferences.anchorCard.name, StringComparison.OrdinalIgnoreCase));

            score += anchorExists ? 30f : -30f;

            // Verificar cadena completa del anchor
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

        // 4. Mínimo 1 Trainer de robo
        bool hasDrawTrainer = deck.Exists(c =>
            IsTrainerCard(c) &&
            !string.IsNullOrEmpty(c.effect) &&
            (c.effect.Contains("draw", StringComparison.OrdinalIgnoreCase) ||
             c.effect.Contains("roba", StringComparison.OrdinalIgnoreCase)));

        score += hasDrawTrainer ? 15f : -20f;

        // 5. Curva de energía general del mazo
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

        if (cheapMoves >= expensiveMoves) score += 10f; // mazo fluido
        else score -= 10f;                              // mazo lento

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