using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform buttonContainer;
    public MoveSelectorManager selector;

    private void Start()
    {
        var monsters = GameState.Instance.currentRun.monsters;

        for (int i = 0; i < monsters.Count; i++)
        {
            int index = i;

            GameObject buttonObj =
                Instantiate(buttonPrefab, buttonContainer);

            Button button =
                buttonObj.GetComponent<Button>();

            TMP_Text text =
                buttonObj.GetComponentInChildren<TMP_Text>();


            bool unlocked =
                index <= GameState.Instance.highestUnlockedMonster;

            bool beaten =
                GameState.Instance.defeatedMonsters.Contains(index);


            // text setup
            text.text = $"Level {i + 1} - {monsters[i].name}";

            if (beaten)
            {
                // text.text += " - CLEARED";
            }


            // lock logic
            if (!unlocked)
            {
                button.interactable = false;

                ColorBlock colors = button.colors;
                colors.disabledColor = Color.gray;
                button.colors = colors;
            }
            else
            {
                button.onClick.AddListener(() =>
                {
                    SelectMonster(index);
                });
            }
        }
    }

    public void SelectMonster(int monsterIndex)
    {
        GameState.Instance.selectedMonsterIndex = monsterIndex;

        Debug.Log(
            "Selected monster: " +
            GameState.Instance.GetSelectedMonster().name
        );

        SceneManager.LoadScene("Battle");
    }

    public void StartLevelUpUI()
    {
        
    }
}