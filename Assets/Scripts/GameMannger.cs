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
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text slimeHPText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text playerHpText;


    private BattleLogic logic = new BattleLogic();
    private string currentWord = "";
    private int score = 0;
    private int combo = 0;
    private int SlimeMaxHP = 3;
    private int SlimeHP = 3;
    private int damageToEnemy = 1;
    private int kills = 0;
    private int playerMaxHp = 5;
    private int playerHp = 5;
    private bool gameOver = false;


    void Start()
    {
        submitButton.onClick.AddListener(Submit);
        NextWord();
        hintText.text = "Enter Or Submit";
        answerInput.ActivateInputField();
        SlimeHP = SlimeMaxHP;
        playerHp = playerMaxHp;
        UpdateHud();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Submit();
    }

    private void Submit()
    {
        if (gameOver) return;
        string typed = answerInput.text;
        bool hit = logic.CheckHit(typed, currentWord);
        hintText.text = hit ? "Correct" : "Wrong";
        if (hit == true)
        {
            combo++;
            score += 10 * combo;
            SlimeHP -= damageToEnemy;

            if (SlimeHP <= 0)
            {
                kills += 1;
                SlimeMaxHP++;
                hintText.text = "Victory";
                SlimeHP = SlimeMaxHP;
            }
        }
        else
        {
            combo = 0;

            playerHp -= 1;
            if(playerHp <= 0)
            {
                playerHp = 0;
                gameOver = true;
                hintText.text = "Game Over";
                UpdateHud();
                return;
            }
        }
        UpdateHud();

        answerInput.text = "";
        answerInput.ActivateInputField();
        NextWord();
    }

    private void NextWord()
    {
        currentWord = wordDb.GetRandomWord();
        wordText.text = currentWord;
    }

    private void UpdateHud()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (comboText != null) comboText.text = "Combo: " + combo;
        if (slimeHPText != null)
            slimeHPText.text = "SlimeHP : " + SlimeHP + "/" + SlimeMaxHP;
        if (playerHpText != null) 
            playerHpText.text = "Player HP: " + playerHp + "/" + playerMaxHp;
        if (killsText != null)
            killsText.text = "kills : " + kills;
    }
}
