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

    public Entry[] Entries { get; private set; }  // 让别的脚本能读到词库

    void Awake()
    {
        if (wordsJson == null)
        {
            Debug.LogError("wordsJson not assigned.");
            Entries = Array.Empty<Entry>();
            return;
        }

        WordList data = JsonUtility.FromJson<WordList>(wordsJson.text);
        Entries = (data != null && data.entries != null) ? data.entries : Array.Empty<Entry>();

        Debug.Log("Loaded entries count: " + Entries.Length);
    }
   
/*    public Entry GetRandomEntey()
    {
        if (Entries.Length == 0 || Entries == null) return null;
        return Entries[UnityEngine.Random.Range(0, Entries.Length)];
    }
*/
    public Entry GetRandomEntryByLang(string lang)
    {
        if (Entries == null || Entries.Length == 0)
            return null;

        int count = 0;
        for(int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i] != null && Entries[i].lang == lang)
                count++;
        }

        if(count == 0)
        return null;

        int k = UnityEngine.Random.Range(0, count);
        for(int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i] != null && Entries[i].lang == lang)
            {
                if (k == 0)
                    return Entries[i];
                k--;
            }
        }
        return null;
    }

}
