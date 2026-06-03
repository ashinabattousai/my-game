using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WrongBookSave
{
    public List<WrongEntry> items = new List<WrongEntry>();
}

public class WrongBook : MonoBehaviour
{
    public static WrongBook Instance { get; private set; }

    private const string SaveKey = "wrong_book_json";
    private WrongBookSave data = new WrongBookSave();

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

    public void AddOrUpdate(string lang, string prompt, string[] answers)
    {
        string id = (lang + "|" + prompt).Trim();
        for (int i = 0; i < data.items.Count; i++)
            if (data.items[i].id == id)
                return;

        WrongEntry entry = new WrongEntry
        {
            id = id,
            lang = lang,
            prompt = prompt,
            answers = answers ?? Array.Empty<string>(),
            createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        data.items.Add(entry);
        Save();
    }

    public bool RemoveById(string id)
    {
        for (int i = 0; i < data.items.Count; i++)
        {
            if (data.items[i].id != id)
                continue;

            data.items.RemoveAt(i);
            Save();
            return true;
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
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            data = new WrongBookSave();
            return;
        }

        try
        {
            data = JsonUtility.FromJson<WrongBookSave>(json);
            if (data == null || data.items == null)
                data = new WrongBookSave();
        }
        catch
        {
            data = new WrongBookSave();
        }
    }
}
