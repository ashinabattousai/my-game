using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        BattleLogic logic = new BattleLogic();

        bool result = logic.CheckHit("apple", "apple");
        Debug.Log("CheckHit result: " + result);
    }
}
