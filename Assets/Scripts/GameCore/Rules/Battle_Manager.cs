using UnityEngine;
using System.Collections.Generic;

public class Battle_Manager : MonoBehaviour
{
    public PlayerState player;
    public PlayerState enemy;

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

        CurrentPlayer().GenerateEnergy();
        CurrentPlayer().Draw(1);
    }

    PlayerState CurrentPlayer()
    {

        return playerTurn ? player : enemy;
    }

    PlayerState Oponent()
    {

        return playerTurn ? enemy : player;
    }
}
