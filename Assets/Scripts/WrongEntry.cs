using System;

[Serializable]
public class WrongEntry
{
    public string id;
    public string lang;
    public string prompt;
    public string[] answers;
    public long createdAtUnix;
}
