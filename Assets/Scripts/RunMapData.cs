using System;

public enum MapNodeType
{
    Monster,
    Elite,
    Rest,
    Shop,
    Event,
    Chest,
    Review,
    Boss
}

[Serializable]
public class MapNode
{
    public string id;
    public int depth;
    public int row;
    public MapNodeType type;
    public string nextCsv;
}

[Serializable]
public class RunProgress
{
    public bool active;
    public string lang;
    public int seed;
    public int score;
    public int combo;
    public int kills;
    public int gold;
    public int xp;
    public int level;
    public int playerMaxHp;
    public int playerHp;
    public string completedCsv;
    public string relicCsv;
}
