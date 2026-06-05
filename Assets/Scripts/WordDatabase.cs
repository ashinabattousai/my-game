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
        Entry[] cleanFallback = SanitizeEntries(ReadEntries(Resources.Load<TextAsset>("Data/words_clean")), out int cleanSkipped);
        if (preferCleanFallbackFirst)
        {
            if (cleanFallback.Length > 0)
            {
                Entries = cleanFallback;
                Debug.Log("Loaded clean entries count: " + Entries.Length);
                return;
            }
        }

        Entry[] primary = SanitizeEntries(ReadEntries(wordsJson), out int primarySkipped);

        if (primary.Length == 0 || (preferCleanFallbackWhenCorrupt && LooksCorrupt(primary)))
        {
            if (cleanFallback.Length > 0)
            {
                Entries = cleanFallback;
                Debug.Log("Loaded clean fallback entries count: " + Entries.Length);
                return;
            }
        }

        Entries = MergeUnique(primary, cleanFallback);
        Debug.Log("Loaded entries count: " + Entries.Length + ", skipped bad entries: " + (primarySkipped + cleanSkipped));
    }

    private Entry[] ReadEntries(TextAsset source)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.text))
            return Array.Empty<Entry>();

        WordList data = JsonUtility.FromJson<WordList>(source.text);
        return data != null && data.entries != null ? data.entries : Array.Empty<Entry>();
    }

    private Entry[] SanitizeEntries(Entry[] entries, out int skipped)
    {
        skipped = 0;
        if (entries == null || entries.Length == 0)
            return Array.Empty<Entry>();

        List<Entry> result = new List<Entry>(entries.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry == null)
            {
                skipped++;
                continue;
            }

            string prompt = CleanPrompt(entry.prompt);
            string[] answers = CleanAnswers(entry.answers);
            if (!IsUsableText(prompt) || answers.Length == 0)
            {
                skipped++;
                continue;
            }

            result.Add(new Entry
            {
                prompt = prompt,
                answers = answers,
                lang = string.IsNullOrWhiteSpace(entry.lang) ? "jp" : entry.lang.Trim(),
                difficulty = Mathf.Clamp(entry.difficulty, 0, 3)
            });
        }

        return result.ToArray();
    }

    private Entry[] MergeUnique(Entry[] primary, Entry[] fallback)
    {
        List<Entry> result = new List<Entry>();
        HashSet<string> seen = new HashSet<string>();
        AddEntries(primary, result, seen);
        AddEntries(fallback, result, seen);
        return result.ToArray();
    }

    private void AddEntries(Entry[] source, List<Entry> result, HashSet<string> seen)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            Entry entry = source[i];
            if (entry == null)
                continue;

            string key = MakeId(entry);
            if (seen.Add(key))
                result.Add(entry);
        }
    }

    private string CleanPrompt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string prompt = value.Trim();
        prompt = prompt.Replace("【", "").Replace("】", " ");
        string[] prefixes =
        {
            "专有词", "惯用语", "自动词", "他动词", "形容动词", "サ变动词",
            "名词", "动词", "形容词", "副词", "连体词", "接续词",
            "名", "动", "形", "副", "感", "接", "代", "助"
        };

        bool removed;
        do
        {
            removed = false;
            for (int i = 0; i < prefixes.Length; i++)
            {
                string prefix = prefixes[i];
                if (prompt == prefix)
                    return "";
                if (prompt.StartsWith(prefix + " "))
                {
                    prompt = prompt.Substring(prefix.Length).Trim();
                    removed = true;
                    break;
                }
                if (prompt.StartsWith(prefix + ":") || prompt.StartsWith(prefix + "："))
                {
                    prompt = prompt.Substring(prefix.Length + 1).Trim();
                    removed = true;
                    break;
                }
            }
        } while (removed);

        while (prompt.Contains("  "))
            prompt = prompt.Replace("  ", " ");
        return prompt;
    }

    private string[] CleanAnswers(string[] answers)
    {
        if (answers == null || answers.Length == 0)
            return Array.Empty<string>();

        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>();
        for (int i = 0; i < answers.Length; i++)
        {
            string answer = answers[i] == null ? "" : answers[i].Trim();
            if (!IsUsableText(answer))
                continue;
            if (seen.Add(answer))
                result.Add(answer);
        }
        return result.ToArray();
    }

    private bool IsUsableText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (LooksLikeMojibake(value) || value.Contains("□"))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsControl(value[i]) && !char.IsWhiteSpace(value[i]))
                return false;
        }
        return true;
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
