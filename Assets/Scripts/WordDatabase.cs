using System;
using UnityEngine;

[Serializable]
public class WordList
{
    public Entry[] entries;
}

public class WordDatabase : MonoBehaviour
{
    [SerializeField] private TextAsset wordsJson;

    public Entry[] Entries { get; private set; } = Array.Empty<Entry>();

    private void Awake()
    {
        if (wordsJson == null)
        {
            Debug.LogError("wordsJson not assigned.");
            return;
        }

        WordList data = JsonUtility.FromJson<WordList>(wordsJson.text);
        Entries = data != null && data.entries != null ? data.entries : Array.Empty<Entry>();
        Debug.Log("Loaded entries count: " + Entries.Length);
    }

    public Entry GetRandomEntryByLang(string lang)
    {
        if (Entries == null || Entries.Length == 0)
            return null;

        int count = 0;
        for (int i = 0; i < Entries.Length; i++)
            if (Entries[i] != null && Entries[i].lang == lang)
                count++;

        if (count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, count);
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i] == null || Entries[i].lang != lang)
                continue;

            if (index == 0)
                return Entries[i];
            index--;
        }

        return null;
    }
}
