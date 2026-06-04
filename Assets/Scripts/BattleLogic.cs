using System;
using System.Globalization;
using System.Text;

public enum AnswerGrade
{
    Wrong,
    Near,
    Exact
}

public class AnswerResult
{
    public AnswerGrade grade;
    public string matchedAnswer;
    public string message;
}

public class BattleLogic
{
    public bool CheckHit(string input, string target)
    {
        return Judge(input, target).grade == AnswerGrade.Exact;
    }

    public AnswerResult JudgeBest(string input, string[] targets)
    {
        AnswerResult best = new AnswerResult { grade = AnswerGrade.Wrong, message = "答案不对" };
        if (targets == null || targets.Length == 0)
            return best;

        for (int i = 0; i < targets.Length; i++)
        {
            AnswerResult result = Judge(input, targets[i]);
            if (result.grade > best.grade)
                best = result;

            if (best.grade == AnswerGrade.Exact)
                return best;
        }

        return best;
    }

    public AnswerResult Judge(string input, string target)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(target))
            return new AnswerResult { grade = AnswerGrade.Wrong, matchedAnswer = target, message = "还没有输入答案" };

        string typed = NormalizeAnswer(input);
        string expected = NormalizeAnswer(target);
        if (typed == expected)
            return new AnswerResult { grade = AnswerGrade.Exact, matchedAnswer = target, message = "完全正确" };

        if (NormalizeLoose(typed) == NormalizeLoose(expected))
            return new AnswerResult { grade = AnswerGrade.Near, matchedAnswer = target, message = "很接近，注意长音或重复元音" };

        int distance = EditDistance(typed, expected);
        if (distance == 1)
            return new AnswerResult { grade = AnswerGrade.Near, matchedAnswer = target, message = "很接近，可能少了或多了一个字符" };

        if (distance == 2 && expected.Length >= 6)
            return new AnswerResult { grade = AnswerGrade.Near, matchedAnswer = target, message = "接近正确，再检查中间拼写" };

        return new AnswerResult { grade = AnswerGrade.Wrong, matchedAnswer = target, message = "答案不对" };
    }

    private string NormalizeAnswer(string value)
    {
        string normalized = value.Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        StringBuilder builder = new StringBuilder(normalized.Length);
        for (int i = 0; i < normalized.Length; i++)
        {
            char c = NormalizeRomanVowel(normalized[i]);
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (char.IsWhiteSpace(c) || category == UnicodeCategory.DashPunctuation ||
                category == UnicodeCategory.ConnectorPunctuation || category == UnicodeCategory.OtherPunctuation)
                continue;

            builder.Append(c);
        }
        return builder.ToString();
    }

    private string NormalizeLoose(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string compact = value.Replace("ー", "").Replace("－", "").Replace("-", "");
        compact = compact.Replace("ou", "o").Replace("oo", "o").Replace("uu", "u");

        StringBuilder builder = new StringBuilder(compact.Length);
        char previous = '\0';
        for (int i = 0; i < compact.Length; i++)
        {
            char c = compact[i];
            bool repeatedVowel = c == previous && IsVowel(c);
            if (!repeatedVowel)
                builder.Append(c);
            previous = c;
        }

        return builder.ToString();
    }

    private char NormalizeRomanVowel(char c)
    {
        switch (c)
        {
            case 'ā':
            case 'á':
            case 'à':
            case 'â':
            case 'ä':
                return 'a';
            case 'ī':
            case 'í':
            case 'ì':
            case 'î':
            case 'ï':
                return 'i';
            case 'ū':
            case 'ú':
            case 'ù':
            case 'û':
            case 'ü':
                return 'u';
            case 'ē':
            case 'é':
            case 'è':
            case 'ê':
            case 'ë':
                return 'e';
            case 'ō':
            case 'ó':
            case 'ò':
            case 'ô':
            case 'ö':
                return 'o';
            default:
                return c;
        }
    }

    private bool IsVowel(char c)
    {
        return c == 'a' || c == 'i' || c == 'u' || c == 'e' || c == 'o';
    }

    private int EditDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++)
            dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++)
            dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
            }
        }
        return dp[a.Length, b.Length];
    }
}
