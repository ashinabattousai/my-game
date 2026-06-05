using System;

[Serializable]
public class Entry
{
    public string id;
    public string lang;
    public string questionType;
    public string prompt;

    public string answerMode;
    public string primaryAnswer;
    public string[] answers;
    public string displayAnswer;

    public string kana;
    public string kanji;
    public string[] romaji;
    public string[] meaning;

    public string pos;
    public int difficulty;
    public string level;
    public string chapter;
    public string[] tags;

    public string hint;
    public string example;
    public string exampleMeaning;
}
