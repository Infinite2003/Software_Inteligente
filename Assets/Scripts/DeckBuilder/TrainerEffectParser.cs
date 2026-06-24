using System.Collections.Generic;
using System.Text.RegularExpressions;
public static class TrainerEffectParser
{
    public struct EffectScore
    {
        public EffectCategory category;
        public float baseValue;
        public float contextBonus;
    }

    private static readonly Dictionary<string, PokemonType> symbolToType = new Dictionary<string, PokemonType>
    {
        { "{g}", PokemonType.Planta },
        { "{r}", PokemonType.Fuego },
        { "{w}", PokemonType.Agua },
        { "{l}", PokemonType.Rayo },
        { "{p}", PokemonType.Psiquico },
        { "{f}", PokemonType.Lucha },
        { "{d}", PokemonType.Oscuro },
        { "{m}", PokemonType.Metalico },
        { "{n}", PokemonType.Dragon },
        { "{c}", PokemonType.Incolora }
    };

    public static EffectScore Evaluate(TCGPCard card, List<TCGPCard> deck, DeckPreferences preferences)
    {
        if (string.IsNullOrEmpty(card.effect))
            return new EffectScore { category = EffectCategory.Unknown, baseValue = 3f };

        string effect = card.effect.ToLower();

        // Fósil — caso especial primero
        if (effect.Contains("como si fuera un pokémon"))
            return EvaluateFossil(card, deck);

        // Curación
        if (effect.Contains("cura") && effect.Contains("puntos de daño"))
            return EvaluateHealing(effect, preferences);

        // Aceleración de energía — FIX: también detecta desde pila de descartes
        if ((effect.Contains("une") || effect.Contains("mueve")) && effect.Contains("energía"))
            return EvaluateEnergyAcceleration(effect, preferences);

        // Daño adicional
        if (effect.Contains("ataques") && effect.Contains("puntos de daño"))
            return EvaluateDamageBoost(effect, deck);

        // Defensa
        if (effect.Contains("hacen -") && effect.Contains("puntos de daño"))
            return EvaluateDefense(effect);

        // Utilidad con tipo específico — FIX: Losa Singular y similares
        if ((effect.Contains("ponla en tu mano") || effect.Contains("pon") && effect.Contains("mano"))
            && effect.Contains("pokémon"))
            return EvaluateUtility(effect, preferences);

        // Manipulación de banco
        if (effect.Contains("banca") || effect.Contains("puesto activo") && effect.Contains("mano"))
            return EvaluateBenchManipulation(effect, deck);

        // Disrupción
        if (effect.Contains("rival no puede"))
            return new EffectScore { category = EffectCategory.Disruption, baseValue = 12f };

        return new EffectScore { category = EffectCategory.Unknown, baseValue = 5f };
    }

    private static EffectScore EvaluateFossil(TCGPCard card, List<TCGPCard> deck)
    {
        bool hasFossilPokemon = deck.Exists(c =>
            !string.IsNullOrEmpty(c.evolve_from) &&
            c.evolve_from.Equals(card.name, System.StringComparison.OrdinalIgnoreCase));

        return new EffectScore
        {
            category = EffectCategory.Fossil,
            baseValue = hasFossilPokemon ? 15f : 2f
        };
    }

    private static EffectScore EvaluateHealing(string effect, DeckPreferences preferences)
    {
        float value = 8f;

        int amount = ExtractNumber(effect);
        if (amount > 0) value += amount * 0.1f; 

        PokemonType? restrictedType = ExtractTypeSymbol(effect);
        if (restrictedType.HasValue && restrictedType.Value != preferences.preferredType)
            value -= 6f;

        return new EffectScore { category = EffectCategory.Healing, baseValue = value };
    }

    private static EffectScore EvaluateEnergyAcceleration(string effect, DeckPreferences preferences)
    {
        float value = 10f;

        int amount = ExtractNumber(effect);
        if (amount > 1) value += amount * 1.5f;

        PokemonType? energyType = ExtractTypeSymbol(effect);
        if (energyType.HasValue)
        {
            if (energyType.Value == preferences.preferredType)
                value += 8f;
            else
                value -= 7f;
        }

        return new EffectScore { category = EffectCategory.EnergyAcceleration, baseValue = value };
    }

    private static EffectScore EvaluateDamageBoost(string effect, List<TCGPCard> deck)
    {
        float value = 8f;

        int boost = ExtractNumber(effect);
        if (boost > 0) value += boost * 0.15f;

        float nameBonus = EvaluatePokemonNameMentions(effect, deck);

        // Trainers que solo funcionan con Pokémon específicos
        bool isSpecific = effect.Contains("ninetales") ||
                          effect.Contains("rapidash") ||
                          effect.Contains("magmar") ||
                          effect.Contains("raichu") ||
                          effect.Contains("electrode") ||
                          effect.Contains("electabuzz") ||
                          effect.Contains("golem") ||
                          effect.Contains("onix");

        if (isSpecific && nameBonus == 0f)
            value -= 10f;

        value += nameBonus;

        return new EffectScore { category = EffectCategory.DamageBoost, baseValue = value };
    }

    private static EffectScore EvaluateDefense(string effect)
    {
        float value = 7f;

        int reduction = ExtractNumber(effect);
        if (reduction > 0) value += reduction * 0.2f;

        return new EffectScore { category = EffectCategory.DamageReduction, baseValue = value };
    }

    private static EffectScore EvaluateUtility(string effect, DeckPreferences preferences)
    {
        float value = 6f;

        PokemonType? restrictedType = ExtractTypeSymbol(effect);
        if (restrictedType.HasValue)
        {
            if (restrictedType.Value == preferences.preferredType)
                value += 4f;
            else
                value -= 4f;
        }

        return new EffectScore { category = EffectCategory.Utility, baseValue = value };
    }

    private static EffectScore EvaluateBenchManipulation(string effect, List<TCGPCard> deck)
    {
        float value = 8f;

        value += EvaluatePokemonNameMentions(effect, deck);

        return new EffectScore { category = EffectCategory.BenchManipulation, baseValue = value };
    }


    private static int ExtractNumber(string text)
    {
        Match match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private static PokemonType? ExtractTypeSymbol(string effect)
    {
        foreach (var pair in symbolToType)
        {
            if (effect.Contains(pair.Key))
                return pair.Value;
        }
        return null;
    }

    private static float EvaluatePokemonNameMentions(string effect, List<TCGPCard> deck)
    {
        float bonus = 0f;
        foreach (var card in deck)
        {
            if (card.category == CardCategory.Pokemon &&
                !string.IsNullOrEmpty(card.name) &&
                effect.Contains(card.name.ToLower(), System.StringComparison.OrdinalIgnoreCase))
            {
                bonus += 5f;
            }
        }
        return bonus;
    }
}