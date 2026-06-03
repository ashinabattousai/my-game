using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 游戏主控：负责把“词库/输入/UI/战斗状态”串起来。
public class GameManager : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] private WordDatabase wordDb;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backHomeButton;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private EnemyTemplate[] enemyTemplates;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text enemyHPText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private TMP_Text modeText;

    [Header("Run Config")]
    [SerializeField] private int startPlayerMaxHp = 5;
    [SerializeField] private int startEnemyMaxHp = 3;
    [SerializeField] private int baseScorePerHit = 10;
    [SerializeField] private int bossEveryKills = 5;

    private Entry currentEntry;
    private string[] currentAnswers = System.Array.Empty<string>();
    private string selectedLang = "jp";
    private int score;
    private int combo;
    private readonly EnemyState currentEnemy = new EnemyState();
    private int damageToEnemy = 1;
    private int kills;
    private int playerMaxHp = 5;
    private int playerHp = 5;
    private bool gameOver;
    private bool waitingNext;

    private void Start()
    {
        AddListener(submitButton, Submit);
        AddListener(restartButton, RestartGame);
        AddListener(backHomeButton, BackToHome);
        AddListener(nextButton, NextQuestion);
        ResetRun();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Submit();
    }

    private void Submit()
    {
        if (gameOver || waitingNext)
            return;

        bool hit = CheckHitAny(answerInput != null ? answerInput.text : string.Empty, currentAnswers);
        SetHint(hit ? "Correct! +" + (baseScorePerHit * (combo + 1)) : "Wrong!");

        if (hit)
        {
            combo++;
            score += baseScorePerHit * combo;
            currentEnemy.hp -= damageToEnemy;

            if (currentEnemy.hp <= 0)
            {
                kills++;
                SetHint("Victory! A new monster appears.");
                SpawnEnemy();
            }

            UpdateHud();
            ClearAndFocusInput();
            NextWord();
            return;
        }

        HandleWrongAnswer();
    }

    private void HandleWrongAnswer()
    {
        combo = 0;
        playerHp = Mathf.Max(0, playerHp - 1);
        SaveWrongEntry();

        if (playerHp <= 0)
        {
            SetActive(backHomeButton, true);
            gameOver = true;
            SetHint("Game Over — press Restart or return Home.");
            SetInteractable(submitButton, false);
            SetInteractable(answerInput, false);
            SetInteractable(nextButton, false);
            UpdateHud();
            return;
        }

        string answer = currentAnswers != null && currentAnswers.Length > 0
            ? string.Join(" / ", currentAnswers)
            : "(no answer)";

        SetHint("Wrong. Answer: " + answer);
        waitingNext = true;
        SetInteractable(submitButton, false);
        SetInteractable(answerInput, false);
        SetInteractable(nextButton, true);
        UpdateHud();
    }

    private void SaveWrongEntry()
    {
        if (currentEntry != null && WrongBook.Instance != null)
            WrongBook.Instance.AddOrUpdate(selectedLang, currentEntry.prompt, currentAnswers);
    }

    private void NextWord()
    {
        currentEntry = wordDb != null ? wordDb.GetRandomEntryByLang(selectedLang) : null;

        if (currentEntry == null)
        {
            currentAnswers = System.Array.Empty<string>();
            SetText(wordText, "No words for mode: " + selectedLang.ToUpperInvariant());
            SetHint("Please check words.json or choose another mode.");
            return;
        }

        SetText(wordText, currentEntry.prompt);
        currentAnswers = currentEntry.answers ?? System.Array.Empty<string>();
    }

    private void UpdateHud()
    {
        SetText(scoreText, "Score  " + score);
        SetText(comboText, "Combo  x" + combo);
        SetText(enemyHPText, $"{currentEnemy.name} HP  {currentEnemy.hp}/{currentEnemy.maxHp}");
        SetText(playerHpText, "Player HP  " + playerHp + "/" + playerMaxHp);
        SetText(killsText, "Kills  " + kills);
        UpdateModeLabel();
    }

    private void RestartGame()
    {
        ResetRun();
    }

    private void ResetRun()
    {
        selectedLang = PlayerPrefs.GetString("selected_lang", "jp");
        SetActive(backHomeButton, false);
        gameOver = false;
        waitingNext = false;
        score = 0;
        combo = 0;
        kills = 0;
        playerMaxHp = Mathf.Max(1, startPlayerMaxHp);
        playerHp = playerMaxHp;

        SpawnEnemy();
        SetInteractable(submitButton, true);
        SetInteractable(answerInput, true);
        SetInteractable(nextButton, false);
        SetHint("Type the answer and press Enter or Submit.");
        UpdateHud();
        NextWord();
        ClearAndFocusInput();
    }

    private void SpawnEnemy()
    {
        EnemyTemplate template = PickEnemyTemplate();
        if (template == null)
        {
            currentEnemy.name = "Slime";
            currentEnemy.maxHp = Mathf.Max(1, startEnemyMaxHp + kills / 2);
            currentEnemy.hp = currentEnemy.maxHp;
            return;
        }

        currentEnemy.name = string.IsNullOrWhiteSpace(template.name) ? "Monster" : template.name;
        int bonus = template.isBoss ? kills : kills / 2;
        currentEnemy.maxHp = Mathf.Max(1, template.baseMaxHp + bonus);
        currentEnemy.hp = currentEnemy.maxHp;
    }

    private EnemyTemplate PickEnemyTemplate()
    {
        if (enemyTemplates == null || enemyTemplates.Length == 0)
            return null;

        bool shouldSpawnBoss = bossEveryKills > 0 && kills > 0 && kills % bossEveryKills == 0;
        if (shouldSpawnBoss)
        {
            for (int i = 0; i < enemyTemplates.Length; i++)
                if (enemyTemplates[i] != null && enemyTemplates[i].isBoss)
                    return enemyTemplates[i];
        }

        for (int tries = 0; tries < 20; tries++)
        {
            EnemyTemplate template = enemyTemplates[Random.Range(0, enemyTemplates.Length)];
            if (template != null && !template.isBoss)
                return template;
        }

        for (int i = 0; i < enemyTemplates.Length; i++)
            if (enemyTemplates[i] != null)
                return enemyTemplates[i];

        return null;
    }

    private bool CheckHitAny(string input, string[] answers)
    {
        if (string.IsNullOrWhiteSpace(input) || answers == null || answers.Length == 0)
            return false;

        string typed = input.Trim();
        for (int i = 0; i < answers.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(answers[i]) && string.Equals(typed, answers[i].Trim(), System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void NextQuestion()
    {
        if (!waitingNext)
            return;

        waitingNext = false;
        SetInteractable(submitButton, true);
        SetInteractable(answerInput, true);
        SetInteractable(nextButton, false);
        SetHint("Type the next answer.");
        ClearAndFocusInput();
        NextWord();
        UpdateHud();
    }

    private void UpdateModeLabel()
    {
        SetText(modeText, selectedLang == "en" ? "Mode  English" : "Mode  Japanese");
    }

    private void BackToHome()
    {
        SceneManager.LoadScene("Home");
    }

    private void ClearAndFocusInput()
    {
        if (answerInput == null)
            return;

        answerInput.text = string.Empty;
        answerInput.ActivateInputField();
    }

    private void SetHint(string message)
    {
        SetText(hintText, message);
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static void SetInteractable(Selectable selectable, bool interactable)
    {
        if (selectable != null)
            selectable.interactable = interactable;
    }

    private static void SetActive(Button button, bool active)
    {
        if (button != null)
            button.gameObject.SetActive(active);
    }
}
