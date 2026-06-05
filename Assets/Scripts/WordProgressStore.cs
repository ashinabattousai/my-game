using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WordMemory
{
    public string id;
    public int correct;
    public int wrong;
    public int streak;
    public long lastSeenUnix;
}

[Serializable]
public class WordMemorySave
{
    public List<WordMemory> items = new List<WordMemory>();
}

public static class WordProgressStore
{
    private const string SaveKey = "word_quest_memory_v1";
    private static WordMemorySave cache;

    public static WordMemory Get(string lang, string prompt)
    {
        EnsureLoaded();
        string id = MakeId(lang, prompt);
        for (int i = 0; i < cache.items.Count; i++)
        {
            if (cache.items[i].id == id)
                return cache.items[i];
        }

        return new WordMemory { id = id };
    }

    public static void Record(string lang, string prompt, bool correct)
    {
        EnsureLoaded();
        string id = MakeId(lang, prompt);
        WordMemory memory = null;
        for (int i = 0; i < cache.items.Count; i++)
        {
            if (cache.items[i].id == id)
            {
                memory = cache.items[i];
                break;
            }
        }

        if (memory == null)
        {
            memory = new WordMemory { id = id };
            cache.items.Add(memory);
        }

        if (correct)
        {
            memory.correct++;
            memory.streak = Mathf.Max(1, memory.streak + 1);
        }
        else
        {
            memory.wrong++;
            memory.streak = 0;
        }

        memory.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Save();
    }

    public static string Label(string lang, string prompt)
    {
        WordMemory memory = Get(lang, prompt);
        if (memory.correct == 0 && memory.wrong == 0)
            return "新词";
        if (memory.wrong > memory.correct || memory.streak == 0)
            return "需复习";
        if (memory.streak >= 3)
            return "较熟";
        return "学习中";
    }

    public static int MasteredCount()
    {
        EnsureLoaded();
        int count = 0;
        for (int i = 0; i < cache.items.Count; i++)
        {
            if (cache.items[i].streak >= 3)
                count++;
        }
        return count;
    }

    public static int Weight(string lang, string prompt)
    {
        WordMemory memory = Get(lang, prompt);
        if (memory.correct == 0 && memory.wrong == 0)
            return 4;
        if (memory.wrong > memory.correct)
            return 7;
        if (memory.streak == 0)
            return 6;
        if (memory.streak >= 3)
            return 1;
        return 3;
    }

    private static void EnsureLoaded()
    {
        if (cache != null)
            return;

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrWhiteSpace(json))
        {
            cache = new WordMemorySave();
            return;
        }

        try
        {
            cache = JsonUtility.FromJson<WordMemorySave>(json);
            if (cache == null || cache.items == null)
                cache = new WordMemorySave();
        }
        catch
        {
            cache = new WordMemorySave();
        }
    }

    private static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(cache));
        PlayerPrefs.Save();
    }

    private static string MakeId(string lang, string prompt)
    {
        return (lang + "|" + prompt).Trim();
    }
}
