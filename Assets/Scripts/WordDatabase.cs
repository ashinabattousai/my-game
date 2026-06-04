using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WordList
{
    public Entry[] entries;
}

public class WordDatabase : MonoBehaviour
{
    [SerializeField] private TextAsset wordsJson;
    [SerializeField] private bool preferCleanFallbackFirst = false;
    [SerializeField] private bool preferCleanFallbackWhenCorrupt = true;
    [SerializeField] private int recentHistoryLimit = 10;

    public Entry[] Entries { get; private set; }

    private readonly Queue<string> recentIds = new Queue<string>();

    private void Awake()
    {
        Load();
    }

    private void Load()
    {
        if (preferCleanFallbackFirst)
        {
            Entry[] cleanEntries = ReadEntries(Resources.Load<TextAsset>("Data/words_clean"));
            if (cleanEntries.Length > 0)
            {
                Entries = cleanEntries;
                Debug.Log("Loaded clean entries count: " + Entries.Length);
                return;
            }
        }

        Entries = ReadEntries(wordsJson);

        if (Entries.Length == 0 || (preferCleanFallbackWhenCorrupt && LooksCorrupt(Entries)))
        {
            Entry[] fallbackEntries = ReadEntries(Resources.Load<TextAsset>("Data/words_clean"));
            if (fallbackEntries.Length > 0)
            {
                Entries = fallbackEntries;
                Debug.Log("Loaded clean fallback entries count: " + Entries.Length);
                return;
            }
        }

        Debug.Log("Loaded entries count: " + Entries.Length);
    }

    private Entry[] ReadEntries(TextAsset source)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.text))
            return Array.Empty<Entry>();

        WordList data = JsonUtility.FromJson<WordList>(source.text);
        return data != null && data.entries != null ? data.entries : Array.Empty<Entry>();
    }

    private bool LooksCorrupt(Entry[] entries)
    {
        int sampleCount = Mathf.Min(entries.Length, 24);
        int suspicious = 0;

        for (int i = 0; i < sampleCount; i++)
        {
            Entry entry = entries[i];
            if (entry == null)
                continue;

            if (LooksLikeMojibake(entry.prompt))
            {
                suspicious++;
                continue;
            }

            if (entry.answers == null)
                continue;

            for (int j = 0; j < entry.answers.Length; j++)
            {
                if (LooksLikeMojibake(entry.answers[j]))
                {
                    suspicious++;
                    break;
                }
            }
        }

        return sampleCount > 0 && suspicious >= Mathf.Max(3, sampleCount / 3);
    }

    private bool LooksLikeMojibake(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.Contains("銇") || value.Contains("涓") || value.Contains("锛") ||
               value.Contains("鐨") || value.Contains("�") || value.Contains("伀");
    }

    public Entry GetRandomEntryByLang(string lang)
    {
        return GetRandomEntryByLangAndDifficulty(lang, 1, 3);
    }

    public Entry GetRandomEntryByLangAndDifficulty(string lang, int minDifficulty, int maxDifficulty)
    {
        if (Entries == null || Entries.Length == 0)
            return null;

        minDifficulty = Mathf.Clamp(minDifficulty, 1, 3);
        maxDifficulty = Mathf.Clamp(maxDifficulty, minDifficulty, 3);

        List<Entry> candidates = new List<Entry>();
        List<Entry> fallback = new List<Entry>();

        for (int i = 0; i < Entries.Length; i++)
        {
            Entry entry = Entries[i];
            if (entry == null || entry.lang != lang)
                continue;

            int difficulty = NormalizeDifficulty(entry);
            if (difficulty < minDifficulty || difficulty > maxDifficulty)
                continue;

            fallback.Add(entry);
            if (!recentIds.Contains(MakeId(entry)))
                candidates.Add(entry);
        }

        if (fallback.Count == 0)
            return null;

        List<Entry> pool = candidates.Count > 0 ? candidates : fallback;
        Entry selected = pool[UnityEngine.Random.Range(0, pool.Count)];
        Remember(selected);
        return selected;
    }

    public Entry GetLearningEntryByLangAndDifficulty(string lang, int minDifficulty, int maxDifficulty)
    {
        if (Entries == null || Entries.Length == 0)
            return null;

        minDifficulty = Mathf.Clamp(minDifficulty, 1, 3);
        maxDifficulty = Mathf.Clamp(maxDifficulty, minDifficulty, 3);

        List<Entry> candidates = new List<Entry>();
        for (int i = 0; i < Entries.Length; i++)
        {
            Entry entry = Entries[i];
            if (entry == null || entry.lang != lang)
                continue;

            int difficulty = NormalizeDifficulty(entry);
            if (difficulty < minDifficulty || difficulty > maxDifficulty)
                continue;

            if (recentIds.Contains(MakeId(entry)))
                continue;

            int weight = Mathf.Max(1, WordProgressStore.Weight(lang, entry.prompt));
            for (int j = 0; j < weight; j++)
                candidates.Add(entry);
        }

        if (candidates.Count == 0)
            return GetRandomEntryByLangAndDifficulty(lang, minDifficulty, maxDifficulty);

        Entry selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        Remember(selected);
        return selected;
    }

    public int CountByLang(string lang)
    {
        if (Entries == null)
            return 0;

        int count = 0;
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i] != null && Entries[i].lang == lang)
                count++;
        }
        return count;
    }

    public int CountByLangAndDifficulty(string lang, int minDifficulty, int maxDifficulty)
    {
        if (Entries == null)
            return 0;

        int count = 0;
        for (int i = 0; i < Entries.Length; i++)
        {
            Entry entry = Entries[i];
            if (entry == null || entry.lang != lang)
                continue;

            int difficulty = NormalizeDifficulty(entry);
            if (difficulty >= minDifficulty && difficulty <= maxDifficulty)
                count++;
        }
        return count;
    }

    private int NormalizeDifficulty(Entry entry)
    {
        if (entry.difficulty > 0)
            return Mathf.Clamp(entry.difficulty, 1, 3);

        int answerLength = 0;
        if (entry.answers != null)
        {
            for (int i = 0; i < entry.answers.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(entry.answers[i]))
                    answerLength = Mathf.Max(answerLength, entry.answers[i].Trim().Length);
            }
        }

        int promptLength = string.IsNullOrWhiteSpace(entry.prompt) ? 0 : entry.prompt.Trim().Length;
        if (answerLength >= 12 || promptLength >= 6)
            return 3;
        if (answerLength >= 7 || promptLength >= 3)
            return 2;
        return 1;
    }

    private string MakeId(Entry entry)
    {
        return entry.lang + "|" + entry.prompt;
    }

    private void Remember(Entry entry)
    {
        recentIds.Enqueue(MakeId(entry));
        while (recentIds.Count > Mathf.Max(1, recentHistoryLimit))
            recentIds.Dequeue();
    }
}
