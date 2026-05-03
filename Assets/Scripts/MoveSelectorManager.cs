using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoveSelectorManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public Transform moveListContainer;
    public GameObject moveButtonPrefab;

    public Image[] slotIcons;
    public TMP_Text[] slotNames;

    private int selectedSlot = -1;

    void Start()
    {
        panel.SetActive(false);
        RefreshSlots();
    }

    public void Open()
    {
        panel.SetActive(true);
        BuildMoveList();
        RefreshSlots();
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    void BuildMoveList()
    {
        foreach (Transform child in moveListContainer)
            Destroy(child.gameObject);

        foreach (var move in GameState.Instance.knownMoves)
        {
            GameObject obj = Instantiate(moveButtonPrefab, moveListContainer);

            TMP_Text text = obj.GetComponentInChildren<TMP_Text>();
            text.text = move.name;

            Button btn = obj.GetComponent<Button>();

            btn.onClick.AddListener(() =>
            {
                if (selectedSlot == -1) return;

                EquipMove(selectedSlot, move);
            });
        }
    }

    public void SelectSlot(int slotIndex)
    {
        selectedSlot = slotIndex;
    }

    void EquipMove(int slot, MoveData move)
    {
        while (GameState.Instance.equippedMoves.Count < 4)
            GameState.Instance.equippedMoves.Add(null);

        GameState.Instance.equippedMoves[slot] = move;

        RefreshSlots();
    }

    void RefreshSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            if (i >= GameState.Instance.equippedMoves.Count ||
                GameState.Instance.equippedMoves[i] == null)
            {
                slotNames[i].text = "-";
                slotIcons[i].enabled = false;
                continue;
            }

            var move = GameState.Instance.equippedMoves[i];

            slotNames[i].text = move.name;

            Sprite sprite = Resources.Load<Sprite>("Sprites/Moves/" + move.name);

            slotIcons[i].sprite = sprite;
            slotIcons[i].enabled = sprite != null;
        }
    }
}