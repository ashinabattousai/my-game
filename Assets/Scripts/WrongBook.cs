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
        string id = (lang + "|" + prompt).Trim();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        for (int i = 0; i < data.items.Count; i++)
        {
            WrongEntry existing = data.items[i];
            if (existing.id != id)
                continue;

            existing.answers = answers ?? Array.Empty<string>();
            existing.lastMissedUnix = now;
            existing.timesWrong = Mathf.Max(1, existing.timesWrong + 1);
            Save();
            return;
        }

        data.items.Add(new WrongEntry
        {
            id = id,
            lang = lang,
            prompt = prompt,
            answers = answers ?? Array.Empty<string>(),
            createdAtUnix = now,
            lastMissedUnix = now,
            timesWrong = 1
        });
        Save();
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
