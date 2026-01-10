using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] private WordDatabase wordDb;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Button submitButton;

    private BattleLogic logic = new BattleLogic();
    private string currentWord = "";

    void Start()
    {
        submitButton.onClick.AddListener(Submit);
        NextWord();
        hintText.text = "输入后按回车或点 Submit";
        answerInput.ActivateInputField();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Submit();
    }

    private void Submit()
    {
        string typed = answerInput.text;

        bool hit = logic.CheckHit(typed, currentWord);
        hintText.text = hit ? "✅ Correct" : "❌ Wrong";

        answerInput.text = "";
        answerInput.ActivateInputField();
        NextWord();
    }

    private void NextWord()
    {
        currentWord = wordDb.GetRandomWord();
        wordText.text = currentWord;
    }
}
