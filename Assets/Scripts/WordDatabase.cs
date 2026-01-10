using System;
using UnityEngine;

[Serializable]
public class WordList
{
    public string[] words;
}

public class WordDatabase : MonoBehaviour
{
    [SerializeField] private TextAsset wordsJson;

    public string[] Words { get; private set; }  // 让别的脚本能读到词库

    void Awake()
    {
        if (wordsJson == null)
        {
            Debug.LogError("wordsJson not assigned.");
            Words = Array.Empty<string>();
            return;
        }

        WordList data = JsonUtility.FromJson<WordList>(wordsJson.text);
        Words = (data != null && data.words != null) ? data.words : Array.Empty<string>();

        Debug.Log("Loaded words count: " + Words.Length);
    }

    public string GetRandomWord()
    {
        if (Words.Length == 0) return "";
        return Words[UnityEngine.Random.Range(0, Words.Length)];
    }
}
