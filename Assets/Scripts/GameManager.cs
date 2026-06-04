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
    [SerializeField] private TMP_Text growthText;
    [SerializeField] private TMP_Text relicText;
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
    private int gold;
    private int xp;
    private int level = 1;
    private int damageToEnemy = 1;
    private int currentSeed;
    private bool gameOver;
    private bool waitingNext;
    private bool routeComplete;
    private bool answerRevealed;
    private bool firstMissBlockedThisBattle;
    private bool phoenixLeafUsed;
    private bool reviewBattle;
    private string activeReviewWrongId;
    private float timeLeft;
    private float currentQuestionLimit;
    private float nextQuestionTimePenalty;

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
    private GameObject choiceRoot;
    private Transform choiceContent;
    private Transform mapContent;
    private TMP_Text mapTitleText;
    private TMP_Text mapStatusText;
    private MapNode[] runMap = System.Array.Empty<MapNode>();
    private HashSet<string> completedNodes = new HashSet<string>();
    private HashSet<string> relics = new HashSet<string>();
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
        growthText = AddStat(top.transform, "Lv1  金币 0");

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
        relicText = RuntimeUI.Text(card.transform, "Relics", "遗物: 无", 20, RuntimeUI.Hex("7B6542"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(relicText.gameObject, 26, 34);
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
        BuildChoiceOverlay(canvas.transform);
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

    private void BuildChoiceOverlay(Transform parent)
    {
        Image root = RuntimeUI.Panel(parent, "Choice Overlay", new Color(0.01f, 0.015f, 0.025f, 0.88f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        choiceRoot = root.gameObject;

        Image panel = RuntimeUI.Panel(root.transform, "Choice Panel", new Color(0.965f, 0.91f, 0.76f, 0.96f), new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.80f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(panel.gameObject, 34, 18);
        TMP_Text title = RuntimeUI.Text(panel.transform, "Choice Title", "选择奖励", 54, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(title.gameObject, 72, 88);
        TMP_Text tip = RuntimeUI.Text(panel.transform, "Choice Tip", "选择一个效果加入本轮冒险。", 26, RuntimeUI.Hex("6C5632"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(tip.gameObject, 42, 54);

        GameObject row = new GameObject("Choice Row", typeof(RectTransform));
        row.transform.SetParent(panel.transform, false);
        RuntimeUI.AddHorizontalLayout(row, 0, 18);
        RuntimeUI.Layout(row, 220, 270);
        choiceContent = row.transform;

        choiceRoot.SetActive(false);
    }

    private void ShowRelicChoice(string reason)
    {
        if (choiceRoot == null || choiceContent == null)
            return;

        choiceRoot.SetActive(true);
        ClearChoiceButtons();
        RelicDef[] options = RollRelicOptions(3);
        for (int i = 0; i < options.Length; i++)
        {
            RelicDef relic = options[i];
            Button button = RuntimeUI.Button(choiceContent, relic.name, relic.name + "\n" + relic.description, RuntimeUI.Hex("8A6A3E"), Color.white);
            RuntimeUI.Layout(button.gameObject, 190, 240, 1);
            button.onClick.AddListener(() => PickRelic(relic, reason));
        }
    }

    private void ClearChoiceButtons()
    {
        for (int i = choiceContent.childCount - 1; i >= 0; i--)
            Destroy(choiceContent.GetChild(i).gameObject);
    }

    private RelicDef[] RollRelicOptions(int count)
    {
        List<RelicDef> pool = new List<RelicDef>();
        for (int i = 0; i < RelicCatalog.All.Length; i++)
        {
            if (!relics.Contains(RelicCatalog.All[i].id))
                pool.Add(RelicCatalog.All[i]);
        }

        if (pool.Count == 0)
            pool.AddRange(RelicCatalog.All);

        List<RelicDef> result = new List<RelicDef>();
        while (result.Count < count && pool.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return result.ToArray();
    }

    private void PickRelic(RelicDef relic, string reason)
    {
        if (relic != null)
            relics.Add(relic.id);

        if (choiceRoot)
            choiceRoot.SetActive(false);

        UpdateHud();
        SaveProgress();
        ShowMap(reason + " 获得遗物: " + (relic != null ? relic.name : "无"));
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
        gold = 0;
        xp = 0;
        level = 1;
        playerMaxHp = startPlayerMaxHp;
        playerHp = playerMaxHp;
        activeNode = null;
        routeComplete = false;
        waitingNext = false;
        gameOver = false;
        completedNodes.Clear();
        relics.Clear();
        phoenixLeafUsed = false;
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

        if (node.type == MapNodeType.Chest)
        {
            gold += 35;
            GainXp(12);
            CompleteActiveNode();
            UpdateHud();
            SaveProgress();
            ShowRelicChoice("宝箱开启，金币 +35。");
            return;
        }

        if (node.type == MapNodeType.Shop)
        {
            ResolveShopNode();
            return;
        }

        if (node.type == MapNodeType.Event)
        {
            ResolveEventNode();
            return;
        }

        if (node.type == MapNodeType.Review)
        {
            WrongEntry review = WrongBook.Instance != null ? WrongBook.Instance.GetRandomForLang(selectedLang) : null;
            if (review == null)
            {
                gold += 20;
                CompleteActiveNode();
                UpdateHud();
                SaveProgress();
                ShowMap("复习节点没有错题，转化为金币 +20。");
                return;
            }

            reviewBattle = true;
            activeReviewWrongId = review.id;
            if (mapRoot) mapRoot.SetActive(false);
            SetPlayInteractable(true);
            SpawnEnemy();
            currentEnemy.maxHp = 1;
            currentEnemy.hp = 1;
            currentEntry = new Entry { lang = review.lang, prompt = review.prompt, answers = review.answers, difficulty = 1 };
            currentAnswers = review.answers ?? System.Array.Empty<string>();
            currentQuestionLimit = questionTimeLimit + RelicTimeBonus();
            timeLeft = currentQuestionLimit;
            wordText.text = currentEntry.prompt;
            masteryText.text = "错题挑战";
            difficultyText.text = "连续答对 2 次后移出错题本";
            hintText.text = string.IsNullOrWhiteSpace(review.lastWrongAnswer) ? "复习最近错题。" : "上次错写: " + review.lastWrongAnswer;
            answerPreviewText.text = "";
            UpdateHud();
            if (answerInput) answerInput.ActivateInputField();
            return;
        }

        if (mapRoot) mapRoot.SetActive(false);
        SetPlayInteractable(true);
        reviewBattle = false;
        activeReviewWrongId = "";
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

        AnswerResult result = logic.JudgeBest(answerInput != null ? answerInput.text : "", currentAnswers);
        if (result.grade == AnswerGrade.Exact && !answerRevealed)
            ResolveHit();
        else if (result.grade == AnswerGrade.Exact)
            ResolveAssistedHit();
        else if (result.grade == AnswerGrade.Near)
            ResolveNear(result);
        else
            ResolveMiss("错误");
    }

    private void ResolveShopNode()
    {
        string result;
        if (gold >= 45 && playerHp < playerMaxHp)
        {
            gold -= 45;
            playerHp = Mathf.Min(playerMaxHp, playerHp + 2);
            result = "商店购买治疗，金币 -45，生命 +2。";
        }
        else if (gold >= 60)
        {
            gold -= 60;
            CompleteActiveNode();
            UpdateHud();
            SaveProgress();
            ShowRelicChoice("商店购买遗物，金币 -60。");
            return;
        }
        else
        {
            gold += 10;
            result = "金币不足，商人赠送路费 +10。";
        }

        CompleteActiveNode();
        UpdateHud();
        SaveProgress();
        ShowMap(result);
    }

    private void ResolveEventNode()
    {
        int roll = UnityEngine.Random.Range(0, 3);
        string result;
        if (roll == 0)
        {
            gold += 30;
            GainXp(15);
            result = "事件: 学习 3 个新词，金币 +30，经验 +15。";
        }
        else if (roll == 1)
        {
            nextQuestionTimePenalty = 5f;
            gold += 55;
            result = "事件: 接受限时挑战，下一题时间 -5 秒，金币 +55。";
        }
        else
        {
            playerHp = Mathf.Max(1, playerHp - 1);
            GainXp(35);
            result = "事件: 古书试炼，生命 -1，经验 +35。";
        }

        CompleteActiveNode();
        UpdateHud();
        SaveProgress();
        ShowMap(result);
    }

    private void GainXp(int amount)
    {
        xp += Mathf.Max(0, amount);
        while (xp >= level * 60)
        {
            xp -= level * 60;
            level++;
            playerMaxHp++;
            playerHp = Mathf.Min(playerMaxHp, playerHp + 1);
        }
    }

    private bool HasRelic(string id)
    {
        return relics.Contains(id);
    }

    private float RelicTimeBonus()
    {
        return HasRelic("hourglass") ? 3f : 0f;
    }

    private string RelicSummary()
    {
        if (relics.Count == 0)
            return "无";

        List<string> names = new List<string>();
        foreach (string id in relics)
        {
            RelicDef relic = RelicCatalog.Find(id);
            if (relic != null)
                names.Add(relic.name);
        }
        return names.Count == 0 ? "无" : string.Join(" / ", names.ToArray());
    }

    private void ResolveNear(AnswerResult result)
    {
        hintText.text = result.message + "，再试一次";
        answerPreviewText.text = "接近正确，不扣生命";
        timeLeft = Mathf.Max(3f, timeLeft - 1.5f);
        StartCoroutine(Shake(questionPanelRect, 8f, 0.12f));
        UpdateTimerBar();
        if (answerInput)
        {
            answerInput.text = "";
            answerInput.ActivateInputField();
        }
    }

    private void ResolveHit()
    {
        WordProgressStore.Record(selectedLang, currentEntry.prompt, true);
        string answer = FormatCurrentAnswers();
        combo++;
        string speedLabel;
        string comboLabel;
        int damage = ComputeAnswerDamage(out speedLabel, out comboLabel);
        score += baseScorePerHit * Mathf.Max(1, combo) + damage * 6;
        currentEnemy.hp -= damage;
        hintText.text = speedLabel + "  " + comboLabel;
        answerPreviewText.text = "正确写法: " + answer + " / 伤害 " + damage;
        StartCoroutine(PulseEnemy(damage >= 5 ? RuntimeUI.Gold : new Color(1f, 0.78f, 0.70f, 1f)));
        StartCoroutine(Shake(enemyPanelRect, damage >= 5 ? 24f : 14f, damage >= 5 ? 0.22f : 0.15f));
        FinishHitIfEnemyDefeated();
        if (activeNode != null && mapRoot != null && !mapRoot.activeSelf)
            answerPreviewText.text = "上一题: " + answer + " / 伤害 " + damage;
    }

    private int ComputeAnswerDamage(out string speedLabel, out string comboLabel)
    {
        float elapsed = Mathf.Max(0f, currentQuestionLimit - timeLeft);
        int damage;
        if (elapsed <= 5f)
        {
            damage = 3;
            speedLabel = "神速答题";
        }
        else if (elapsed <= 10f)
        {
            damage = 2;
            speedLabel = "快速答题";
        }
        else
        {
            damage = 1;
            speedLabel = "稳住命中";
        }

        comboLabel = "连击 " + combo;

        if (combo % 10 == 0)
        {
            damage += 5;
            score += 120;
            comboLabel = "10 连击特殊攻击";
        }
        else if (combo % 5 == 0)
        {
            if (playerHp < playerMaxHp)
                playerHp++;
            score += 50;
            comboLabel = "5 连击恢复生命";
        }
        else if (combo % 3 == 0)
        {
            damage *= 2;
            score += 30;
            comboLabel = "3 连击暴击";
        }

        if (HasRelic("combo_sword") && combo % 3 == 0)
        {
            damage += 1;
            comboLabel += " + 连击剑";
        }

        if (HasRelic("review_book") && (reviewBattle || WordProgressStore.Label(selectedLang, currentEntry.prompt) == "需复习"))
        {
            damage += 1;
            comboLabel += " + 复习之书";
        }

        return damage;
    }

    private void ResolveAssistedHit()
    {
        WordProgressStore.Record(selectedLang, currentEntry.prompt, false);
        string answer = FormatCurrentAnswers();
        combo = 0;
        score += Mathf.Max(2, baseScorePerHit / 3);
        currentEnemy.hp -= damageToEnemy;
        hintText.text = "看过答案后命中，已标为需复习";
        answerPreviewText.text = "正确写法: " + answer;
        StartCoroutine(PulseEnemy(new Color(1f, 0.85f, 0.58f, 1f)));
        FinishHitIfEnemyDefeated();
        if (activeNode != null && mapRoot != null && !mapRoot.activeSelf)
            answerPreviewText.text = "上一题: " + answer + " / 需复习";
    }

    private void FinishHitIfEnemyDefeated()
    {
        if (currentEnemy.hp <= 0)
        {
            bool rewardRelic = activeNode != null && (activeNode.type == MapNodeType.Elite || activeNode.type == MapNodeType.Boss);
            kills++;
            int goldReward = currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? 50 : (activeNode != null && activeNode.type == MapNodeType.Elite ? 35 : 18);
            if (HasRelic("coin_purse"))
                goldReward = Mathf.RoundToInt(goldReward * 1.5f);
            gold += goldReward;
            GainXp(currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? 45 : (activeNode != null && activeNode.type == MapNodeType.Elite ? 30 : 15));
            score += currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? 120 : (activeNode != null && activeNode.type == MapNodeType.Elite ? 70 : 35);
            hintText.text = currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? "首领击败" : "战斗胜利";
            if (reviewBattle && !string.IsNullOrEmpty(activeReviewWrongId) && WrongBook.Instance != null)
                WrongBook.Instance.MarkReviewCorrect(activeReviewWrongId, 2);
            CompleteActiveNode();
            UpdateHud();
            SaveProgress();
            if (rewardRelic)
                ShowRelicChoice("强敌击败，金币 +" + goldReward + "。");
            else
                ShowMap(routeComplete ? "本轮通关！可以生成新路线继续挑战。" : "胜利！金币 +" + goldReward + "，选择下一关。");
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
        string penaltyLabel;
        int hpLoss = ApplyEnemyPenalty(out penaltyLabel);
        playerHp = Mathf.Max(0, playerHp - hpLoss);
        if (playerHp <= 0 && HasRelic("phoenix_leaf") && !phoenixLeafUsed)
        {
            phoenixLeafUsed = true;
            playerHp = Mathf.Min(playerMaxHp, 2);
            penaltyLabel += "，凤羽触发，生命回复 2";
        }

        if (WrongBook.Instance != null)
            WrongBook.Instance.AddOrUpdate(selectedLang, currentEntry.prompt, currentAnswers, answerInput != null ? answerInput.text : "");

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
        hintText.text = reason + " - " + penaltyLabel;
        answerPreviewText.text = "答案: " + FormatCurrentAnswers();
        StartCoroutine(Shake(questionPanelRect, 18f, 0.20f));
        SetPlayInteractable(false);
        if (nextButton) nextButton.interactable = true;
        UpdateHud();
        SaveProgress();
    }

    private int ApplyEnemyPenalty(out string penaltyLabel)
    {
        if (HasRelic("amulet") && !firstMissBlockedThisBattle)
        {
            firstMissBlockedThisBattle = true;
            penaltyLabel = "护身符抵挡了本场第一次错误";
            return 0;
        }

        int hpLoss = 1;
        penaltyLabel = "生命 -1";

        if (currentEnemyTemplate == null)
            return hpLoss;

        if (currentEnemyTemplate.isBoss)
        {
            hpLoss = 2;
            score = Mathf.Max(0, score - 25);
            penaltyLabel = "首领反击，生命 -2，分数 -25";
        }
        else if (currentEnemyTemplate.tier >= 3)
        {
            hpLoss = 2;
            penaltyLabel = "高阶怪物反击，生命 -2";
        }
        else if (currentEnemyTemplate.tier >= 2)
        {
            score = Mathf.Max(0, score - 10);
            penaltyLabel = "精英压制，生命 -1，分数 -10";
        }

        if (!string.IsNullOrEmpty(currentEnemyTemplate.faction) && currentEnemyTemplate.faction.Contains("Japanese"))
        {
            nextQuestionTimePenalty = Mathf.Max(nextQuestionTimePenalty, 2f);
            penaltyLabel += "，下一题时间 -2 秒";
        }

        return hpLoss;
    }

    private void NextWord()
    {
        int minDifficulty;
        int maxDifficulty;
        GetDifficultyWindow(out minDifficulty, out maxDifficulty);
        currentEntry = wordDb != null ? wordDb.GetLearningEntryByLangAndDifficulty(selectedLang, minDifficulty, maxDifficulty) : null;
        currentQuestionLimit = Mathf.Max(7f, questionTimeLimit + RelicTimeBonus() - Mathf.Min(5f, kills * 0.3f) - nextQuestionTimePenalty);
        nextQuestionTimePenalty = 0f;
        timeLeft = currentQuestionLimit;
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
        if (reviewBattle && currentEntry != null)
        {
            currentQuestionLimit = questionTimeLimit + RelicTimeBonus();
            timeLeft = currentQuestionLimit;
            hintText.text = "继续复习这道错题。";
            answerPreviewText.text = "";
            UpdateTimerBar();
        }
        else
        {
            NextWord();
        }
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
        firstMissBlockedThisBattle = false;
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
        if (growthText) growthText.text = "Lv" + level + "  XP " + xp + "/" + (level * 60) + "  金币 " + gold;
        if (relicText) relicText.text = "遗物: " + RelicSummary();
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

        float limit = Mathf.Max(1f, currentQuestionLimit > 0f ? currentQuestionLimit : questionTimeLimit - Mathf.Min(5f, kills * 0.3f));
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
        if (depth > 1 && roll < 0.12)
            return MapNodeType.Rest;
        if (depth > 1 && roll < 0.22)
            return MapNodeType.Shop;
        if (depth > 1 && roll < 0.34)
            return MapNodeType.Event;
        if (depth > 1 && roll < 0.44)
            return MapNodeType.Chest;
        if (depth > 1 && roll < 0.56)
            return MapNodeType.Review;
        if (roll < 0.76)
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
            case MapNodeType.Shop:
                return RuntimeUI.Hex("5E8C61");
            case MapNodeType.Event:
                return RuntimeUI.Hex("7B68A6");
            case MapNodeType.Chest:
                return RuntimeUI.Hex("B88945");
            case MapNodeType.Review:
                return RuntimeUI.Hex("5D8DA8");
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
            case MapNodeType.Shop:
                return "商店";
            case MapNodeType.Event:
                return "事件";
            case MapNodeType.Chest:
                return "宝箱";
            case MapNodeType.Review:
                return "复习";
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
            gold = gold,
            xp = xp,
            level = level,
            playerMaxHp = playerMaxHp,
            playerHp = playerHp,
            completedCsv = string.Join(",", new List<string>(completedNodes).ToArray()),
            relicCsv = string.Join(",", new List<string>(relics).ToArray())
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
        gold = progress.gold;
        xp = progress.xp;
        level = progress.level > 0 ? progress.level : 1;
        playerMaxHp = progress.playerMaxHp > 0 ? progress.playerMaxHp : startPlayerMaxHp;
        playerHp = Mathf.Clamp(progress.playerHp, 1, playerMaxHp);
        completedNodes = ParseCompleted(progress.completedCsv);
        relics = ParseCsvSet(progress.relicCsv);
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
        return ParseCsvSet(csv);
    }

    private HashSet<string> ParseCsvSet(string csv)
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
