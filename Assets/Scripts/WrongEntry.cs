using System;

[Serializable]
public class WrongEntry
{
    public string id;
    public string lang;
    public string prompt;
    public string[] answers;
    public string lastWrongAnswer;
    public long createdAtUnix;
    public long lastMissedUnix;
    public int timesWrong;
    public int reviewCorrectStreak;
}
