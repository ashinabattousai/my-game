using System;

[Serializable]
public class Entry
{
    public string prompt;     // 屏幕上显示的中文释义，比如 “樱花”“苹果”
    public string[] answers;  // 可接受答案列表，比如 ["さくら","桜"] 或 ["apple"]
    public string lang;       // 可选：语言标签 "jp"/"en"（先留着，后面可用）
}
