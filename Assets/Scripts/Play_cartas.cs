using System.Collections.Generic;
using UnityEngine;

public class Play_cartas : MonoBehaviour
{
    public GameObject cartaPrefab;   // Tu sprite Carta
    public Transform zonaMazo;      // Punto a la derecha de la pantalla
    public Transform zonaMano;      // Punto donde inicia la mano del jugador

    void CrearMazo()
    {
        for (int i = 0; i < 20; i++)
        {
            // Creamos la carta en la posición del mazo (derecha)
            // Añadimos un pequeño desfase en 'z' o 'y' para que parezca una pila real
            Vector3 posicionPila = zonaMazo.position + new Vector3(0, i * 0.05f, 0);
            GameObject nuevaCarta = Instantiate(cartaPrefab, posicionPila, Quaternion.identity);

            nuevaCarta.name = "Carta" + i;

            // La "desactivamos" o simplemente le quitamos el script de mover 
            // para que no se puedan arrastrar mientras están en el mazo
            nuevaCarta.GetComponent<MoverSprite>().enabled = false;

            mazo.Push(nuevaCarta);
        }
    }

    void RepartirMano()
    {
        for (int i = 0; i < 5; i++)
        {
            if (mazo.Count > 0)
            {
                // Sacamos la carta de arriba de la pila
                GameObject cartaParaMano = mazo.Pop();

                // La movemos a la zona de la mano con espacio entre ellas
                cartaParaMano.transform.position = zonaMano.position + new Vector3(i * 1.5f, 0, 0);

                // Activamos el script para que ahora SÍ se pueda mover con el mouse
                cartaParaMano.GetComponent<MoverSprite>().enabled = true;
            }
        }
    }
}
