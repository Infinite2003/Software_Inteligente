using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerState : MonoBehaviour
{

    public List<TCGPCard> deck = new List<TCGPCard>();
    public List<TCGPCard> hand = new List<TCGPCard>();
    public List<TCGPCard> Discarded = new List<TCGPCard>();

    public PokemonInstance active;
    public List<PokemonInstance> bench = new List<PokemonInstance>();

    public int prizePoints = 0;
    public int energyThisTurn = 0;

    public void Setup()
    {


    }


    public void Draw(int amount)
    {
        
        for(int i = 0; i < amount; i++)
        {

            if (deck.Count == 0) return;

            hand.Add(deck[0]);
            deck.RemoveAt(0);
        }
    }
}
