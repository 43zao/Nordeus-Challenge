using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public Image playerSprite;
    public Image monsterSprite;

    public Slider playerHpBar;
    public Slider monsterHpBar;

    public TMP_Text playerStatsText;
    public TMP_Text monsterStatsText;

    public Button[] playerMoveButtons;
    public TMP_Text[] playerMoveTexts;

    public TMP_Text battleLogText;

    public LevelUpManager levelUpManager;

    private HeroData hero;
    private MonsterData monster;

    private int currentPlayerHP;
    private int currentMonsterHP;
    private int currentPlayerAttack;
    private int currentPlayerDefense;
    private int currentPlayerMagic;

    private int currentMonsterAttack;
    private int currentMonsterDefense;
    private int currentMonsterMagic;

    private int turnNumber = 1;
    private int currentSelectedMoveIndex;

    private Queue<string> logQueue = new Queue<string>();
    private const int MAX_LOG_LINES = 12;

    void Start()
    {
        hero = GameState.Instance.currentRun.hero;

        monster = GameState.Instance.GetSelectedMonster();

        currentPlayerHP = hero.stats.hp;
        currentMonsterHP = monster.stats.hp;

        currentPlayerAttack = hero.stats.attack;
        currentPlayerDefense = hero.stats.defense;
        currentPlayerMagic = hero.stats.magic;

        currentMonsterAttack = monster.stats.attack;
        currentMonsterDefense = monster.stats.defense;
        currentMonsterMagic = monster.stats.magic;

        SetupBattle();
    }

    void SetupBattle()
    {
        // sprites
        Sprite[] rogueSprites =
            Resources.LoadAll<Sprite>("Sprites/Characters/rogues");

        if (rogueSprites.Length > 1)
        {
            playerSprite.sprite = rogueSprites[1];
        }
        else
        {
            Debug.LogError("Rogue sprite sheet missing or not sliced correctly!");
        }

        Sprite[] monsterSprites =
            Resources.LoadAll<Sprite>("Sprites/Characters/monsters");

        Sprite monsterSpriteFinal = null;

        switch (monster.name)
        {
            case "Witch":
                monsterSpriteFinal = monsterSprites.Length > 30 ? monsterSprites[30] : null;
                break;

            case "Giant Spider":
                monsterSpriteFinal = monsterSprites.Length > 39 ? monsterSprites[39] : null;
                break;

            case "Dragon":
                monsterSpriteFinal = monsterSprites.Length > 45 ? monsterSprites[45] : null;
                break;

            case "Goblin Warrior":
                monsterSpriteFinal = monsterSprites.Length > 2 ? monsterSprites[2] : null;
                break;

            case "Goblin Mage":
                monsterSpriteFinal = monsterSprites.Length > 1 ? monsterSprites[1] : null;
                break;
        }

        monsterSprite.sprite = monsterSpriteFinal;

        if (monsterSpriteFinal == null)
        {
            Debug.LogError("Missing monster sprite for: " + monster.name);
        }

        // hp bars
        playerHpBar.maxValue = hero.stats.hp;
        playerHpBar.value = currentPlayerHP;

        monsterHpBar.maxValue = monster.stats.hp;
        monsterHpBar.value = currentMonsterHP;

        // stats
        UpdateStatsUI();

        // move buttons
        var equippedMoves = GameState.Instance.equippedMoves;

        for (int i = 0; i < equippedMoves.Count; i++)
        {
            int moveIndex = i;

            playerMoveTexts[i].text = equippedMoves[i].name;

            Image icon = playerMoveButtons[i].transform.Find("Icon").GetComponent<Image>();

            Sprite sprite =
                Resources.Load<Sprite>("Sprites/Moves/" + equippedMoves[i].name);

            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.enabled = true;
            }
            else
            {
                icon.enabled = false;
                Debug.LogWarning("Missing move icon: " + equippedMoves[i].name);
            }

            playerMoveButtons[i].onClick.AddListener(() =>
            {
                StartCoroutine(HandleTurn(moveIndex));
            });
        }

        AppendLog("Battle started vs " + monster.name);
    }

    void UpdateStatsUI()
    {
        playerStatsText.text =
            $"HP: {currentPlayerHP}/{hero.stats.hp}\n" +
            $"ATK: {currentPlayerAttack}\n" +
            $"DEF: {currentPlayerDefense}\n" +
            $"MAG: {currentPlayerMagic}";

        monsterStatsText.text =
            $"HP: {currentMonsterHP}/{monster.stats.hp}\n" +
            $"ATK: {currentMonsterAttack}\n" +
            $"DEF: {currentMonsterDefense}\n" +
            $"MAG: {currentMonsterMagic}";
    }

    IEnumerator HandleTurn(int playerMoveIndex)
    {
        DisableButtons();

        currentSelectedMoveIndex = playerMoveIndex;

        yield return ApiClient.Instance.SendTurn(
            playerMoveIndex,
            OnTurnResponse
        );
    }

    void HandleReward(MoveData rewardMove)
    {
        bool alreadyKnown = false;

        foreach (var move in GameState.Instance.knownMoves)
        {
            if (move.name == rewardMove.name)
            {
                alreadyKnown = true;
                break;
            }
        }

        if (!alreadyKnown)
        {
            GameState.Instance.knownMoves.Add(rewardMove);
            AppendLog("Learned new move: " + rewardMove.name);
        }
        else
        {
            AppendLog("Duplicate move ignored: " + rewardMove.name);
        }

        // refresh hook for UI
        var loadout = FindAnyObjectByType<MoveLoadoutManager>();
        if (loadout != null) loadout.Refresh();
    }

    void OnTurnResponse(TurnResponse response)
    {
        currentPlayerHP = response.hero_hp;
        currentMonsterHP = response.monster_hp;

        currentPlayerAttack = response.hero_stats.attack;
        currentPlayerDefense = response.hero_stats.defense;
        currentPlayerMagic = response.hero_stats.magic;

        currentMonsterAttack = response.monster_stats.attack;
        currentMonsterDefense = response.monster_stats.defense;
        currentMonsterMagic = response.monster_stats.magic;

        playerHpBar.value = currentPlayerHP;
        monsterHpBar.value = currentMonsterHP;

        UpdateStatsUI();

        // logging
        AppendLog($"\n- TURN {turnNumber} -");
        AppendLog(" ");

        AppendLog("HERO TURN:");
        AppendLog(
            "Knight used " +
            GameState.Instance.equippedMoves[currentSelectedMoveIndex].name
        );

        var playerMove =
            GameState.Instance.equippedMoves[currentSelectedMoveIndex];

        int playerEffectCount = playerMove.effects.Count;

        for (int i = 0; i < response.log.Count; i++)
        {
            // once all player effects are printed,
            // everything after belongs to monster
            if (i == playerEffectCount)
            {
                AppendLog(" ");
                AppendLog("MONSTER TURN:");
                AppendLog(monster.name + " used " + response.monster_move);
            }

            AppendLog(FormatLog(response.log[i]));
        }

        if (response.battle_over)
        {
            AppendLog("\n" + response.winner + " wins!");

            if (response.winner == "player" && response.reward_move != null)
            {
                HandleReward(response.reward_move);
            }

            HandleBattleEnd(response.winner);
            return;
        }

        turnNumber++;

        EnableButtons();
    }

    string FormatLog(BattleLog log)
    {
        switch (log.type)
        {
            case "damage":
                return $"{log.target} took {log.value} damage.";

            case "heal":
                return $"{log.target} healed for {log.value} HP.";

            case "buff":
                return $"{log.target}'s {log.stat} increased by {log.value} for {log.duration} turns.";

            case "debuff":
                return $"{log.target}'s {log.stat} decreased by {Mathf.Abs(log.value)} for {log.duration} turns.";

            case "self_damage":
                return $"{log.caster} took {log.value} self-damage.";

            default:
                return $"{log.type}: {log.value}";
        }
    }

    void AppendLog(string text)
    {
        logQueue.Enqueue(text);

        if (logQueue.Count > MAX_LOG_LINES)
        {
            logQueue.Dequeue();
        }

        battleLogText.text = string.Join("\n", logQueue);
    }

    void HandleBattleEnd(string winner)
    {
        Debug.Log("Winner: " + winner);

        if (winner == "player")
        {
            int monsterIndex = GameState.Instance.selectedMonsterIndex;

            GameState.Instance.MarkMonsterDefeated(monsterIndex);

            int xp = GameState.Instance.GetXPForMonster(monsterIndex);
            GameState.Instance.AddXP(xp);

            AppendLog("Gained " + xp + " XP!");

            if (GameState.Instance.levelUpPending)
            {
                levelUpManager.TryOpen();
                return;
            }

            SceneManager.LoadScene("LevelSelect");
        }
        else
        {
            SceneManager.LoadScene("LevelSelect");
        }
    }

    void DisableButtons()
    {
        foreach (Button b in playerMoveButtons)
            b.interactable = false;
    }

    void EnableButtons()
    {
        foreach (Button b in playerMoveButtons)
            b.interactable = true;
    }
}