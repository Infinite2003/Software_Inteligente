using UnityEngine;

[System.Serializable]
public class PokemonInstance
{
    public TCGPCard data;
    public int currentHP;
    public int attachedEnergy = 0;

    public bool evolvedThisTurn;
    public int turnsInPlay;

    public PokemonInstance(TCGPCard Data)
    {

        data = Data;
        currentHP = Data.hp;
        attachedEnergy = 0;
        evolvedThisTurn = false;
        turnsInPlay = 0;
    }

    public void TakeDamage( int damage)
    {

        currentHP -= damage;
    }

    public void Heal(int amount)
    {

        currentHP += amount;

        if(currentHP > data.hp)
        {
            currentHP = data.hp;
        }
    }

    public bool isOk()
    {

        return currentHP <= 0;
    }
}
