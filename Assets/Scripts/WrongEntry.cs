using System;
using UnityEngine;

[Serializable]
public class WrongEntry
{
    // 用来“唯一标识”一条错题，方便去重/删除
    public string id;

    // jp / en
    public string lang;

    // 题干（你显示给玩家看的提示，比如中文释义）
    public string prompt;

    // 正确答案列表（可接受多个）
    public string[] answers;

    // 记录时间（可选，后面做排序用）
    public long createdAtUnix;
}