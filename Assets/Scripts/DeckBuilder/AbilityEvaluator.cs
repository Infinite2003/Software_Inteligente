using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class AbilityEvaluator
{
    public struct AbilityScore
    {
        public EffectCategory category;
        public float baseValue;
        public float contextBonus;
    }

    public static float Evaluate(TCGPCard card, List<TCGPCard> deck, DeckPreferences preferences)
    {
        if (card.ability == null || card.ability.Count == 0)
            return 0f;

        float totalScore = 0f;

        foreach (var ability in card.ability)
        {
            if (string.IsNullOrEmpty(ability.effect)) continue;

            AbilityScore score = EvaluateAbility(ability.effect.ToLower(), deck, preferences);
            totalScore += score.baseValue + score.contextBonus;
        }

        return totalScore;
    }

    private static AbilityScore EvaluateAbility(string effect, List<TCGPCard> deck, DeckPreferences preferences)
    {
        // Curación
        if (effect.Contains("curar") || effect.Contains("cura") && effect.Contains("puntos de daño"))
            return EvaluateHealing(effect);

        // Aceleración de energía
        if ((effect.Contains("unir") || effect.Contains("une") || effect.Contains("mover")) && effect.Contains("energía"))
            return EvaluateEnergyAcceleration(effect, preferences);

        // Daño pasivo / represalia
        if (effect.Contains("pokémon atacante sufre") || effect.Contains("resulta dañado"))
            return EvaluatePassiveDamage(effect);

        // Reducción de daño
        if (effect.Contains("ataques hacen -") || effect.Contains("hacen -") && effect.Contains("puntos de daño"))
            return EvaluateDamageReduction(effect);

        // Disrupción al rival
        if (effect.Contains("rival no puede"))
            return EvaluateDisruption(effect);

        // Efecto de estado (veneno, sueño)
        if (effect.Contains("envenenado") || effect.Contains("dormido") || effect.Contains("paralizado"))
            return EvaluateStatusEffect(effect);

        // Manipulación de banco
        if (effect.Contains("banca") && (effect.Contains("rival") || effect.Contains("activo")))
            return EvaluateBenchManipulation(effect);

        // Daño directo por habilidad (ej. Greninja)
        if (effect.Contains("puntos de daño") && effect.Contains("pokémon de tu rival"))
            return EvaluateDirectDamage(effect);

        // Utilidad (ver carta, reducir retirada, etc.)
        if (effect.Contains("mirar") || effect.Contains("primera carta") || effect.Contains("baraja"))
            return new AbilityScore { category = EffectCategory.Utility, baseValue = 5f };

        return new AbilityScore { category = EffectCategory.Unknown, baseValue = 3f };
    }

    private static AbilityScore EvaluateHealing(string effect)
    {
        float value = 8f;

        int amount = ExtractNumber(effect);
        if (amount > 0) value += amount * 0.15f; 

        if (effect.Contains("cada uno") || effect.Contains("todos"))
            value += 5f;

        return new AbilityScore { category = EffectCategory.Healing, baseValue = value };
    }

    private static AbilityScore EvaluateEnergyAcceleration(string effect, DeckPreferences preferences)
    {
        float value = 10f;

        int amount = ExtractNumber(effect);
        if (amount > 1) value += amount * 1.5f;

        if (effect.Contains("proporciona 2"))
            value += 10f;

        if (effect.Contains("todas las veces") || effect.Contains("quieras"))
            value += 6f;

        return new AbilityScore { category = EffectCategory.EnergyAcceleration, baseValue = value };
    }

    private static AbilityScore EvaluatePassiveDamage(string effect)
    {
        float value = 7f;

        int amount = ExtractNumber(effect);
        if (amount > 0) value += amount * 0.2f; 

        return new AbilityScore { category = EffectCategory.PassiveDamage, baseValue = value };
    }

    private static AbilityScore EvaluateDamageReduction(string effect)
    {
        float value = 7f;

        int reduction = ExtractNumber(effect);
        if (reduction > 0) value += reduction * 0.3f; 

        return new AbilityScore { category = EffectCategory.DamageReduction, baseValue = value };
    }

    private static AbilityScore EvaluateDisruption(string effect)
    {
        float value = 12f;

        if (effect.Contains("partidario"))
            value += 5f;

        if (effect.Contains("evolucionar"))
            value += 5f;

        return new AbilityScore { category = EffectCategory.Disruption, baseValue = value };
    }

    private static AbilityScore EvaluateStatusEffect(string effect)
    {
        float value = 6f;

        if (effect.Contains("envenenado")) value += 4f;
        if (effect.Contains("dormido")) value += 3f;
        if (effect.Contains("paralizado")) value += 3f;

        if (effect.Contains("moneda")) value -= 2f;

        return new AbilityScore { category = EffectCategory.StatusEffect, baseValue = value };
    }

    private static AbilityScore EvaluateBenchManipulation(string effect)
    {
        float value = 8f;

        if (effect.Contains("rival") && effect.Contains("activo"))
            value += 4f;

        if (effect.Contains("mover") && effect.Contains("rival"))
            value += 3f;

        return new AbilityScore { category = EffectCategory.BenchManipulation, baseValue = value };
    }

    private static AbilityScore EvaluateDirectDamage(string effect)
    {
        float value = 7f;

        int amount = ExtractNumber(effect);
        if (amount > 0) value += amount * 0.2f;

        return new AbilityScore { category = EffectCategory.PassiveDamage, baseValue = value };
    }


    private static int ExtractNumber(string text)
    {
        Match match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }
}