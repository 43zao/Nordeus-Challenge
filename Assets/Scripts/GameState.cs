using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    public RunConfig currentRun;
    public int selectedMonsterIndex = -1;

    public HashSet<int> defeatedMonsters = new HashSet<int>();

    public int highestUnlockedMonster = 0;

    public List<MoveData> knownMoves = new List<MoveData>();
    public List<MoveData> equippedMoves = new List<MoveData>();

    public int level = 1;
    public int currentXP = 0;
    public bool levelUpPending = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetConfig(string json)
    {
        currentRun = JsonConvert.DeserializeObject<RunConfig>(json);

        Debug.Log("Run ID: " + currentRun.run_id);
        Debug.Log("Hero: " + currentRun.hero.name);
        Debug.Log("Monster count: " + currentRun.monsters.Count);

        foreach (var monster in currentRun.monsters)
        {
            Debug.Log("Monster loaded: " + monster.name);
        }

        knownMoves.Clear();
        equippedMoves.Clear();

        knownMoves.Clear();
        equippedMoves.Clear();

        foreach (var move in currentRun.hero.moves)
        {
            knownMoves.Add(move);
        }

        for (int i = 0; i < 4; i++)
        {
            if (i < currentRun.hero.moves.Count)
            {
                equippedMoves.Add(currentRun.hero.moves[i]);
            }
            else
            {
                equippedMoves.Add(null);
            }
        }

        Debug.Log("Run initialized.");
    }

    public MonsterData GetSelectedMonster()
    {
        if (selectedMonsterIndex < 0 || selectedMonsterIndex >= currentRun.monsters.Count)
        {
            Debug.LogError("Invalid monster selection.");
            return null;
        }

        return currentRun.monsters[selectedMonsterIndex];
    }

    public void MarkMonsterDefeated(int monsterIndex)
    {
        defeatedMonsters.Add(monsterIndex);

        if (monsterIndex == highestUnlockedMonster &&
            highestUnlockedMonster < currentRun.monsters.Count - 1)
        {
            highestUnlockedMonster++;
        }
    }

    public int GetXPForMonster(int monsterIndex)
    {
        return monsterIndex + 1;    // 1–5 XP
    }

    public int GetXPRequiredForNextLevel()
    {
        return level;
    }

    public void AddXP(int amount)
    {
        currentXP += amount;

        while (currentXP >= GetXPRequiredForNextLevel())
        {
            currentXP -= GetXPRequiredForNextLevel();
            LevelUp();
        }
    }

    void LevelUp()
    {
        level++;
        levelUpPending = true;

        Debug.Log("LEVEL UP! Now level " + level);
    }

}