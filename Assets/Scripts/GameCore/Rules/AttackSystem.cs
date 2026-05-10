using UnityEngine;

public class AttackSystem 
{
    
    public static bool CanUseMove(PokemonInstance attacker, Move move)
    {

        int requiredEnergy = 0;

        foreach(var cost in move.costs)
        {

            requiredEnergy += cost.amount;
        }

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


        if (target.data.weakness == attacker.data.type)
            damage += 20;

        target.TakeDamage(damage);

        Debug.Log(attacker.data.name + "hizo" + damage + "daño a" + target.data.name);
    }
}
