using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelUpManager : MonoBehaviour
{
    public GameObject panel;

    public Button attackButton;
    public Button defenseButton;
    public Button magicButton;
    public Button hpButton;

    public TMP_Text levelText;

    void Start()
    {
        panel.SetActive(false);

        attackButton.onClick.AddListener(() => Choose("attack"));
        defenseButton.onClick.AddListener(() => Choose("defense"));
        magicButton.onClick.AddListener(() => Choose("magic"));
        hpButton.onClick.AddListener(() => Choose("hp"));

        TryOpen();
    }

    public void TryOpen()
    {
        if (!GameState.Instance.levelUpPending)
            return;

        panel.SetActive(true);

        levelText.text = "LEVEL UP - " + GameState.Instance.level;
    }

    void Choose(string stat)
    {
        GameState.Instance.ApplyStatUpgrade(stat);

        panel.SetActive(false);

        SceneManager.LoadScene("LevelSelect");
    }
}