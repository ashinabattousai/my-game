using System;

[Serializable]
public class RelicDef
{
    public string id;
    public string name;
    public string description;
}

public static class RelicCatalog
{
    public static readonly RelicDef[] All =
    {
        new RelicDef { id = "hourglass", name = "沙漏", description = "每题时间 +3 秒" },
        new RelicDef { id = "amulet", name = "护身符", description = "每场战斗第一次答错不扣血" },
        new RelicDef { id = "combo_sword", name = "连击剑", description = "每 3 连击额外 +1 伤害" },
        new RelicDef { id = "review_book", name = "复习之书", description = "需复习词额外 +1 伤害" },
        new RelicDef { id = "coin_purse", name = "钱袋", description = "战斗金币奖励 +50%" },
        new RelicDef { id = "phoenix_leaf", name = "凤羽", description = "首次濒死时回复 2 点生命" }
    };

    public static RelicDef Find(string id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].id == id)
                return All[i];
        }
        return null;
    }
}
