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
            Image icon = obj.GetComponentInChildren<Image>();
            text.text = move.name;
            Sprite sprite = Resources.Load<Sprite>("Sprites/Moves/" + move.name);


            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            Button btn = obj.GetComponent<Button>();
            bool alreadyEquipped =
            GameState.Instance.equippedMoves.Contains(move);

            btn.interactable = !alreadyEquipped;

            MoveData capturedMove = move;

            btn.onClick.AddListener(() =>
            {
                if (selectedSlot == -1) return;

                EquipMove(selectedSlot, capturedMove);
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

        RefreshMoveListAvailability();
    }

    void RefreshMoveListAvailability()
{
        foreach (Transform child in moveListContainer)
        {
            Button btn = child.GetComponent<Button>();
            TMP_Text text = child.GetComponentInChildren<TMP_Text>();

            if (text == null || btn == null) continue;

            string moveName = text.text;

            bool alreadyEquipped = false;

            foreach (var m in GameState.Instance.equippedMoves)
            {
                if (m != null && m.name == moveName)
                {
                    alreadyEquipped = true;
                    break;
                }
            }

            btn.interactable = !alreadyEquipped;
        }
    }
}