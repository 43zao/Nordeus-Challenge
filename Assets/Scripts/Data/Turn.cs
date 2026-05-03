using System.Collections.Generic;

[System.Serializable]
public class TurnRequest
{
    public string run_id;
    public int monster_index;
    public int player_move_index;
    public List<string> known_moves;
    public List<string> equipped_moves;
}

[System.Serializable]
public class TurnResponse
{
    public bool battle_over;
    public string winner;
    public MoveData reward_move;

    public int hero_hp;
    public int monster_hp;

    public string monster_move;

    public StatValues hero_stats;
    public StatValues monster_stats;

    public List<BattleLog> log;
}

[System.Serializable]
public class BattleLog
{
    public string type;
    public string target;
    public string caster;
    public string stat;
    public int value;
    public int duration;
}

[System.Serializable]
public class StatValues
{
    public int attack;
    public int defense;
    public int magic;
}