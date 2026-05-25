using UnityEngine;

public class AttackSystem 
{
    
    public static bool CanUseMove(PokemonInstance attacker, Move move)
    {

        int requiredEnergy = 0;

        if (move.cost != null)
            requiredEnergy = move.cost.Count;

        return attacker.attachedEnergy >= requiredEnergy;
    }

    public static void UseMove(PokemonInstance attacker, PokemonInstance target, Move move)
    {

        if(!CanUseMove(attacker, move))
        {

            Debug.Log("No hay suficiente energia");
            return;
        }

        int damage = 0;

        int.TryParse(move.damage, out damage);


        if (target.data.weakness != null && target.data.weakness.type == attacker.data.type)
            damage += 20;

        target.TakeDamage(damage);

        Debug.Log(attacker.data.name + "hizo" + damage + "daño a" + target.data.name);
    }
}
