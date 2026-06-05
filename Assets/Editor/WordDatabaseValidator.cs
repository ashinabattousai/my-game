using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WordDatabaseValidator
{
    private const string MainPath = "Assets/Resources/Data/words.json";

    [MenuItem("Word Quest/Validate Word Database")]
    public static void ValidateMainDatabase()
    {
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(MainPath);
        if (asset == null)
        {
            Debug.LogError("Word database not found: " + MainPath);
            return;
        }

        WordList list = JsonUtility.FromJson<WordList>(asset.text);
        if (list == null || list.entries == null)
        {
            Debug.LogError("Word database JSON cannot be parsed: " + MainPath);
            return;
        }

        int errors = 0;
        int warnings = 0;
        HashSet<string> ids = new HashSet<string>();
        HashSet<string> langPrompts = new HashSet<string>();
        StringBuilder report = new StringBuilder();

        for (int i = 0; i < list.entries.Length; i++)
        {
            Entry entry = list.entries[i];
            string label = "entry[" + i + "]";
            if (entry == null)
            {
                errors += Add(report, label, "entry is null");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.id))
                errors += Add(report, label, "id is empty");
            else if (!ids.Add(entry.id))
                errors += Add(report, label, "duplicate id: " + entry.id);

            if (string.IsNullOrWhiteSpace(entry.lang))
                errors += Add(report, label, "lang is empty");
            if (string.IsNullOrWhiteSpace(entry.prompt))
                errors += Add(report, label, "prompt is empty");
            if (entry.answers == null || entry.answers.Length == 0)
                errors += Add(report, label, "answers is empty");
            if (entry.difficulty < 1 || entry.difficulty > 3)
                errors += Add(report, label, "difficulty must be 1-3");
            if (!string.IsNullOrWhiteSpace(entry.id) && !LooksLikeIdForEntry(entry))
                warnings += Add(report, label, "id does not match recommended pattern for lang/level/pos: " + entry.id);

            string langPrompt = entry.lang + "|" + entry.prompt;
            if (!string.IsNullOrWhiteSpace(entry.lang) && !string.IsNullOrWhiteSpace(entry.prompt) && !langPrompts.Add(langPrompt))
                warnings += Add(report, label, "same lang+prompt appears more than once: " + langPrompt);

            if (entry.answers != null)
            {
                for (int j = 0; j < entry.answers.Length; j++)
                {
                    string answer = entry.answers[j];
                    if (string.IsNullOrWhiteSpace(answer))
                        errors += Add(report, label, "answer[" + j + "] is empty");
                    else if (answer != answer.Trim())
                        warnings += Add(report, label, "answer[" + j + "] has leading or trailing spaces");
                }
            }

            if (entry.lang == "jp")
            {
                if (string.IsNullOrWhiteSpace(entry.kana) && !ArrayHasKana(entry.answers))
                    warnings += Add(report, label, "jp entry has no kana");
                if ((entry.romaji == null || entry.romaji.Length == 0) && !ArrayHasRomaji(entry.answers))
                    warnings += Add(report, label, "jp entry has no romaji");
            }
        }

        if (errors == 0 && warnings == 0)
        {
            Debug.Log("Word database validation passed. Entries: " + list.entries.Length);
            return;
        }

        string summary = "Word database validation finished. Entries: " + list.entries.Length + ", errors: " + errors + ", warnings: " + warnings + "\n" + report;
        if (errors > 0)
            Debug.LogError(summary);
        else
            Debug.LogWarning(summary);
    }

    private static int Add(StringBuilder report, string label, string message)
    {
        report.Append(label).Append(": ").Append(message).Append('\n');
        return 1;
    }

    private static bool ArrayHasKana(string[] values)
    {
        if (values == null)
            return false;
        for (int i = 0; i < values.Length; i++)
        {
            if (ContainsKana(values[i]))
                return true;
        }
        return false;
    }

    private static bool ArrayHasRomaji(string[] values)
    {
        if (values == null)
            return false;
        for (int i = 0; i < values.Length; i++)
        {
            if (LooksLikeRomaji(values[i]))
                return true;
        }
        return false;
    }

    private static bool ContainsKana(string value)
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

    private static bool LooksLikeRomaji(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        bool hasLetter = false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
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

    private static bool LooksLikeIdForEntry(Entry entry)
    {
        string id = entry.id.ToLowerInvariant();
        string pos = string.IsNullOrWhiteSpace(entry.pos) ? "word" : entry.pos.ToLowerInvariant();
        string level = string.IsNullOrWhiteSpace(entry.level) ? "" : entry.level.ToLowerInvariant();
        if (entry.lang == "jp")
            return id.StartsWith("jp_" + level + "_" + pos + "_");
        if (entry.lang == "en")
            return id.StartsWith("en_");
        return true;
    }
}
