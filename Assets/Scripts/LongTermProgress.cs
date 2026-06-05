using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LongTermProgressSave
{
    public string lastPlayDate;
    public int streakDays;
    public int todayCorrect;
    public int todayRewardClaimed;
    public int totalCorrect;
    public int routesCleared;
    public List<string> defeatedBosses = new List<string>();
    public List<string> collectedRelics = new List<string>();
}

public static class LongTermProgress
{
    private const string SaveKey = "word_quest_long_term_v1";
    private static LongTermProgressSave cache;

    public static void RecordCorrect()
    {
        EnsureLoaded();
        cache.todayCorrect++;
        cache.totalCorrect++;
        Save();
    }

    public static void RecordBoss(string bossName)
    {
        EnsureLoaded();
        if (!string.IsNullOrWhiteSpace(bossName) && !cache.defeatedBosses.Contains(bossName))
            cache.defeatedBosses.Add(bossName);
        Save();
    }

    public static void RecordRelic(string relicId)
    {
        EnsureLoaded();
        if (!string.IsNullOrWhiteSpace(relicId) && !cache.collectedRelics.Contains(relicId))
            cache.collectedRelics.Add(relicId);
        Save();
    }

    public static void RecordRouteClear()
    {
        EnsureLoaded();
        cache.routesCleared++;
        Save();
    }

    public static bool TryClaimDailyReward(out string message)
    {
        EnsureLoaded();
        if (cache.todayCorrect >= 30 && cache.todayRewardClaimed == 0)
        {
            cache.todayRewardClaimed = 1;
            Save();
            message = "今日目标完成: 正确 30 题";
            return true;
        }

        message = "今日正确 " + cache.todayCorrect + "/30";
        return false;
    }

    public static string Summary()
    {
        EnsureLoaded();
        return "今日 " + cache.todayCorrect + "/30  掌握 " + WordProgressStore.MasteredCount() +
               "  连续 " + cache.streakDays + " 天  Boss " + cache.defeatedBosses.Count;
    }

    private static void EnsureLoaded()
    {
        if (cache != null)
        {
            RefreshDate();
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrWhiteSpace(json))
            cache = new LongTermProgressSave();
        else
            cache = JsonUtility.FromJson<LongTermProgressSave>(json);

        if (cache == null)
            cache = new LongTermProgressSave();
        if (cache.defeatedBosses == null)
            cache.defeatedBosses = new List<string>();
        if (cache.collectedRelics == null)
            cache.collectedRelics = new List<string>();

        RefreshDate();
    }

    private static void RefreshDate()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (cache.lastPlayDate == today)
            return;

        if (!string.IsNullOrWhiteSpace(cache.lastPlayDate) &&
            DateTime.TryParse(cache.lastPlayDate, out DateTime last) &&
            (DateTime.Now.Date - last.Date).TotalDays <= 1.1)
            cache.streakDays++;
        else
            cache.streakDays = 1;

        cache.lastPlayDate = today;
        cache.todayCorrect = 0;
        cache.todayRewardClaimed = 0;
        Save();
    }

    private static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(cache));
        PlayerPrefs.Save();
    }
}
