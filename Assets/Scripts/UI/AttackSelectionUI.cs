using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttackSelectionUI : MonoBehaviour
{
    public GameObject panel;
    public Button botonPrefab;

    public void ShowAttacks(
        PokemonInstance pokemon,
        System.Action<Move> onSelected)
    {
        panel.SetActive(true);

        foreach (Transform child in panel.transform)
            Destroy(child.gameObject);

        foreach (var move in pokemon.data.moves)
        {
            Button btn =
                Instantiate(botonPrefab, panel.transform);

            btn.GetComponentInChildren<TMP_Text>().text =
                move.name;

            btn.onClick.AddListener(() =>
            {
                panel.SetActive(false);
                onSelected(move);
            });
        }
    }
}
