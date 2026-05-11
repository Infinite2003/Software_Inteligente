using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerState 
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

        ShuffleDeck();
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

    public void ShuffleDeck()
    {

        for(int i = 0; i < deck.Count; i++)
        {

            TCGPCard temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }
    }

    public void SetActiveBasic()
    {

        foreach(var card in hand)
        {

            if(card.category =="Pokemon" && card.sub_category == "Basic")
            {

                active = new PokemonInstance(card);
                hand.Remove(card);
                return;
            }
        }
    }

    public bool AddToBench(TCGPCard card)
    {

        if(bench.Count >= 3)
            return false;

        if (card.category != "Pokemon")
            return false;

        if (card.sub_category != "Basic")
            return false;

        PokemonInstance pokemon = new PokemonInstance(card);

        bench.Add(pokemon);

        hand.Remove(card);

        return true;
    }

    public bool AttachEnergy(PokemonInstance target)
    {

        if (energyThisTurn <= 0)
            return false;

        target.attachedEnergy++;

        energyThisTurn--;

        return true;
    }

    public bool Retreat(int benchIndex)
    {

        if (benchIndex >= bench.Count)
            return false;

        if (active.attachedEnergy < active.data.retreat_cost)
            return false;

        active.attachedEnergy -= active.data.retreat_cost;

        PokemonInstance temp = active;

        active = bench[benchIndex];

        bench[benchIndex] = temp;

        return true;
    }
}
