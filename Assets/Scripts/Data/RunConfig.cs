using System.Collections.Generic;

[System.Serializable]
public class RunConfig
{
    public string run_id;
    public HeroData hero;
    public List<MonsterData> monsters;
}

[System.Serializable]
public class HeroData
{
    public string name;
    public StatsData stats;
    public List<MoveData> moves;
}

[System.Serializable]
public class MonsterData
{
    public string name;
    public StatsData stats;
    public List<MoveData> moves;
}

[System.Serializable]
public class StatsData
{
    public int hp;
    public int attack;
    public int defense;
    public int magic;
}

[System.Serializable]
public class MoveData
{
    public string name;
    public List<EffectData> effects;
}

[System.Serializable]
public class EffectData
{
    public string type;
    public string target;
    public string stat;
    public float value;
    public int duration;
}