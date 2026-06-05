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
    [SerializeField] private string primaryResourcePath = "Data/words";
    [SerializeField] private string cleanFallbackResourcePath = "Data/words_clean";
    [SerializeField] private bool preferCleanFallbackFirst = false;
    [SerializeField] private bool preferCleanFallbackWhenCorrupt = true;
    [SerializeField] private bool mergeInspectorWordsJson = false;
    [SerializeField] private int recentHistoryLimit = 10;

    public Entry[] Entries { get; private set; }

    private readonly Queue<string> recentIds = new Queue<string>();

    private void Awake()
    {
        Load();
    }

    private void Load()
    {
        Entry[] cleanFallback = SanitizeEntries(ReadEntries(Resources.Load<TextAsset>(cleanFallbackResourcePath)), out int cleanSkipped);
        if (preferCleanFallbackFirst)
        {
            if (cleanFallback.Length > 0)
            {
                Entries = cleanFallback;
                Debug.Log("Loaded clean entries count: " + Entries.Length);
                return;
            }
        }

        Entry[] resourcePrimary = SanitizeEntries(ReadEntries(Resources.Load<TextAsset>(primaryResourcePath)), out int resourceSkipped);
        int inspectorSkipped = 0;
        Entry[] inspectorPrimary = resourcePrimary.Length == 0 || mergeInspectorWordsJson
            ? SanitizeEntries(ReadEntries(wordsJson), out inspectorSkipped)
            : Array.Empty<Entry>();
        Entry[] primary = resourcePrimary.Length > 0 ? MergeUnique(resourcePrimary, inspectorPrimary) : inspectorPrimary;
        int primarySkipped = resourceSkipped + inspectorSkipped;

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
                id = string.IsNullOrWhiteSpace(entry.id) ? MakeGeneratedId(entry.lang, prompt, answers) : entry.id.Trim(),
                prompt = prompt,
                questionType = string.IsNullOrWhiteSpace(entry.questionType) ? InferQuestionType(entry) : entry.questionType.Trim(),
                answerMode = string.IsNullOrWhiteSpace(entry.answerMode) ? InferAnswerMode(entry) : entry.answerMode.Trim(),
                primaryAnswer = CleanSingleAnswer(string.IsNullOrWhiteSpace(entry.primaryAnswer) ? PickPrimaryAnswer(entry, answers) : entry.primaryAnswer),
                answers = answers,
                displayAnswer = CleanSingleAnswer(string.IsNullOrWhiteSpace(entry.displayAnswer) ? BuildDisplayAnswer(entry, answers) : entry.displayAnswer),
                kana = CleanSingleAnswer(entry.kana),
                kanji = CleanSingleAnswer(entry.kanji),
                romaji = CleanTextArray(entry.romaji),
                meaning = CleanTextArray(entry.meaning),
                pos = CleanSingleAnswer(entry.pos),
                lang = string.IsNullOrWhiteSpace(entry.lang) ? "jp" : entry.lang.Trim(),
                difficulty = Mathf.Clamp(entry.difficulty, 0, 3),
                level = CleanSingleAnswer(entry.level),
                chapter = CleanSingleAnswer(entry.chapter),
                tags = CleanTextArray(entry.tags),
                hint = CleanSingleAnswer(entry.hint),
                example = CleanSingleAnswer(entry.example),
                exampleMeaning = CleanSingleAnswer(entry.exampleMeaning)
            });
        }

        return result.ToArray();
    }

    public string GetDisplayAnswer(Entry entry)
    {
        if (entry == null)
            return "没有答案";
        if (!string.IsNullOrWhiteSpace(entry.displayAnswer))
            return entry.displayAnswer;
        return BuildDisplayAnswer(entry, entry.answers);
    }

    public string GetHintAnswer(Entry entry)
    {
        if (entry == null)
            return "";
        if (!string.IsNullOrWhiteSpace(entry.primaryAnswer))
            return entry.primaryAnswer.Trim();
        return PickPrimaryAnswer(entry, entry.answers);
    }

    public string GetStudyHint(Entry entry)
    {
        return entry != null && !string.IsNullOrWhiteSpace(entry.hint) ? entry.hint.Trim() : "";
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

    private string CleanSingleAnswer(string value)
    {
        string text = value == null ? "" : value.Trim();
        return IsUsableText(text) ? text : "";
    }

    private string[] CleanTextArray(string[] values)
    {
        if (values == null || values.Length == 0)
            return Array.Empty<string>();

        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>();
        for (int i = 0; i < values.Length; i++)
        {
            string value = CleanSingleAnswer(values[i]);
            if (!string.IsNullOrEmpty(value) && seen.Add(value))
                result.Add(value);
        }
        return result.ToArray();
    }

    private string PickPrimaryAnswer(Entry entry, string[] answers)
    {
        if (answers == null || answers.Length == 0)
            return "";

        string lang = entry != null ? entry.lang : "";
        if (lang == "jp")
        {
            for (int i = 0; i < answers.Length; i++)
            {
                if (LooksLikeRomaji(answers[i]))
                    return answers[i];
            }
        }

        return answers[0];
    }

    private string BuildDisplayAnswer(Entry entry, string[] answers)
    {
        List<string> parts = new List<string>();
        HashSet<string> seen = new HashSet<string>();

        AddDisplayPart(parts, seen, entry != null ? entry.kana : "");
        AddDisplayPart(parts, seen, entry != null ? entry.kanji : "");
        if (entry != null && entry.romaji != null)
        {
            for (int i = 0; i < entry.romaji.Length; i++)
                AddDisplayPart(parts, seen, entry.romaji[i]);
        }

        if (answers != null)
        {
            if (entry != null && entry.lang == "jp")
            {
                for (int i = 0; i < answers.Length; i++)
                {
                    if (ContainsKana(answers[i]))
                        AddDisplayPart(parts, seen, answers[i]);
                }
                for (int i = 0; i < answers.Length; i++)
                {
                    if (!LooksLikeRomaji(answers[i]) && !ContainsKana(answers[i]))
                        AddDisplayPart(parts, seen, answers[i]);
                }
                for (int i = 0; i < answers.Length; i++)
                {
                    if (LooksLikeRomaji(answers[i]))
                        AddDisplayPart(parts, seen, answers[i]);
                }
            }
            else
            {
                for (int i = 0; i < answers.Length; i++)
                    AddDisplayPart(parts, seen, answers[i]);
            }
        }

        return parts.Count > 0 ? string.Join(" / ", parts.ToArray()) : "没有答案";
    }

    private void AddDisplayPart(List<string> parts, HashSet<string> seen, string value)
    {
        string text = CleanSingleAnswer(value);
        if (!string.IsNullOrEmpty(text) && seen.Add(text))
            parts.Add(text);
    }

    private string InferAnswerMode(Entry entry)
    {
        if (entry == null || entry.lang != "jp")
            return "exact_text";
        return "kana_or_romaji";
    }

    private string InferQuestionType(Entry entry)
    {
        if (entry == null)
            return "zh_to_word";
        if (entry.lang == "jp")
            return "zh_to_romaji";
        if (entry.lang == "en")
            return "zh_to_english";
        return "zh_to_word";
    }

    private string MakeGeneratedId(string lang, string prompt, string[] answers)
    {
        string firstAnswer = answers != null && answers.Length > 0 ? answers[0] : "";
        return (string.IsNullOrWhiteSpace(lang) ? "word" : lang.Trim()) + "_" + Mathf.Abs((prompt + "|" + firstAnswer).GetHashCode()).ToString("X");
    }

    private bool LooksLikeRomaji(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        bool hasLetter = false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')
            {
                hasLetter = true;
                continue;
            }
            if (c == '-' || c == '\'' || char.IsWhiteSpace(c))
                continue;
            return false;
        }
        return hasLetter;
    }

    private bool ContainsKana(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            int code = value[i];
            if ((code >= 0x3040 && code <= 0x30FF) || (code >= 0x31F0 && code <= 0x31FF))
                return true;
        }
        return false;
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
