using System.Collections.Generic;
using UnityEngine;

public class Play_cartas : MonoBehaviour
{

    [SerializeField] private List<TCGPCard> miMazo;
    

    void CrearMazo()
    {
        miMazo = CardGameManager._instance.CreateDeck();
        foreach (var c in miMazo)
        {

        }
    }
}
    