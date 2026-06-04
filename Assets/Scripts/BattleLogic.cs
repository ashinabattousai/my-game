public class BattleLogic
{
    public bool CheckHit(string input, string target)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(target))
            return false;

        return string.Equals(input.Trim(), target.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
