using UnityEngine;

[System.Serializable]
public class PokemonInstance : MonoBehaviour
{
    public TCGPCard data;
    public int currentHP;
    public int attachedEnergy = 0;

    public PokemonInstance(TCGPCard Data)
    {

        data = Data;
        currentHP = Data.hp;
    }

    public void TakeDamage( int damage)
    {

        currentHP -= damage;
    }

    public bool isOk()
    {

        return currentHP <= 0;
    }
}
