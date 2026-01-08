public class BattleLogic
{
    public bool CheckHit(string input, string target)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(target))
            return false;

        return input.Trim() == target.Trim();
    }
}
