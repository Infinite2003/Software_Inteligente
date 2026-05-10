using UnityEngine;
using System.Collections.Generic;


public enum BattlePhase
{

    StartTurn,
    MainPhase,
    AttackPhase,
    EndTurn
}
public class Battle_Manager : MonoBehaviour
{
    public PlayerState player;
    public PlayerState enemy;
    public BattlePhase currentPhase;

    private bool playerTurn = true;

    void Start()
    {

        StartMatch();
    }

    void StartMatch()
    {

        player.Setup();
        enemy.Setup();

        player.Draw(5);
        enemy.Draw(5);

        player.SetActiveBasic();
        enemy.SetActiveBasic();
    }

    public void EndTurn()
    {

        playerTurn = !playerTurn;

        CurrentPlayer().energyThisTurn = 1;
        CurrentPlayer().Draw(1);

        foreach(var pokemon in CurrentPlayer().bench)
        {

            pokemon.turnsInPlay++;
        }

        if (CurrentPlayer().active != null)
            CurrentPlayer().active.turnsInPlay++;
    }

    PlayerState CurrentPlayer()
    {

        return playerTurn ? player : enemy;
    }

    PlayerState Oponent()
    {

        return playerTurn ? enemy : player;
    }

    public void CheckKO(PlayerState owner, PlayerState opponent)
    {

        if (owner.active == null)
            return;

        if (owner.active.isOk())
        {

            owner.Discarded.Add(owner.active.data);

            opponent.prizePoints++;

            Debug.Log(opponent.prizePoints + "puntos");

            if(opponent.prizePoints >= 3)
            {

                Debug.Log("Ganador");
                return;
            }

            if(owner.bench.Count > 0)
            {

                owner.active = owner.bench[0];

                owner.bench.RemoveAt(0);
            }

            else
            {

                Debug.Log("No quedan Pokemon");
            }
        }
    }
}
