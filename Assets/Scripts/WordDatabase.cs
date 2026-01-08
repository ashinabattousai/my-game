using System;
using UnityEngine;

[Serializable]
public class WordList
{
    public string[] words;
}

public class WordDatabase : MonoBehaviour
{
    [SerializeField] private TextAsset wordsJson; // °Ñ words.json ÍÏ½øÀ´

    void Start()
    {
        if (wordsJson == null)
        {
            Debug.LogError("wordsJson not assigned. Drag words.json into the slot.");
            return;
        }

        WordList data = JsonUtility.FromJson<WordList>(wordsJson.text);

        int count = (data != null && data.words != null) ? data.words.Length : 0;
        Debug.Log("Loaded words count: " + count);

        if (count > 0)
            Debug.Log("First word: " + data.words[0]);
    }
}
