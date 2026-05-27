using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class CartaVisual : MonoBehaviour
{
    [Header("Textos Principales")]
    public TextMeshProUGUI nombreTexto;
    public TextMeshProUGUI hpTexto;
    public TextMeshProUGUI descripcionTexto;

    [Header("Iconos y Tipos")]
    public TextMeshProUGUI textoCategoria;

    // Cambiamos 'CartaDatos' por tu clase real 'TCGPCard'
    public void Configurar(TCGPCard datos)
    {
        // Asignamos los nombres exactos de tus variables
        nombreTexto.text = datos.name;

        // HP: Solo se muestra si es mayor a 0
        if (hpTexto != null)
            hpTexto.text = datos.hp > 0 ? "HP " + datos.hp.ToString() : "";

        // Prioridad: Si tiene efecto (Trainer), mostrar efecto. Si no, descripción.
        if (descripcionTexto != null)
            descripcionTexto.text = !string.IsNullOrEmpty(datos.effect) ? datos.effect : datos.description;

        if (textoCategoria != null)
            textoCategoria.text = $"{datos.category} / {datos.sub_category}";

        // Ejemplo simple para los movimientos
        if (datos.moves != null && datos.moves.Count > 0)
        {
            // Esto junta los nombres de los ataques: "Placaje, Gruñido"
            string listaAtaques = string.Join(", ", datos.moves.Select(m => m.name));
            Debug.Log($"Ataques cargados en {datos.name}: {listaAtaques}");
        }
    }
}
