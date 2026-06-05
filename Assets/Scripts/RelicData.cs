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
        new RelicDef { id = "phoenix_leaf", name = "凤羽", description = "首次濒死时回复 2 点生命" },
        new RelicDef { id = "gambler_dice", name = "赌徒骰子", description = "答对时伤害大幅波动" },
        new RelicDef { id = "glass_sword", name = "玻璃剑", description = "满血时伤害 +2，受伤后失效" },
        new RelicDef { id = "repeater", name = "复读机", description = "答错后重答同题，第二次答对不扣血" },
        new RelicDef { id = "wrong_crown", name = "错题王冠", description = "复习题伤害 +3，但答错双倍扣血" },
        new RelicDef { id = "time_bond", name = "时间债券", description = "每题时间 +8 秒，战斗胜利后扣 1 生命" },
        new RelicDef { id = "review_chalice", name = "复习圣杯", description = "复习题答对回复 1 生命" }
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
