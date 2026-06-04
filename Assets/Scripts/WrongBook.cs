using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WrongBooksave
{
    public List<WrongEntry> items = new List<WrongEntry>();
}

public class WrongBook : MonoBehaviour
{
    public static WrongBook Instance { get; private set; }

    private const string SaveKey = "wrong_book_json";
    private WrongBooksave data = new WrongBooksave();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public IReadOnlyList<WrongEntry> GetAll()
    {
        return data.items;
    }

    public WrongEntry GetById(string id)
    {
        for (int i = 0; i < data.items.Count; i++)
        {
            if (data.items[i].id == id)
                return data.items[i];
        }
        return null;
    }

    public void AddOrUpdate(string lang, string prompt, string[] answers)
    {
        AddOrUpdate(lang, prompt, answers, "");
    }

    public void AddOrUpdate(string lang, string prompt, string[] answers, string wrongAnswer)
    {
        string id = (lang + "|" + prompt).Trim();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        for (int i = 0; i < data.items.Count; i++)
        {
            WrongEntry existing = data.items[i];
            if (existing.id != id)
                continue;

            existing.answers = answers ?? Array.Empty<string>();
            existing.lastWrongAnswer = wrongAnswer;
            existing.lastMissedUnix = now;
            existing.timesWrong = Mathf.Max(1, existing.timesWrong + 1);
            existing.reviewCorrectStreak = 0;
            Save();
            return;
        }

        data.items.Add(new WrongEntry
        {
            id = id,
            lang = lang,
            prompt = prompt,
            answers = answers ?? Array.Empty<string>(),
            lastWrongAnswer = wrongAnswer,
            createdAtUnix = now,
            lastMissedUnix = now,
            timesWrong = 1,
            reviewCorrectStreak = 0
        });
        Save();
    }

    public bool MarkReviewCorrect(string id, int requiredStreak)
    {
        for (int i = 0; i < data.items.Count; i++)
        {
            WrongEntry entry = data.items[i];
            if (entry.id != id)
                continue;

            entry.reviewCorrectStreak++;
            if (entry.reviewCorrectStreak >= Mathf.Max(1, requiredStreak))
                data.items.RemoveAt(i);
            Save();
            return true;
        }
        return false;
    }

    public WrongEntry GetRandomForLang(string lang)
    {
        List<WrongEntry> candidates = new List<WrongEntry>();
        for (int i = 0; i < data.items.Count; i++)
        {
            if (data.items[i] != null && data.items[i].lang == lang)
                candidates.Add(data.items[i]);
        }

        if (candidates.Count == 0)
            return null;

        candidates.Sort((a, b) => b.timesWrong.CompareTo(a.timesWrong));
        int limit = Mathf.Min(candidates.Count, 5);
        return candidates[UnityEngine.Random.Range(0, limit)];
    }

    public bool RemoveById(string id)
    {
        for (int i = 0; i < data.items.Count; i++)
        {
            if (data.items[i].id == id)
            {
                data.items.RemoveAt(i);
                Save();
                return true;
            }
        }
        return false;
    }

    public void ClearAll()
    {
        data.items.Clear();
        Save();
    }

    private void Save()
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrWhiteSpace(json))
        {
            data = new WrongBooksave();
            return;
        }

        try
        {
            data = JsonUtility.FromJson<WrongBooksave>(json);
            if (data == null || data.items == null)
                data = new WrongBooksave();
        }
        catch
        {
            data = new WrongBooksave();
        }
    }
}
