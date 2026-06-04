using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private const string ProgressKey = "word_quest_run_progress_v1";
    private const string StartNewRunKey = "word_quest_start_new_run";

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
    [SerializeField] private TMP_Text enemyMetaText;
    [SerializeField] private TMP_Text difficultyText;

    [Header("Config")]
    [SerializeField] private int startPlayerMaxHp = 5;
    [SerializeField] private int baseScorePerHit = 10;
    [SerializeField] private float questionTimeLimit = 18f;
    [SerializeField] private bool useRuntimeUi = true;
    [SerializeField] private int mapDepthCount = 8;
    [SerializeField] private int mapRows = 3;

    private readonly BattleLogic logic = new BattleLogic();
    private Entry currentEntry;
    private string[] currentAnswers = System.Array.Empty<string>();
    private string selectedLang = "jp";
    private int score;
    private int combo;
    private int kills;
    private int playerMaxHp;
    private int playerHp;
    private int damageToEnemy = 1;
    private int currentSeed;
    private bool gameOver;
    private bool waitingNext;
    private bool routeComplete;
    private bool answerRevealed;
    private float timeLeft;

    private EnemyState currentEnemy = new EnemyState();
    private EnemyTemplate currentEnemyTemplate;
    private RawImage enemyImage;
    private Image enemyHpFill;
    private Image playerHpFill;
    private Image timerFill;
    private TMP_Text answerPreviewText;
    private TMP_Text masteryText;
    private TMP_Text studyPromptText;
    private Button hintButton;
    private Button revealButton;
    private RectTransform enemyPanelRect;
    private RectTransform questionPanelRect;
    private float enemyHpTarget = 1f;
    private float playerHpTarget = 1f;
    private float timerTarget = 1f;

    private GameObject mapRoot;
    private Transform mapContent;
    private TMP_Text mapTitleText;
    private TMP_Text mapStatusText;
    private MapNode[] runMap = System.Array.Empty<MapNode>();
    private HashSet<string> completedNodes = new HashSet<string>();
    private MapNode activeNode;

    private void Start()
    {
        EnsureSystems();
        EnsureEnemyTemplates();

        if (useRuntimeUi)
            BuildRuntimeUi();

        if (submitButton) submitButton.onClick.AddListener(Submit);
        if (restartButton) restartButton.onClick.AddListener(RestartGame);
        if (backHomeButton) backHomeButton.onClick.AddListener(BackToHome);
        if (nextButton) nextButton.onClick.AddListener(NextQuestion);
        if (hintButton) hintButton.onClick.AddListener(ShowHint);
        if (revealButton) revealButton.onClick.AddListener(RevealAnswer);

        InitializeRun();
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !waitingNext && mapRoot != null && !mapRoot.activeSelf)
            Submit();

        if (Input.GetKeyDown(KeyCode.Escape))
            BackToHome();

        SmoothBars();

        if (gameOver || waitingNext || currentEntry == null || mapRoot == null || mapRoot.activeSelf)
            return;

        timeLeft -= Time.deltaTime;
        UpdateTimerBar();
        if (timeLeft <= 0f)
            ResolveMiss("超时");
    }

    private void BuildRuntimeUi()
    {
        Canvas canvas = RuntimeUI.CreateCanvas("Runtime Battle Canvas");
        RuntimeUI.Background(canvas.transform, RuntimeUI.LoadTexture("Art/library_battle_bg"));
        RuntimeUI.Panel(canvas.transform, "Dark Wash", new Color(0.01f, 0.02f, 0.04f, 0.62f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).raycastTarget = false;

        Image top = RuntimeUI.Panel(canvas.transform, "Top Dock", RuntimeUI.Hex("111722"), new Vector2(0.035f, 0.895f), new Vector2(0.965f, 0.972f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddHorizontalLayout(top.gameObject, 18, 16);
        modeText = AddStat(top.transform, "模式 JP");
        scoreText = AddStat(top.transform, "分数 0");
        comboText = AddStat(top.transform, "连击 0");
        killsText = AddStat(top.transform, "击败 0");
        playerHpText = AddStat(top.transform, "生命 5/5");

        Image stage = RuntimeUI.Panel(canvas.transform, "Enemy Stage", new Color(0.02f, 0.025f, 0.035f, 0.82f), new Vector2(0.055f, 0.12f), new Vector2(0.39f, 0.84f), Vector2.zero, Vector2.zero);
        enemyPanelRect = stage.GetComponent<RectTransform>();
        RuntimeUI.AddVerticalLayout(stage.gameObject, 26, 12);
        TMP_Text stageTitle = RuntimeUI.Text(stage.transform, "Stage Title", "敌影", 28, RuntimeUI.Hex("D8C08A"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(stageTitle.gameObject, 34, 42);
        enemyImage = RuntimeUI.RawImage(stage.transform, "Enemy Art", RuntimeUI.LoadTexture("Art/Enemies/enemy_goblin"));
        RuntimeUI.Layout(enemyImage.gameObject, 380, 430);
        enemyHPText = RuntimeUI.Text(stage.transform, "Enemy HP", "敌人 0/0", 30, RuntimeUI.Paper, TextAlignmentOptions.Center);
        RuntimeUI.Layout(enemyHPText.gameObject, 40, 52);
        enemyMetaText = RuntimeUI.Text(stage.transform, "Enemy Meta", "", 22, RuntimeUI.Hex("99A8B8"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(enemyMetaText.gameObject, 34, 44);
        enemyHpFill = CreateBar(stage.transform, RuntimeUI.Coral, RuntimeUI.Hex("351B24"));
        RuntimeUI.Layout(enemyHpFill.transform.parent.gameObject, 22, 30);

        Image card = RuntimeUI.Panel(canvas.transform, "Study Card", new Color(0.965f, 0.91f, 0.76f, 0.94f), new Vector2(0.425f, 0.12f), new Vector2(0.945f, 0.84f), Vector2.zero, Vector2.zero);
        questionPanelRect = card.GetComponent<RectTransform>();
        RuntimeUI.AddVerticalLayout(card.gameObject, 34, 14);

        studyPromptText = RuntimeUI.Text(card.transform, "Study Prompt", "看提示，先回忆，再输入。", 26, RuntimeUI.Hex("6C5632"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(studyPromptText.gameObject, 36, 46);
        wordText = RuntimeUI.Text(card.transform, "Question", "准备中", 76, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(wordText.gameObject, 112, 132);
        masteryText = RuntimeUI.Text(card.transform, "Mastery", "新词", 24, RuntimeUI.Hex("7B6542"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(masteryText.gameObject, 30, 38);
        difficultyText = RuntimeUI.Text(card.transform, "Difficulty", "", 22, RuntimeUI.Hex("7B6542"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(difficultyText.gameObject, 28, 36);
        hintText = RuntimeUI.Text(card.transform, "Hint", "选择地图节点开始", 30, RuntimeUI.Hex("9C6A2B"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(hintText.gameObject, 48, 62);
        answerPreviewText = RuntimeUI.Text(card.transform, "Answer Preview", "", 26, RuntimeUI.Hex("394B5C"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(answerPreviewText.gameObject, 48, 62);

        timerFill = CreateBar(card.transform, RuntimeUI.Teal, RuntimeUI.Hex("CDBB8D"));
        RuntimeUI.Layout(timerFill.transform.parent.gameObject, 18, 26);
        answerInput = RuntimeUI.InputField(card.transform, "输入答案后按 Enter");
        RuntimeUI.Layout(answerInput.gameObject, 68, 82);

        GameObject studyButtons = new GameObject("Study Buttons", typeof(RectTransform));
        studyButtons.transform.SetParent(card.transform, false);
        RuntimeUI.AddHorizontalLayout(studyButtons, 0, 12);
        RuntimeUI.Layout(studyButtons, 58, 72);
        hintButton = RuntimeUI.Button(studyButtons.transform, "Hint", "提示", RuntimeUI.Hex("65748A"), Color.white);
        revealButton = RuntimeUI.Button(studyButtons.transform, "Reveal", "看答案", RuntimeUI.Hex("8A6A3E"), Color.white);
        submitButton = RuntimeUI.Button(studyButtons.transform, "Submit", "提交", RuntimeUI.Teal, Color.white);

        GameObject runButtons = new GameObject("Run Buttons", typeof(RectTransform));
        runButtons.transform.SetParent(card.transform, false);
        RuntimeUI.AddHorizontalLayout(runButtons, 0, 12);
        RuntimeUI.Layout(runButtons, 58, 72);
        nextButton = RuntimeUI.Button(runButtons.transform, "Next", "下一题", RuntimeUI.Gold, RuntimeUI.Ink);
        restartButton = RuntimeUI.Button(runButtons.transform, "Restart", "新路线", RuntimeUI.Coral, Color.white);
        backHomeButton = RuntimeUI.Button(runButtons.transform, "Home", "退出", RuntimeUI.Hex("2E394B"), Color.white);

        Image bottom = RuntimeUI.Panel(canvas.transform, "Player HP Bar", new Color(0f, 0f, 0f, 0f), new Vector2(0.425f, 0.07f), new Vector2(0.945f, 0.095f), Vector2.zero, Vector2.zero);
        playerHpFill = CreateBar(bottom.transform, RuntimeUI.Gold, RuntimeUI.Hex("3B3020"));
        RuntimeUI.SetRect(playerHpFill.transform.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        BuildMapOverlay(canvas.transform);
    }

    private void BuildMapOverlay(Transform parent)
    {
        Image root = RuntimeUI.Panel(parent, "Route Map", new Color(0.02f, 0.025f, 0.035f, 0.94f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        mapRoot = root.gameObject;

        Image header = RuntimeUI.Panel(root.transform, "Map Header", new Color(0f, 0f, 0f, 0f), new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(header.gameObject, 0, 6);
        mapTitleText = RuntimeUI.Text(header.transform, "Map Title", "选择路线", 62, RuntimeUI.Paper, TextAlignmentOptions.Center);
        RuntimeUI.Layout(mapTitleText.gameObject, 74, 86);
        mapStatusText = RuntimeUI.Text(header.transform, "Map Status", "选择一个可到达的关卡。", 28, RuntimeUI.Hex("B8C2D0"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(mapStatusText.gameObject, 42, 54);

        Image content = RuntimeUI.Panel(root.transform, "Map Content", new Color(0.82f, 0.72f, 0.50f, 0.10f), new Vector2(0.07f, 0.18f), new Vector2(0.93f, 0.78f), Vector2.zero, Vector2.zero);
        mapContent = content.transform;

        GameObject bottomButtons = new GameObject("Map Buttons", typeof(RectTransform));
        bottomButtons.transform.SetParent(root.transform, false);
        RuntimeUI.SetRect(bottomButtons.transform, new Vector2(0.30f, 0.065f), new Vector2(0.70f, 0.145f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddHorizontalLayout(bottomButtons, 0, 18);
        Button restartMap = RuntimeUI.Button(bottomButtons.transform, "Restart Route", "生成新路线", RuntimeUI.Coral, Color.white);
        Button back = RuntimeUI.Button(bottomButtons.transform, "Back Home", "返回首页", RuntimeUI.Hex("2E394B"), Color.white);
        restartMap.onClick.AddListener(RestartGame);
        back.onClick.AddListener(BackToHome);
    }

    private TMP_Text AddStat(Transform parent, string value)
    {
        TMP_Text text = RuntimeUI.Text(parent, "Stat", value, 26, RuntimeUI.Paper, TextAlignmentOptions.Center);
        RuntimeUI.Layout(text.gameObject, 46, 58, 1);
        return text;
    }

    private Image CreateBar(Transform parent, Color fillColor, Color backColor)
    {
        Image back = RuntimeUI.Panel(parent, "Bar", backColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(back.transform, false);
        Image fill = fillGo.GetComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        RuntimeUI.SetRect(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return fill;
    }

    private void InitializeRun()
    {
        bool startNew = PlayerPrefs.GetInt(StartNewRunKey, 0) == 1;
        PlayerPrefs.SetInt(StartNewRunKey, 0);
        PlayerPrefs.Save();

        if (!startNew && TryLoadProgress())
        {
            ShowMap("已读取进度，选择下一关继续。");
            return;
        }

        NewRun(false);
    }

    private void NewRun(bool showMessage)
    {
        selectedLang = PlayerPrefs.GetString("selected_lang", "jp");
        score = 0;
        combo = 0;
        kills = 0;
        playerMaxHp = startPlayerMaxHp;
        playerHp = playerMaxHp;
        activeNode = null;
        routeComplete = false;
        waitingNext = false;
        gameOver = false;
        completedNodes.Clear();
        currentSeed = UnityEngine.Random.Range(10000, 999999);
        runMap = GenerateMap(currentSeed);
        currentEnemy = new EnemyState();
        currentEntry = null;
        currentAnswers = System.Array.Empty<string>();
        SetPlayInteractable(false);
        UpdateHud();
        SaveProgress();
        ShowMap(showMessage ? "新路线已生成。" : "选择第一个关卡开始冒险。");
    }

    private void SelectMapNode(MapNode node)
    {
        if (node == null || !IsReachable(node) || completedNodes.Contains(node.id) || routeComplete)
            return;

        activeNode = node;
        waitingNext = false;
        gameOver = false;
        if (answerInput) answerInput.text = "";
        if (nextButton) nextButton.interactable = false;

        if (node.type == MapNodeType.Rest)
        {
            int before = playerHp;
            playerHp = Mathf.Min(playerMaxHp, playerHp + 2);
            hintText.text = "休息恢复 " + (playerHp - before) + " 点生命";
            answerPreviewText.text = "";
            CompleteActiveNode();
            UpdateHud();
            SaveProgress();
            ShowMap("休息完成，生命已恢复。");
            return;
        }

        if (mapRoot) mapRoot.SetActive(false);
        SetPlayInteractable(true);
        SpawnEnemy();
        NextWord();
        UpdateHud();
        SaveProgress();
        if (answerInput) answerInput.ActivateInputField();
    }

    private void Submit()
    {
        if (gameOver || waitingNext || activeNode == null || currentEntry == null)
            return;

        bool hit = CheckHitAny(answerInput != null ? answerInput.text : "", currentAnswers);
        if (hit && !answerRevealed)
            ResolveHit();
        else if (hit)
            ResolveAssistedHit();
        else
            ResolveMiss("错误");
    }

    private void ResolveHit()
    {
        WordProgressStore.Record(selectedLang, currentEntry.prompt, true);
        string answer = FormatCurrentAnswers();
        combo++;
        score += baseScorePerHit * Mathf.Max(1, combo);
        currentEnemy.hp -= damageToEnemy;
        hintText.text = combo >= 3 ? "记住了，连击继续" : "答对";
        answerPreviewText.text = "正确写法: " + answer;
        StartCoroutine(PulseEnemy(new Color(1f, 0.78f, 0.70f, 1f)));
        StartCoroutine(Shake(enemyPanelRect, 14f, 0.15f));
        FinishHitIfEnemyDefeated();
    }

    private void ResolveAssistedHit()
    {
        WordProgressStore.Record(selectedLang, currentEntry.prompt, false);
        combo = 0;
        score += Mathf.Max(2, baseScorePerHit / 3);
        currentEnemy.hp -= damageToEnemy;
        hintText.text = "看过答案后命中，已标为需复习";
        answerPreviewText.text = "正确写法: " + FormatCurrentAnswers();
        StartCoroutine(PulseEnemy(new Color(1f, 0.85f, 0.58f, 1f)));
        FinishHitIfEnemyDefeated();
    }

    private void FinishHitIfEnemyDefeated()
    {
        if (currentEnemy.hp <= 0)
        {
            kills++;
            score += currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? 120 : (activeNode != null && activeNode.type == MapNodeType.Elite ? 70 : 35);
            hintText.text = currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? "首领击败" : "战斗胜利";
            CompleteActiveNode();
            UpdateHud();
            SaveProgress();
            ShowMap(routeComplete ? "本轮通关！可以生成新路线继续挑战。" : "胜利！选择下一关继续前进。");
            return;
        }

        if (answerInput) answerInput.text = "";
        NextWord();
        UpdateHud();
        SaveProgress();
        if (answerInput) answerInput.ActivateInputField();
    }

    private void ResolveMiss(string reason)
    {
        if (gameOver || waitingNext)
            return;

        WordProgressStore.Record(selectedLang, currentEntry.prompt, false);
        combo = 0;
        playerHp = Mathf.Max(0, playerHp - 1);

        if (WrongBook.Instance != null)
            WrongBook.Instance.AddOrUpdate(selectedLang, currentEntry.prompt, currentAnswers);

        if (playerHp <= 0)
        {
            gameOver = true;
            routeComplete = true;
            hintText.text = "挑战失败";
            answerPreviewText.text = "答案: " + FormatCurrentAnswers();
            SetPlayInteractable(false);
            ClearProgress();
            UpdateHud();
            return;
        }

        waitingNext = true;
        hintText.text = reason + " - 生命 -1";
        answerPreviewText.text = "答案: " + FormatCurrentAnswers();
        StartCoroutine(Shake(questionPanelRect, 18f, 0.20f));
        SetPlayInteractable(false);
        if (nextButton) nextButton.interactable = true;
        UpdateHud();
        SaveProgress();
    }

    private void NextWord()
    {
        int minDifficulty;
        int maxDifficulty;
        GetDifficultyWindow(out minDifficulty, out maxDifficulty);
        currentEntry = wordDb != null ? wordDb.GetLearningEntryByLangAndDifficulty(selectedLang, minDifficulty, maxDifficulty) : null;
        timeLeft = Mathf.Max(7f, questionTimeLimit - Mathf.Min(5f, kills * 0.3f));
        answerRevealed = false;
        answerPreviewText.text = "";

        if (currentEntry == null)
        {
            currentAnswers = System.Array.Empty<string>();
            wordText.text = "没有可用词条";
            return;
        }

        wordText.text = currentEntry.prompt;
        currentAnswers = currentEntry.answers ?? System.Array.Empty<string>();
        studyPromptText.text = selectedLang == "en" ? "根据中文回忆英文拼写" : "根据中文回忆日语读法";
        masteryText.text = WordProgressStore.Label(selectedLang, currentEntry.prompt);
        difficultyText.text = "词汇难度 " + minDifficulty + "-" + maxDifficulty + " / " + MapNodeTypeLabel(activeNode != null ? activeNode.type : MapNodeType.Monster);
        hintText.text = "先在脑中回忆，再输入答案。";
        UpdateTimerBar();
    }

    private void ShowHint()
    {
        if (currentAnswers == null || currentAnswers.Length == 0)
            return;

        string answer = currentAnswers[0];
        string first = answer.Length > 0 ? answer.Substring(0, 1) : "?";
        hintText.text = "提示: 首字母 " + first + " / 长度 " + answer.Length;
        timeLeft = Mathf.Max(3f, timeLeft - 2f);
        UpdateTimerBar();
    }

    private void RevealAnswer()
    {
        if (currentEntry == null)
            return;

        answerRevealed = true;
        answerPreviewText.text = "正确写法: " + FormatCurrentAnswers();
        hintText.text = "看过答案后再提交，会加入复习权重。";
        if (answerInput) answerInput.ActivateInputField();
    }

    private void NextQuestion()
    {
        if (!waitingNext)
            return;

        waitingNext = false;
        if (answerInput) answerInput.text = "";
        SetPlayInteractable(true);
        if (nextButton) nextButton.interactable = false;
        NextWord();
        UpdateHud();
        SaveProgress();
        if (answerInput) answerInput.ActivateInputField();
    }

    private void GetDifficultyWindow(out int minDifficulty, out int maxDifficulty)
    {
        int depth = activeNode != null ? activeNode.depth : kills;
        minDifficulty = depth >= 5 ? 2 : 1;
        maxDifficulty = depth >= 5 ? 3 : (depth >= 2 ? 2 : 1);
        if (activeNode != null && activeNode.type == MapNodeType.Elite)
            minDifficulty = Mathf.Max(minDifficulty, 2);
        if (activeNode != null && activeNode.type == MapNodeType.Boss)
        {
            minDifficulty = 2;
            maxDifficulty = 3;
        }
    }

    private void SpawnEnemy()
    {
        EnemyTemplate template = ChooseEnemyTemplate();
        currentEnemyTemplate = template;
        currentEnemy.name = template.name;
        int depthBonus = activeNode != null ? activeNode.depth : kills / 2;
        float nodeMultiplier = activeNode != null && activeNode.type == MapNodeType.Elite ? 1.35f : 1f;
        if (activeNode != null && activeNode.type == MapNodeType.Boss)
            nodeMultiplier = 1.7f;
        int bonus = template.isBoss ? kills + depthBonus : depthBonus + kills / 2;
        currentEnemy.maxHp = Mathf.Max(1, Mathf.RoundToInt((template.baseMaxHp + bonus) * nodeMultiplier));
        currentEnemy.hp = currentEnemy.maxHp;
        damageToEnemy = 1;
        UpdateEnemyArt(template);
    }

    private EnemyTemplate ChooseEnemyTemplate()
    {
        int unlock = kills + (activeNode != null ? activeNode.depth : 0);
        bool wantsBoss = activeNode != null && activeNode.type == MapNodeType.Boss;
        bool wantsElite = activeNode != null && activeNode.type == MapNodeType.Elite;
        List<EnemyTemplate> pool = new List<EnemyTemplate>();

        for (int i = 0; i < enemyTemplates.Length; i++)
        {
            EnemyTemplate template = enemyTemplates[i];
            if (template == null || template.minKills > unlock)
                continue;

            if (wantsBoss && template.isBoss)
                pool.Add(template);
            else if (!wantsBoss && !template.isBoss && (!wantsElite || template.tier >= 2))
                pool.Add(template);
        }

        if (pool.Count == 0)
            pool.Add(enemyTemplates[0]);

        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    private void UpdateEnemyArt(EnemyTemplate template)
    {
        if (enemyImage == null)
            return;

        string path = template == null || string.IsNullOrWhiteSpace(template.artResource) ? "Art/Enemies/enemy_goblin" : template.artResource;
        Texture2D texture = RuntimeUI.LoadTexture(path);
        if (texture != null)
            enemyImage.texture = texture;
    }

    private void UpdateHud()
    {
        if (scoreText) scoreText.text = "分数 " + score;
        if (comboText) comboText.text = "连击 " + combo;
        if (killsText) killsText.text = "击败 " + kills;
        if (modeText) modeText.text = selectedLang == "en" ? "模式 EN" : "模式 JP";
        if (playerHpText) playerHpText.text = "生命 " + playerHp + "/" + playerMaxHp;
        if (enemyHPText) enemyHPText.text = currentEnemy.name + "  " + currentEnemy.hp + "/" + currentEnemy.maxHp;
        if (enemyMetaText && currentEnemyTemplate != null)
            enemyMetaText.text = currentEnemyTemplate.faction + "  Tier " + currentEnemyTemplate.tier;

        enemyHpTarget = currentEnemy.maxHp > 0 ? Mathf.Clamp01((float)currentEnemy.hp / currentEnemy.maxHp) : 0f;
        playerHpTarget = playerMaxHp > 0 ? Mathf.Clamp01((float)playerHp / playerMaxHp) : 0f;
        UpdateTimerBar();
    }

    private void UpdateTimerBar()
    {
        if (timerFill == null)
            return;

        float limit = Mathf.Max(1f, questionTimeLimit - Mathf.Min(5f, kills * 0.3f));
        timerTarget = Mathf.Clamp01(timeLeft / limit);
        timerFill.color = timerTarget < 0.25f ? RuntimeUI.Coral : RuntimeUI.Teal;
    }

    private void SmoothBars()
    {
        float speed = Time.deltaTime * 6f;
        if (enemyHpFill)
            enemyHpFill.fillAmount = Mathf.MoveTowards(enemyHpFill.fillAmount, enemyHpTarget, speed);
        if (playerHpFill)
            playerHpFill.fillAmount = Mathf.MoveTowards(playerHpFill.fillAmount, playerHpTarget, speed);
        if (timerFill)
            timerFill.fillAmount = Mathf.MoveTowards(timerFill.fillAmount, timerTarget, Time.deltaTime * 10f);
    }

    private void SetPlayInteractable(bool value)
    {
        if (submitButton) submitButton.interactable = value;
        if (answerInput) answerInput.interactable = value;
        if (hintButton) hintButton.interactable = value;
        if (revealButton) revealButton.interactable = value;
    }

    private bool CheckHitAny(string input, string[] answers)
    {
        if (string.IsNullOrWhiteSpace(input) || answers == null || answers.Length == 0)
            return false;

        string typed = input.Trim();
        for (int i = 0; i < answers.Length; i++)
        {
            if (logic.CheckHit(typed, answers[i]))
                return true;
        }
        return false;
    }

    private IEnumerator PulseEnemy(Color color)
    {
        if (enemyImage == null)
            yield break;

        Color original = enemyImage.color;
        Vector3 originalScale = enemyImage.transform.localScale;
        enemyImage.color = color;
        enemyImage.transform.localScale = originalScale * 1.06f;
        yield return new WaitForSeconds(0.09f);
        enemyImage.color = original;
        enemyImage.transform.localScale = originalScale;
    }

    private IEnumerator Shake(RectTransform target, float strength, float duration)
    {
        if (target == null)
            yield break;

        Vector2 original = target.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 1f - elapsed / duration;
            target.anchoredPosition = original + UnityEngine.Random.insideUnitCircle * strength * t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.anchoredPosition = original;
    }

    private MapNode[] GenerateMap(int seed)
    {
        int depthCount = Mathf.Max(4, mapDepthCount);
        int rows = Mathf.Max(2, mapRows);
        int bossRow = rows / 2;
        System.Random rng = new System.Random(seed);
        List<MapNode> nodes = new List<MapNode>();

        for (int depth = 0; depth < depthCount; depth++)
        {
            int rowCount = depth == depthCount - 1 ? 1 : rows;
            for (int i = 0; i < rowCount; i++)
            {
                int row = depth == depthCount - 1 ? bossRow : i;
                nodes.Add(new MapNode { id = NodeId(depth, row), depth = depth, row = row, type = PickNodeType(depth, depthCount, rng) });
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            MapNode node = nodes[i];
            if (node.depth >= depthCount - 1)
                continue;

            int nextDepth = node.depth + 1;
            bool nextIsBoss = nextDepth == depthCount - 1;
            List<string> nextIds = new List<string>();
            int primaryRow = nextIsBoss ? bossRow : Mathf.Clamp(node.row + rng.Next(-1, 2), 0, rows - 1);
            nextIds.Add(NodeId(nextDepth, primaryRow));
            if (!nextIsBoss && rng.NextDouble() < 0.42)
            {
                int extraRow = Mathf.Clamp(primaryRow + (rng.NextDouble() < 0.5 ? -1 : 1), 0, rows - 1);
                string extraId = NodeId(nextDepth, extraRow);
                if (!nextIds.Contains(extraId))
                    nextIds.Add(extraId);
            }
            node.nextCsv = string.Join(",", nextIds.ToArray());
        }

        return nodes.ToArray();
    }

    private MapNodeType PickNodeType(int depth, int depthCount, System.Random rng)
    {
        if (depth == depthCount - 1)
            return MapNodeType.Boss;
        if (depth == 0)
            return MapNodeType.Monster;
        double roll = rng.NextDouble();
        if (depth > 1 && roll < 0.18)
            return MapNodeType.Rest;
        if (roll < 0.42)
            return MapNodeType.Elite;
        return MapNodeType.Monster;
    }

    private void ShowMap(string status)
    {
        if (mapRoot == null)
            return;

        mapRoot.SetActive(true);
        SetPlayInteractable(false);
        if (nextButton) nextButton.interactable = false;
        if (mapTitleText) mapTitleText.text = routeComplete ? "路线完成" : "选择路线";
        if (mapStatusText) mapStatusText.text = status;
        RebuildMapNodes();
    }

    private void RebuildMapNodes()
    {
        if (mapContent == null)
            return;

        for (int i = mapContent.childCount - 1; i >= 0; i--)
            Destroy(mapContent.GetChild(i).gameObject);

        for (int i = 0; i < runMap.Length; i++)
        {
            if (string.IsNullOrEmpty(runMap[i].nextCsv))
                continue;

            string[] nextIds = runMap[i].nextCsv.Split(',');
            for (int j = 0; j < nextIds.Length; j++)
            {
                MapNode next = FindMapNode(nextIds[j]);
                if (next != null)
                    CreateMapLine(runMap[i], next, completedNodes.Contains(runMap[i].id));
            }
        }

        int rows = Mathf.Max(2, mapRows);
        int depthCount = Mathf.Max(4, mapDepthCount);
        for (int i = 0; i < runMap.Length; i++)
        {
            MapNode node = runMap[i];
            bool completed = completedNodes.Contains(node.id);
            bool reachable = IsReachable(node);
            Color color = completed ? RuntimeUI.Hex("4B6B57") : (reachable ? NodeColor(node.type) : RuntimeUI.Hex("283142"));
            Button button = RuntimeUI.Button(mapContent, "Node " + node.id, completed ? "已完成" : MapNodeTypeLabel(node.type), color, Color.white);
            RectTransform rt = button.GetComponent<RectTransform>();
            float x = depthCount <= 1 ? 0.5f : Mathf.Lerp(0.07f, 0.93f, (float)node.depth / (depthCount - 1));
            float y = rows <= 1 ? 0.5f : Mathf.Lerp(0.20f, 0.80f, (float)node.row / (rows - 1));
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = new Vector2(x, y);
            rt.sizeDelta = node.type == MapNodeType.Boss ? new Vector2(156, 88) : new Vector2(132, 74);
            rt.anchoredPosition = Vector2.zero;
            button.interactable = reachable && !completed && !routeComplete;
            MapNode captured = node;
            button.onClick.AddListener(() => SelectMapNode(captured));
        }
    }

    private void CreateMapLine(MapNode from, MapNode to, bool active)
    {
        Image line = RuntimeUI.Panel(mapContent, "Path", active ? new Color(0.93f, 0.75f, 0.35f, 0.78f) : new Color(0.55f, 0.61f, 0.70f, 0.22f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        line.raycastTarget = false;
        RectTransform rt = line.GetComponent<RectTransform>();
        Vector2 a = MapLocalPosition(from);
        Vector2 b = MapLocalPosition(to);
        Vector2 delta = b - a;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (a + b) * 0.5f;
        rt.sizeDelta = new Vector2(delta.magnitude, active ? 7f : 5f);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private Vector2 MapLocalPosition(MapNode node)
    {
        RectTransform rect = mapContent.GetComponent<RectTransform>();
        Vector2 size = rect.rect.size;
        if (size.x < 100f || size.y < 100f)
            size = new Vector2(1600f, 600f);

        int rows = Mathf.Max(2, mapRows);
        int depthCount = Mathf.Max(4, mapDepthCount);
        float x = depthCount <= 1 ? 0.5f : Mathf.Lerp(0.07f, 0.93f, (float)node.depth / (depthCount - 1));
        float y = rows <= 1 ? 0.5f : Mathf.Lerp(0.20f, 0.80f, (float)node.row / (rows - 1));
        return new Vector2((x - 0.5f) * size.x, (y - 0.5f) * size.y);
    }

    private bool IsReachable(MapNode node)
    {
        if (node == null || completedNodes.Contains(node.id) || routeComplete)
            return false;
        if (node.depth == 0)
            return true;

        for (int i = 0; i < runMap.Length; i++)
        {
            if (!completedNodes.Contains(runMap[i].id) || string.IsNullOrEmpty(runMap[i].nextCsv))
                continue;
            string[] nextIds = runMap[i].nextCsv.Split(',');
            for (int j = 0; j < nextIds.Length; j++)
            {
                if (nextIds[j] == node.id)
                    return true;
            }
        }
        return false;
    }

    private void CompleteActiveNode()
    {
        if (activeNode == null)
            return;
        completedNodes.Add(activeNode.id);
        if (activeNode.type == MapNodeType.Boss)
        {
            routeComplete = true;
            ClearProgress();
        }
        activeNode = null;
    }

    private Color NodeColor(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Elite:
                return RuntimeUI.Coral;
            case MapNodeType.Rest:
                return RuntimeUI.Gold;
            case MapNodeType.Boss:
                return RuntimeUI.Hex("8F3E52");
            default:
                return RuntimeUI.Teal;
        }
    }

    private string MapNodeTypeLabel(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Elite:
                return "精英";
            case MapNodeType.Rest:
                return "休息";
            case MapNodeType.Boss:
                return "首领";
            default:
                return "普通";
        }
    }

    private MapNode FindMapNode(string id)
    {
        for (int i = 0; i < runMap.Length; i++)
        {
            if (runMap[i].id == id)
                return runMap[i];
        }
        return null;
    }

    private string NodeId(int depth, int row)
    {
        return depth + "-" + row;
    }

    private string FormatCurrentAnswers()
    {
        return currentAnswers != null && currentAnswers.Length > 0 ? string.Join(" / ", currentAnswers) : "没有答案";
    }

    private void RestartGame()
    {
        NewRun(true);
    }

    private void BackToHome()
    {
        SaveProgress();
        SceneManager.LoadScene("Home");
    }

    private void EnsureSystems()
    {
        if (wordDb == null)
            wordDb = FindObjectOfType<WordDatabase>();
        if (wordDb == null)
            wordDb = new GameObject("WordDatabase").AddComponent<WordDatabase>();
        if (WrongBook.Instance == null && FindObjectOfType<WrongBook>() == null)
            new GameObject("WrongBook").AddComponent<WrongBook>();
    }

    private void EnsureEnemyTemplates()
    {
        enemyTemplates = new[]
        {
            new EnemyTemplate { name = "Goblin Scribe", baseMaxHp = 3, isBoss = false, artResource = "Art/Enemies/enemy_goblin", faction = "Western Fantasy", minKills = 0, tier = 1 },
            new EnemyTemplate { name = "Dire Wolf", baseMaxHp = 4, isBoss = false, artResource = "Art/Enemies/enemy_dire_wolf", faction = "Western Fantasy", minKills = 1, tier = 1 },
            new EnemyTemplate { name = "Kappa Trickster", baseMaxHp = 4, isBoss = false, artResource = "Art/Enemies/enemy_kappa", faction = "Japanese Myth", minKills = 1, tier = 1 },
            new EnemyTemplate { name = "Paper Tengu", baseMaxHp = 5, isBoss = false, artResource = "Art/Enemies/enemy_tengu", faction = "Japanese Myth", minKills = 2, tier = 2 },
            new EnemyTemplate { name = "Wyvern Whelp", baseMaxHp = 5, isBoss = false, artResource = "Art/Enemies/enemy_wyvern", faction = "Western Fantasy", minKills = 2, tier = 2 },
            new EnemyTemplate { name = "Kitsune Adept", baseMaxHp = 6, isBoss = false, artResource = "Art/Enemies/enemy_kitsune", faction = "Japanese Myth", minKills = 3, tier = 2 },
            new EnemyTemplate { name = "Griffin Guard", baseMaxHp = 7, isBoss = false, artResource = "Art/Enemies/enemy_griffin", faction = "Western Fantasy", minKills = 4, tier = 3 },
            new EnemyTemplate { name = "Yuki-onna", baseMaxHp = 7, isBoss = false, artResource = "Art/Enemies/enemy_yuki_onna", faction = "Japanese Myth", minKills = 4, tier = 3 },
            new EnemyTemplate { name = "Lich Librarian", baseMaxHp = 10, isBoss = true, artResource = "Art/Enemies/enemy_lich", faction = "Western Boss", minKills = 5, tier = 3 },
            new EnemyTemplate { name = "Oni Warlord", baseMaxHp = 11, isBoss = true, artResource = "Art/Enemies/enemy_oni", faction = "Japanese Boss", minKills = 6, tier = 3 },
            new EnemyTemplate { name = "Nue Chimera", baseMaxHp = 12, isBoss = true, artResource = "Art/Enemies/enemy_nue", faction = "Japanese Boss", minKills = 7, tier = 3 },
            new EnemyTemplate { name = "Archdemon", baseMaxHp = 13, isBoss = true, artResource = "Art/Enemies/enemy_archdemon", faction = "Western Boss", minKills = 8, tier = 3 }
        };
    }

    private void SaveProgress()
    {
        if (routeComplete)
            return;
        RunProgress progress = new RunProgress
        {
            active = true,
            lang = selectedLang,
            seed = currentSeed,
            score = score,
            combo = combo,
            kills = kills,
            playerMaxHp = playerMaxHp,
            playerHp = playerHp,
            completedCsv = string.Join(",", new List<string>(completedNodes).ToArray())
        };
        PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(progress));
        PlayerPrefs.Save();
    }

    private bool TryLoadProgress()
    {
        string json = PlayerPrefs.GetString(ProgressKey, "");
        if (string.IsNullOrWhiteSpace(json))
            return false;
        RunProgress progress = JsonUtility.FromJson<RunProgress>(json);
        if (progress == null || !progress.active || progress.seed <= 0)
            return false;

        selectedLang = string.IsNullOrWhiteSpace(progress.lang) ? PlayerPrefs.GetString("selected_lang", "jp") : progress.lang;
        currentSeed = progress.seed;
        score = progress.score;
        combo = progress.combo;
        kills = progress.kills;
        playerMaxHp = progress.playerMaxHp > 0 ? progress.playerMaxHp : startPlayerMaxHp;
        playerHp = Mathf.Clamp(progress.playerHp, 1, playerMaxHp);
        completedNodes = ParseCompleted(progress.completedCsv);
        runMap = GenerateMap(currentSeed);
        routeComplete = false;
        waitingNext = false;
        gameOver = false;
        activeNode = null;
        SetPlayInteractable(false);
        UpdateHud();
        return true;
    }

    private HashSet<string> ParseCompleted(string csv)
    {
        HashSet<string> set = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(csv))
            return set;
        string[] parts = csv.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(parts[i]))
                set.Add(parts[i]);
        }
        return set;
    }

    private void ClearProgress()
    {
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();
    }
}
