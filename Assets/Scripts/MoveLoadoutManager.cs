using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoveLoadoutManager : MonoBehaviour
{
    [Header("UI Slots (4 slots)")]
    public Image[] moveIcons;
    public TMP_Text[] moveNames;

    private HeroData hero;

    void Start()
    {
        hero = GameState.Instance.currentRun.hero;

        if (hero == null)
        {
            Debug.LogError("Hero is null in GameState!");
            return;
        }

        SetupLoadout();
    }

    void SetupLoadout()
    {
        for (int i = 0; i < moveIcons.Length; i++)
        {
            if (i >= GameState.Instance.equippedMoves.Count)
            {
                moveIcons[i].enabled = false;
                moveNames[i].text = "";
                continue;
            }

            var move = GameState.Instance.equippedMoves[i];

            // TEXT
            moveNames[i].text = move.name;

            // SPRITE LOAD (from Resources)
            Sprite sprite = Resources.Load<Sprite>("Sprites/Moves/" + move.name);

            if (sprite == null)
            {
                Debug.LogWarning("Missing sprite for move: " + move.name);
            }

            moveIcons[i].sprite = sprite;
            moveIcons[i].enabled = sprite != null;
        }
    }

    public void Refresh()
    {
        SetupLoadout();
    }
}