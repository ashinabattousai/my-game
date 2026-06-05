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
    [SerializeField] private float questionTimeLimit = 60f;
    [SerializeField] private float wrongAnswerReviewDelay = 2.8f;
    [SerializeField] private bool useRuntimeUi = true;
    [SerializeField] private int mapDepthCount = 13;
    [SerializeField] private int mapRows = 3;
    [SerializeField] private bool hideUnvisitedMapNodes = false;
    [SerializeField] private int maxRelicSlots = 3;
    [SerializeField] private string backgroundMusicResource = "Audio/bgm_enishi";
    [SerializeField] private float backgroundMusicVolume = 0.42f;

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
    private int hintCharges = 2;
    private int nextBattleTimeBonus;
    private int nextBattleTimePenalty;
    private int damageToEnemy = 1;
    private int currentSeed;
    private bool gameOver;
    private bool waitingNext;
    private bool routeComplete;
    private bool answerRevealed;
    private bool firstMissBlockedThisBattle;
    private bool phoenixLeafUsed;
    private bool reviewBattle;
    private bool eventReviewGamble;
    private bool repeaterPending;
    private bool glassSwordBroken;
    private string activeReviewWrongId;
    private string activeEnemyRule = "normal";
    private int questionIndexInBattle;
    private float timeLeft;
    private float currentQuestionLimit;
    private float nextQuestionTimePenalty;

    private EnemyState currentEnemy = new EnemyState();
    private EnemyTemplate currentEnemyTemplate;
    private RawImage enemyImage;
    private Image enemyHpFill;
    private Image enemyHpTrailFill;
    private RectTransform enemyHpFillRect;
    private RectTransform enemyHpTrailFillRect;
    private Image playerHpFill;
    private Image playerHpTrailFill;
    private RectTransform playerHpFillRect;
    private RectTransform playerHpTrailFillRect;
    private Image timerFill;
    private TMP_Text answerPreviewText;
    private TMP_Text masteryText;
    private TMP_Text studyPromptText;
    private TMP_Text enemyHpBarText;
    private TMP_Text playerHpBarText;
    private Button hintButton;
    private Button revealButton;
    private RectTransform enemyPanelRect;
    private RectTransform questionPanelRect;
    private RectTransform canvasRect;
    private Image transitionImage;
    private Coroutine enemyIdleRoutine;
    private float enemyHpTarget = 1f;
    private float enemyHpVisible = 1f;
    private float enemyHpTrailVisible = 1f;
    private float playerHpTarget = 1f;
    private float playerHpVisible = 1f;
    private float playerHpTrailVisible = 1f;
    private float timerTarget = 1f;

    private GameObject mapRoot;
    private GameObject choiceRoot;
    private Transform choiceContent;
    private TMP_Text choiceTitleText;
    private TMP_Text choiceTipText;
    private Transform mapContent;
    private TMP_Text mapTitleText;
    private TMP_Text mapStatusText;
    private Texture2D mapStampTexture;
    private Texture2D mapFrameTexture;
    private AudioSource musicSource;
    private MapNode[] runMap = System.Array.Empty<MapNode>();
    private HashSet<string> completedNodes = new HashSet<string>();
    private HashSet<string> relics = new HashSet<string>();
    private List<string> relicSlots = new List<string>();
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

        StartBackgroundMusic();
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
        canvasRect = canvas.GetComponent<RectTransform>();
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
        enemyHpFill = CreateHpBar(stage.transform, out enemyHpTrailFill, RuntimeUI.Hex("F01822"), RuntimeUI.Hex("FF7A72"), RuntimeUI.Hex("330B10"));
        enemyHpFillRect = enemyHpFill.GetComponent<RectTransform>();
        enemyHpTrailFillRect = enemyHpTrailFill.GetComponent<RectTransform>();
        RuntimeUI.Layout(enemyHpFill.transform.parent.gameObject, 34, 44);
        enemyHpBarText = RuntimeUI.Text(enemyHpFill.transform.parent, "Enemy HP Label", "敌人 0/0", 20, RuntimeUI.Paper, TextAlignmentOptions.Center);
        RuntimeUI.SetRect(enemyHpBarText, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image card = RuntimeUI.Panel(canvas.transform, "Study Card", new Color(0.965f, 0.91f, 0.76f, 0.94f), new Vector2(0.425f, 0.12f), new Vector2(0.945f, 0.84f), Vector2.zero, Vector2.zero);
        card.gameObject.AddComponent<DraggablePanel>();
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

        playerHpFill = CreateHpBar(card.transform, out playerHpTrailFill, RuntimeUI.Hex("32D26B"), RuntimeUI.Hex("8BF0A9"), RuntimeUI.Hex("17351F"));
        playerHpFillRect = playerHpFill.GetComponent<RectTransform>();
        playerHpTrailFillRect = playerHpTrailFill.GetComponent<RectTransform>();
        RuntimeUI.Layout(playerHpFill.transform.parent.gameObject, 28, 38);
        playerHpBarText = RuntimeUI.Text(playerHpFill.transform.parent, "Player HP Label", "玩家生命 5/5", 18, RuntimeUI.Paper, TextAlignmentOptions.Center);
        RuntimeUI.SetRect(playerHpBarText, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        timerFill = CreateBar(card.transform, RuntimeUI.Hex("56B8FF"), RuntimeUI.Hex("CDBB8D"));
        RuntimeUI.Layout(timerFill.transform.parent.gameObject, 10, 16);
        answerInput = RuntimeUI.InputField(card.transform, "输入答案后按 Enter");
        RuntimeUI.Layout(answerInput.gameObject, 68, 82);

        GameObject studyButtons = new GameObject("Study Buttons", typeof(RectTransform));
        studyButtons.transform.SetParent(card.transform, false);
        RuntimeUI.AddHorizontalLayout(studyButtons, 0, 12);
        RuntimeUI.Layout(studyButtons, 58, 72);
        hintButton = RuntimeUI.Button(studyButtons.transform, "Hint", "提示", RuntimeUI.Hex("65748A"), Color.white);
        revealButton = RuntimeUI.Button(studyButtons.transform, "Reveal", "看答案", RuntimeUI.Hex("8A6A3E"), Color.white);
        submitButton = RuntimeUI.Button(studyButtons.transform, "Submit", "提交", RuntimeUI.Teal, Color.white);

        AddTooltip(hintButton, "消耗一次提示，显示答案线索。");
        AddTooltip(revealButton, "直接看正确写法，但本题会标为需复习。");
        AddTooltip(submitButton, "提交答案。连击和答题速度会影响伤害。");

        GameObject runButtons = new GameObject("Run Buttons", typeof(RectTransform));
        runButtons.transform.SetParent(card.transform, false);
        RuntimeUI.AddHorizontalLayout(runButtons, 0, 12);
        RuntimeUI.Layout(runButtons, 58, 72);
        nextButton = RuntimeUI.Button(runButtons.transform, "Next", "下一题", RuntimeUI.Gold, RuntimeUI.Ink);
        restartButton = RuntimeUI.Button(runButtons.transform, "Restart", "新路线", RuntimeUI.Coral, Color.white);
        backHomeButton = RuntimeUI.Button(runButtons.transform, "Home", "退出", RuntimeUI.Hex("2E394B"), Color.white);

        AddTooltip(nextButton, "查看完答案后进入下一题。");
        AddTooltip(restartButton, "放弃当前路线并生成更长的新路线。");
        AddTooltip(backHomeButton, "退出到首页，当前进度会保留。");

        BuildMapOverlay(canvas.transform);
        BuildChoiceOverlay(canvas.transform);
        BuildTransitionOverlay(canvas.transform);
    }

    private void AddTooltip(Button button, string text)
    {
        if (button == null)
            return;

        TooltipTrigger trigger = button.gameObject.GetComponent<TooltipTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<TooltipTrigger>();
        trigger.Text = text;
    }

    private void BuildTransitionOverlay(Transform parent)
    {
        transitionImage = RuntimeUI.Panel(parent, "Transition Overlay", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        transitionImage.raycastTarget = false;
        transitionImage.color = new Color(0f, 0f, 0f, 0f);
        transitionImage.gameObject.SetActive(false);
    }

    private IEnumerator TransitionFlash()
    {
        if (transitionImage == null)
            yield break;

        transitionImage.gameObject.SetActive(true);
        float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = t < 0.45f ? Mathf.Lerp(0f, 0.45f, t / 0.45f) : Mathf.Lerp(0.45f, 0f, (t - 0.45f) / 0.55f);
            transitionImage.color = new Color(0f, 0f, 0f, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transitionImage.color = new Color(0f, 0f, 0f, 0f);
        transitionImage.gameObject.SetActive(false);
    }

    private void BuildMapOverlay(Transform parent)
    {
        mapStampTexture = RuntimeUI.LoadTexture("Art/Map/map_stamps");
        mapFrameTexture = RuntimeUI.LoadTexture("Art/Map/map_frame");
        Image root = RuntimeUI.Panel(parent, "Route Map", new Color(0.012f, 0.013f, 0.018f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        mapRoot = root.gameObject;

        if (mapFrameTexture != null)
        {
            RawImage frame = RuntimeUI.Background(root.transform, mapFrameTexture);
            frame.color = Color.white;
        }

        Image header = RuntimeUI.Panel(root.transform, "Map Header", new Color(0f, 0f, 0f, 0f), new Vector2(0.28f, 0.82f), new Vector2(0.89f, 0.93f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(header.gameObject, 0, 6);
        mapTitleText = RuntimeUI.Text(header.transform, "Map Title", "选择路线", 62, RuntimeUI.Paper, TextAlignmentOptions.Center);
        RuntimeUI.Layout(mapTitleText.gameObject, 74, 86);
        mapStatusText = RuntimeUI.Text(header.transform, "Map Status", "选择一个可到达的关卡。", 28, RuntimeUI.Hex("B8C2D0"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(mapStatusText.gameObject, 42, 54);

        Image content = RuntimeUI.Panel(root.transform, "Map Content", new Color(0f, 0f, 0f, 0f), new Vector2(0.275f, 0.235f), new Vector2(0.905f, 0.785f), Vector2.zero, Vector2.zero);
        content.raycastTarget = false;
        mapContent = content.transform;

        Image sidePanel = RuntimeUI.Panel(root.transform, "Map Side Actions", new Color(0f, 0f, 0f, 0f), new Vector2(0.07f, 0.22f), new Vector2(0.245f, 0.75f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(sidePanel.gameObject, 26, 18);
        TMP_Text sideTitle = RuntimeUI.Text(sidePanel.transform, "Side Title", "远征", 34, RuntimeUI.Paper, TextAlignmentOptions.Center);
        RuntimeUI.Layout(sideTitle.gameObject, 52, 66);
        Button restartMap = RuntimeUI.Button(sidePanel.transform, "Restart Route", "生成新路线", RuntimeUI.Coral, Color.white);
        Button back = RuntimeUI.Button(sidePanel.transform, "Back Home", "返回首页", RuntimeUI.Hex("2E394B"), Color.white);
        RuntimeUI.Layout(restartMap.gameObject, 66, 84);
        RuntimeUI.Layout(back.gameObject, 66, 84);
        restartMap.onClick.AddListener(RestartGame);
        back.onClick.AddListener(BackToHome);
    }

    private void BuildChoiceOverlay(Transform parent)
    {
        Image root = RuntimeUI.Panel(parent, "Choice Overlay", new Color(0.01f, 0.015f, 0.025f, 0.88f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        choiceRoot = root.gameObject;

        Image panel = RuntimeUI.Panel(root.transform, "Choice Panel", new Color(0.965f, 0.91f, 0.76f, 0.96f), new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.80f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(panel.gameObject, 34, 18);
        choiceTitleText = RuntimeUI.Text(panel.transform, "Choice Title", "选择奖励", 54, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(choiceTitleText.gameObject, 72, 88);
        choiceTipText = RuntimeUI.Text(panel.transform, "Choice Tip", "选择一个效果加入本轮冒险。", 26, RuntimeUI.Hex("6C5632"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(choiceTipText.gameObject, 42, 54);

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

        TooltipTrigger.HideAll();
        choiceRoot.SetActive(true);
        if (choiceTitleText) choiceTitleText.text = "选择遗物";
        if (choiceTipText) choiceTipText.text = relicSlots.Count >= maxRelicSlots ? "遗物槽已满，选择新遗物会替换最早装备的遗物。" : "选择一个效果加入本轮构筑。";
        ClearChoiceButtons();
        RelicDef[] options = RollRelicOptions(3);
        for (int i = 0; i < options.Length; i++)
        {
            RelicDef relic = options[i];
            Button button = RuntimeUI.Button(choiceContent, relic.name, relic.name + "\n" + relic.description, RuntimeUI.Hex("8A6A3E"), Color.white);
            RuntimeUI.Layout(button.gameObject, 190, 240, 1);
            button.onClick.AddListener(() => PickRelic(relic, reason));
        }

        if (relicSlots.Count >= maxRelicSlots)
        {
            Button keep = RuntimeUI.Button(choiceContent, "Keep Relics", "保留当前遗物\n离开奖励", RuntimeUI.Hex("2E394B"), Color.white);
            RuntimeUI.Layout(keep.gameObject, 190, 240, 1);
            keep.onClick.AddListener(() =>
            {
                if (choiceRoot)
                    choiceRoot.SetActive(false);
                ShowMap(reason + " 已保留当前遗物。");
            });
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
        {
            EquipRelic(relic.id);
            LongTermProgress.RecordRelic(relic.id);
        }

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

    private Image CreateHpBar(Transform parent, out Image trailFill, Color fillColor, Color trailColor, Color backColor)
    {
        Image back = RuntimeUI.Panel(parent, "HP Bar", backColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        trailFill = CreateBarFill(back.transform, "Damage Trail", trailColor);
        Image fill = CreateBarFill(back.transform, "HP Fill", fillColor);
        return fill;
    }

    private Image CreateBarFill(Transform parent, string name, Color color)
    {
        GameObject fillGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(parent, false);
        Image fill = fillGo.GetComponent<Image>();
        fill.color = color;
        fill.raycastTarget = false;
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
        hintCharges = 2;
        nextBattleTimeBonus = 0;
        nextBattleTimePenalty = 0;
        playerMaxHp = startPlayerMaxHp;
        playerHp = playerMaxHp;
        SnapPlayerHpBar();
        activeNode = null;
        routeComplete = false;
        waitingNext = false;
        gameOver = false;
        completedNodes.Clear();
        relics.Clear();
        relicSlots.Clear();
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
        TooltipTrigger.HideAll();
        if (node == null || !IsReachable(node) || completedNodes.Contains(node.id) || routeComplete)
            return;

        activeNode = node;
        waitingNext = false;
        gameOver = false;
        StartCoroutine(TransitionFlash());
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
            eventReviewGamble = false;
            activeReviewWrongId = review.id;
            if (mapRoot) mapRoot.SetActive(false);
            SetPlayInteractable(true);
            SpawnEnemy();
            currentEnemy.maxHp = 1;
            currentEnemy.hp = 1;
            currentEntry = new Entry { lang = review.lang, prompt = review.prompt, answers = review.answers, difficulty = 1 };
            currentAnswers = review.answers ?? System.Array.Empty<string>();
            currentQuestionLimit = Mathf.Clamp(questionTimeLimit + RelicTimeBonus(), 10f, 60f);
            timeLeft = currentQuestionLimit;
            RuntimeUI.RegisterTextCharacters(currentEntry.prompt);
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
        eventReviewGamble = false;
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
        ShowShopChoice();
    }

    private void ResolveEventNode()
    {
        ShowEventChoice();
    }

    private void ShowShopChoice()
    {
        if (choiceRoot == null || choiceContent == null)
            return;

        choiceRoot.SetActive(true);
        if (choiceTitleText) choiceTitleText.text = "商店";
        if (choiceTipText) choiceTipText.text = "金币有限，选择最适合当前路线的补给。";
        ClearChoiceButtons();
        AddChoiceButton("治疗\n45 金币 / 生命 +2", RuntimeUI.Teal, BuyShopHeal);
        AddChoiceButton("随机遗物\n60 金币", RuntimeUI.Hex("8A6A3E"), BuyShopRelic);
        AddChoiceButton("删除错题\n30 金币", RuntimeUI.Gold, BuyShopRemoveWrong);
        AddChoiceButton("下战加时\n25 金币 / +5 秒", RuntimeUI.Hex("65748A"), BuyShopTime);
        AddChoiceButton("补充提示\n20 金币 / +2 次", RuntimeUI.Hex("5D8DA8"), BuyShopHints);
        AddChoiceButton("离开", RuntimeUI.Hex("2E394B"), LeaveShop);
    }

    private void ShowEventChoice()
    {
        if (choiceRoot == null || choiceContent == null)
            return;

        choiceRoot.SetActive(true);
        if (choiceTitleText) choiceTitleText.text = "事件";
        if (choiceTipText) choiceTipText.text = "选择风险和收益，结果由你承担。";
        ClearChoiceButtons();
        AddChoiceButton("古书试炼\n生命 -1 / 经验 +70", RuntimeUI.Hex("7B68A6"), EventStudyTrial);
        AddChoiceButton("时间交易\n下战 -5 秒 / 金币 +80", RuntimeUI.Coral, EventTimeTrade);
        AddChoiceButton("错题赌局\n成功得遗物 / 失败扣血", RuntimeUI.Hex("5D8DA8"), EventReviewGamble);
        AddChoiceButton("安全离开\n金币 +10", RuntimeUI.Hex("2E394B"), EventLeave);
    }

    private void AddChoiceButton(string label, Color color, UnityEngine.Events.UnityAction action)
    {
        Button button = RuntimeUI.Button(choiceContent, "Choice", label, color, Color.white);
        RuntimeUI.Layout(button.gameObject, 170, 220, 1);
        button.onClick.AddListener(action);
    }

    private void BuyShopHeal()
    {
        if (!TrySpendGold(45, "金币不足，无法治疗。"))
            return;
        playerHp = Mathf.Min(playerMaxHp, playerHp + 2);
        CompleteShopNode("购买治疗，生命 +2。");
    }

    private void BuyShopRelic()
    {
        if (!TrySpendGold(60, "金币不足，无法购买遗物。"))
            return;
        CompleteActiveNode();
        UpdateHud();
        SaveProgress();
        if (choiceRoot) choiceRoot.SetActive(false);
        ShowRelicChoice("商店购买遗物。");
    }

    private void BuyShopRemoveWrong()
    {
        if (!TrySpendGold(30, "金币不足，无法删除错题。"))
            return;

        WrongEntry wrong = WrongBook.Instance != null ? WrongBook.Instance.GetRandomForLang(selectedLang) : null;
        if (wrong != null)
            WrongBook.Instance.RemoveById(wrong.id);
        CompleteShopNode(wrong != null ? "删除一条错题记录。" : "没有错题可删，金币已支付给商人。");
    }

    private void BuyShopTime()
    {
        if (!TrySpendGold(25, "金币不足，无法购买加时。"))
            return;
        nextBattleTimeBonus += 5;
        CompleteShopNode("下一场战斗答题时间 +5 秒。");
    }

    private void BuyShopHints()
    {
        if (!TrySpendGold(20, "金币不足，无法购买提示。"))
            return;
        hintCharges += 2;
        CompleteShopNode("补充提示 +2。");
    }

    private void LeaveShop()
    {
        CompleteShopNode("离开商店。");
    }

    private void CompleteShopNode(string message)
    {
        if (choiceRoot) choiceRoot.SetActive(false);
        CompleteActiveNode();
        UpdateHud();
        SaveProgress();
        ShowMap(message);
    }

    private bool TrySpendGold(int cost, string failMessage)
    {
        if (gold >= cost)
        {
            gold -= cost;
            return true;
        }
        if (mapStatusText) mapStatusText.text = failMessage;
        return false;
    }

    private void EventStudyTrial()
    {
        playerHp = Mathf.Max(1, playerHp - 1);
        GainXp(70);
        CompleteEventNode("古书试炼完成，生命 -1，经验 +70。");
    }

    private void EventTimeTrade()
    {
        nextBattleTimePenalty += 5;
        gold += 80;
        CompleteEventNode("时间交易完成，下一战时间 -5 秒，金币 +80。");
    }

    private void EventReviewGamble()
    {
        WrongEntry wrong = WrongBook.Instance != null ? WrongBook.Instance.GetRandomForLang(selectedLang) : null;
        if (wrong == null)
        {
            gold += 25;
            CompleteEventNode("没有错题可挑战，获得金币 +25。");
            return;
        }

        if (choiceRoot) choiceRoot.SetActive(false);
        reviewBattle = true;
        eventReviewGamble = true;
        activeReviewWrongId = wrong.id;
        if (mapRoot) mapRoot.SetActive(false);
        SetPlayInteractable(true);
        SpawnEnemy();
        currentEnemy.maxHp = 1;
        currentEnemy.hp = 1;
        currentEntry = new Entry { lang = wrong.lang, prompt = wrong.prompt, answers = wrong.answers, difficulty = 1 };
        currentAnswers = wrong.answers ?? System.Array.Empty<string>();
        currentQuestionLimit = Mathf.Clamp(questionTimeLimit + RelicTimeBonus(), 10f, 60f);
        timeLeft = currentQuestionLimit;
        RuntimeUI.RegisterTextCharacters(currentEntry.prompt);
        wordText.text = currentEntry.prompt;
        masteryText.text = "事件错题赌局";
        difficultyText.text = "成功获得遗物，失败扣 1 生命";
        hintText.text = string.IsNullOrWhiteSpace(wrong.lastWrongAnswer) ? "挑战一条错题。" : "上次错写: " + wrong.lastWrongAnswer;
        answerPreviewText.text = "";
        UpdateHud();
        if (answerInput) answerInput.ActivateInputField();
    }

    private void EventLeave()
    {
        gold += 10;
        CompleteEventNode("谨慎离开，金币 +10。");
    }

    private void CompleteEventNode(string message)
    {
        if (choiceRoot) choiceRoot.SetActive(false);
        CompleteActiveNode();
        UpdateHud();
        SaveProgress();
        ShowMap(message);
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

    private void EquipRelic(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (relics.Contains(id))
            return;

        int limit = Mathf.Max(1, maxRelicSlots);
        if (relicSlots.Count >= limit)
        {
            string removed = relicSlots[0];
            relicSlots.RemoveAt(0);
            relics.Remove(removed);
        }

        relicSlots.Add(id);
        relics.Add(id);
    }

    private float RelicTimeBonus()
    {
        float bonus = 0f;
        if (HasRelic("hourglass"))
            bonus += 3f;
        if (HasRelic("time_bond"))
            bonus += 8f;
        bonus += nextBattleTimeBonus;
        return bonus;
    }

    private string RelicSummary()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < Mathf.Max(1, maxRelicSlots); i++)
        {
            if (i < relicSlots.Count)
            {
                RelicDef relic = RelicCatalog.Find(relicSlots[i]);
                names.Add((i + 1) + "." + (relic != null ? relic.name : relicSlots[i]));
            }
            else
            {
                names.Add((i + 1) + ".空");
            }
        }
        return string.Join(" / ", names.ToArray());
    }

    private void ResolveNear(AnswerResult result)
    {
        hintText.text = result.message + "，再试一次";
        answerPreviewText.text = "接近正确，不扣生命";
        SpawnFloatingText(questionPanelRect, "NEAR", RuntimeUI.Gold, 42);
        timeLeft = Mathf.Max(3f, timeLeft - 1.5f);
        if (CurrentRule() == "trickster")
        {
            timeLeft = Mathf.Max(2f, timeLeft - 3f);
            hintText.text += "，诡术惩罚时间";
        }
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
        LongTermProgress.RecordCorrect();
        if (LongTermProgress.TryClaimDailyReward(out string dailyMessage))
        {
            gold += 80;
            SpawnFloatingText(questionPanelRect, dailyMessage + " +80 金币", RuntimeUI.Gold, 34);
        }
        string answer = FormatCurrentAnswers();
        combo++;
        string speedLabel;
        string comboLabel;
        int damage = ComputeAnswerDamage(out speedLabel, out comboLabel);
        score += baseScorePerHit * Mathf.Max(1, combo) + damage * 6;
        currentEnemy.hp -= damage;
        hintText.text = speedLabel + "  " + comboLabel;
        answerPreviewText.text = "正确写法: " + answer + " / 伤害 " + damage;
        SpawnFloatingText(enemyPanelRect, "-" + damage, damage >= 5 ? RuntimeUI.Gold : RuntimeUI.Coral, damage >= 5 ? 58 : 46);
        if (damage >= 5)
            SpawnFloatingText(questionPanelRect, "CRITICAL", RuntimeUI.Gold, 46);
        SpawnParticleBurst(enemyPanelRect, damage >= 5 ? RuntimeUI.Gold : RuntimeUI.Coral, damage >= 5 ? 18 : 10);
        StartCoroutine(PulseEnemy(damage >= 5 ? RuntimeUI.Gold : new Color(1f, 0.78f, 0.70f, 1f)));
        StartCoroutine(Shake(enemyPanelRect, damage >= 5 ? 24f : 14f, damage >= 5 ? 0.22f : 0.15f));
        if (damage >= 5)
            StartCoroutine(ScreenShake(9f, 0.16f));
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

        if (HasRelic("wrong_crown") && (reviewBattle || WordProgressStore.Label(selectedLang, currentEntry.prompt) == "需复习"))
        {
            damage += 3;
            comboLabel += " + 错题王冠";
        }

        if (HasRelic("glass_sword") && !glassSwordBroken && playerHp == playerMaxHp)
        {
            damage += 2;
            comboLabel += " + 玻璃剑";
        }

        if (HasRelic("gambler_dice"))
        {
            int roll = UnityEngine.Random.Range(0, 6);
            if (roll == 0)
            {
                damage = 1;
                comboLabel += " / 骰子失手";
            }
            else if (roll >= 4)
            {
                damage += 4;
                comboLabel += " / 骰子暴走";
            }
        }

        if (CurrentRule() == "guard")
        {
            damage = Mathf.Max(1, damage - 1);
            comboLabel += " / 守卫减伤";
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
        SpawnFloatingText(enemyPanelRect, "-1", RuntimeUI.Gold, 42);
        SpawnParticleBurst(enemyPanelRect, RuntimeUI.Gold, 8);
        StartCoroutine(PulseEnemy(new Color(1f, 0.85f, 0.58f, 1f)));
        FinishHitIfEnemyDefeated();
        if (activeNode != null && mapRoot != null && !mapRoot.activeSelf)
            answerPreviewText.text = "上一题: " + answer + " / 需复习";
    }

    private void FinishHitIfEnemyDefeated()
    {
        if (currentEnemy.hp <= 0)
        {
            bool rewardRelic = activeNode != null && (activeNode.type == MapNodeType.Elite || activeNode.type == MapNodeType.Boss || eventReviewGamble);
            kills++;
            int goldReward = currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? 50 : (activeNode != null && activeNode.type == MapNodeType.Elite ? 35 : 18);
            if (activeEnemyRule == "ninja")
                goldReward += 20;
            if (HasRelic("coin_purse"))
                goldReward = Mathf.RoundToInt(goldReward * 1.5f);
            gold += goldReward;
            GainXp(currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? 45 : (activeNode != null && activeNode.type == MapNodeType.Elite ? 30 : 15));
            score += currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? 120 : (activeNode != null && activeNode.type == MapNodeType.Elite ? 70 : 35);
            hintText.text = currentEnemyTemplate != null && currentEnemyTemplate.isBoss ? "首领击败" : "战斗胜利";
            if (currentEnemyTemplate != null && currentEnemyTemplate.isBoss)
                LongTermProgress.RecordBoss(currentEnemyTemplate.name);
            if (activeNode != null && activeNode.type == MapNodeType.Boss)
                LongTermProgress.RecordRouteClear();
            if (HasRelic("time_bond"))
            {
                playerHp = Mathf.Max(1, playerHp - 1);
                hintText.text += "，时间债券扣除 1 生命";
            }
            nextBattleTimeBonus = 0;
            nextBattleTimePenalty = 0;
            if (reviewBattle && !string.IsNullOrEmpty(activeReviewWrongId) && WrongBook.Instance != null)
            {
                WrongBook.Instance.MarkReviewCorrect(activeReviewWrongId, 2);
                if (HasRelic("review_chalice"))
                    playerHp = Mathf.Min(playerMaxHp, playerHp + 1);
            }
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

        if (WrongBook.Instance != null)
            WrongBook.Instance.AddOrUpdate(selectedLang, currentEntry.prompt, currentAnswers, answerInput != null ? answerInput.text : "");

        bool canRepeat = (CurrentRule() == "slime" || HasRelic("repeater")) && !repeaterPending;
        if (canRepeat)
        {
            repeaterPending = true;
            hintText.text = CurrentRule() == "slime" ? "史莱姆分裂: 同一题再来一次" : "复读机触发: 第二次答对不扣血";
            answerPreviewText.text = "答案暂不揭晓，重答成功可避免扣血。";
            if (answerInput) answerInput.text = "";
            currentQuestionLimit = Mathf.Min(60f, currentQuestionLimit + 8f);
            timeLeft = currentQuestionLimit;
            UpdateTimerBar();
            if (answerInput) answerInput.ActivateInputField();
            return;
        }

        string penaltyLabel;
        int hpLoss = ApplyEnemyPenalty(out penaltyLabel);
        if (HasRelic("wrong_crown") && (reviewBattle || WordProgressStore.Label(selectedLang, currentEntry.prompt) == "需复习"))
            hpLoss *= 2;
        playerHp = Mathf.Max(0, playerHp - hpLoss);
        if (hpLoss > 0)
            glassSwordBroken = true;
        if (playerHp <= 0 && HasRelic("phoenix_leaf") && !phoenixLeafUsed)
        {
            phoenixLeafUsed = true;
            playerHp = Mathf.Min(playerMaxHp, 2);
            penaltyLabel += "，凤羽触发，生命回复 2";
        }

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

        if (eventReviewGamble)
        {
            eventReviewGamble = false;
            reviewBattle = false;
            CompleteActiveNode();
            UpdateHud();
            SaveProgress();
            ShowMap("错题赌局失败，生命已扣除。");
            return;
        }

        waitingNext = true;
        hintText.text = reason + " - " + penaltyLabel;
        answerPreviewText.text = "答案: " + FormatCurrentAnswers();
        SpawnFloatingText(questionPanelRect, "MISS", RuntimeUI.Coral, 46);
        SpawnParticleBurst(questionPanelRect, RuntimeUI.Coral, 12);
        StartCoroutine(Shake(questionPanelRect, 18f, 0.20f));
        StartCoroutine(ScreenShake(7f, 0.14f));
        SetPlayInteractable(false);
        bool requiresConfirm = reviewBattle || (currentEnemyTemplate != null && currentEnemyTemplate.isBoss);
        if (nextButton) nextButton.interactable = requiresConfirm;
        if (!requiresConfirm)
            StartCoroutine(AutoNextQuestionAfterDelay(wrongAnswerReviewDelay));
        UpdateHud();
        SaveProgress();
    }

    private IEnumerator AutoNextQuestionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (waitingNext && !gameOver)
            NextQuestion();
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
        questionIndexInBattle++;
        if (!eventReviewGamble && (activeNode == null || activeNode.type != MapNodeType.Review))
        {
            reviewBattle = false;
            activeReviewWrongId = "";
        }
        WrongEntry bossWrong = CurrentRule() == "boss_review" && WrongBook.Instance != null ? WrongBook.Instance.GetRandomForLang(selectedLang) : null;
        if (bossWrong != null)
        {
            currentEntry = new Entry { lang = bossWrong.lang, prompt = bossWrong.prompt, answers = bossWrong.answers, difficulty = 1 };
            activeReviewWrongId = bossWrong.id;
            reviewBattle = true;
        }
        else
        {
            currentEntry = wordDb != null ? wordDb.GetLearningEntryByLangAndDifficulty(selectedLang, minDifficulty, maxDifficulty) : null;
        }
        currentQuestionLimit = Mathf.Clamp(questionTimeLimit + RelicTimeBonus() - nextBattleTimePenalty - Mathf.Min(8f, kills * 0.35f) - nextQuestionTimePenalty, 10f, 60f);
        nextQuestionTimePenalty = 0f;
        ApplyEnemyRuleToQuestion();
        timeLeft = currentQuestionLimit;
        answerRevealed = false;
        answerPreviewText.text = "";

        if (currentEntry == null)
        {
            currentAnswers = System.Array.Empty<string>();
            wordText.text = "没有可用词条";
            return;
        }

        string displayPrompt = PromptForCurrentEnemy(currentEntry.prompt);
        RuntimeUI.RegisterTextCharacters(displayPrompt);
        wordText.text = displayPrompt;
        currentAnswers = currentEntry.answers ?? System.Array.Empty<string>();
        studyPromptText.text = selectedLang == "en" ? "根据中文回忆英文拼写" : "根据中文回忆日语读法";
        masteryText.text = WordProgressStore.Label(selectedLang, currentEntry.prompt);
        difficultyText.text = "词汇难度 " + minDifficulty + "-" + maxDifficulty + " / " + MapNodeTypeLabel(activeNode != null ? activeNode.type : MapNodeType.Monster);
        hintText.text = CurrentRule() == "mage" || CurrentRule() == "boss_review" ? "法师遮蔽: 部分释义被封印。" : "先在脑中回忆，再输入答案。";
        UpdateTimerBar();
    }

    private void ApplyEnemyRuleToQuestion()
    {
        string rule = CurrentRule();
        if (hintButton)
            hintButton.interactable = rule != "ghost" && rule != "boss_no_hint";

        if (rule == "ninja")
            currentQuestionLimit = Mathf.Max(8f, currentQuestionLimit - 12f);
        else if (rule == "boss_speed")
            currentQuestionLimit = Mathf.Max(10f, currentQuestionLimit - 18f);
        else if (rule == "boss_combo")
            currentQuestionLimit = Mathf.Max(12f, currentQuestionLimit - 8f);
        else if (rule == "pressure")
            currentQuestionLimit = Mathf.Max(12f, currentQuestionLimit - 6f);
    }

    private string CurrentRule()
    {
        if (currentEnemyTemplate == null)
            return "normal";

        if (!currentEnemyTemplate.isBoss)
            return string.IsNullOrWhiteSpace(activeEnemyRule) ? "normal" : activeEnemyRule;

        int phase = (questionIndexInBattle / 3) % 4;
        if (phase == 0)
            return "boss_speed";
        if (phase == 1)
            return "boss_no_hint";
        if (phase == 2)
            return "boss_review";
        return "boss_combo";
    }

    private string RuleLabel(string rule)
    {
        switch (rule)
        {
            case "ninja":
                return "疾速";
            case "ghost":
                return "无提示";
            case "slime":
                return "分裂";
            case "mage":
                return "遮蔽";
            case "guard":
                return "守护";
            case "pressure":
                return "压迫";
            case "trickster":
                return "诡术";
            case "boss_speed":
                return "首领: 限时";
            case "boss_no_hint":
                return "首领: 无提示";
            case "boss_review":
                return "首领: 错题复仇";
            case "boss_combo":
                return "首领: 连击挑战";
            default:
                return "标准";
        }
    }

    private string RuleDescription(string rule)
    {
        switch (rule)
        {
            case "ninja":
                return "疾速: 答题时间缩短，胜利额外金币。";
            case "ghost":
                return "无提示: 本场不能使用提示按钮。";
            case "slime":
                return "分裂: 答错后重答同题，第二次答对免扣血。";
            case "mage":
                return "遮蔽: 部分中文释义会被隐藏。";
            case "guard":
                return "守护: 每次受到的伤害 -1。";
            case "pressure":
                return "压迫: 每题时间略微缩短。";
            case "trickster":
                return "诡术: 接近正确会额外扣时间。";
            case "boss_speed":
                return "首领阶段: 限时作答。";
            case "boss_no_hint":
                return "首领阶段: 禁用提示。";
            case "boss_review":
                return "首领阶段: 混入错题并遮蔽释义。";
            case "boss_combo":
                return "首领阶段: 连击越高越占优。";
            default:
                return "标准: 正常答题战斗。";
        }
    }
    private string PromptForCurrentEnemy(string prompt)
    {
        string rule = CurrentRule();
        if (string.IsNullOrEmpty(prompt))
            return prompt;

        if (rule != "mage" && rule != "boss_review")
            return prompt;

        char[] chars = prompt.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (i % 3 == 1 && !char.IsWhiteSpace(chars[i]) && !char.IsPunctuation(chars[i]))
                chars[i] = '?';
        }
        return new string(chars);
    }

    private void ShowHint()
    {
        if (currentAnswers == null || currentAnswers.Length == 0)
            return;

        if (CurrentRule() == "ghost" || CurrentRule() == "boss_no_hint")
        {
            hintText.text = "敌人规则: 本题无法使用提示";
            return;
        }

        if (hintCharges <= 0)
        {
            hintText.text = "提示次数不足，可在商店补充。";
            return;
        }

        hintCharges--;

        string answer = GetCurrentHintAnswer();
        string first = answer.Length > 0 ? answer.Substring(0, 1) : "?";
        string studyHint = wordDb != null ? wordDb.GetStudyHint(currentEntry) : "";
        hintText.text = string.IsNullOrWhiteSpace(studyHint)
            ? "提示: 首字母 " + first + " / 长度 " + answer.Length
            : "提示: " + studyHint + " / 首字母 " + first + " / 长度 " + answer.Length;
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
            currentQuestionLimit = Mathf.Clamp(questionTimeLimit + RelicTimeBonus(), 10f, 60f);
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
        repeaterPending = false;
        questionIndexInBattle = 0;
        activeEnemyRule = string.IsNullOrWhiteSpace(template.ruleType) ? "normal" : template.ruleType;
        currentEnemy.name = template.name;
        int depthBonus = activeNode != null ? activeNode.depth : kills / 2;
        float nodeMultiplier = activeNode != null && activeNode.type == MapNodeType.Elite ? 1.35f : 1f;
        if (activeNode != null && activeNode.type == MapNodeType.Boss)
            nodeMultiplier = 1.7f;
        int bonus = template.isBoss ? kills + depthBonus : depthBonus + kills / 2;
        currentEnemy.maxHp = Mathf.Max(1, Mathf.RoundToInt((template.baseMaxHp + bonus) * nodeMultiplier));
        currentEnemy.hp = currentEnemy.maxHp;
        damageToEnemy = 1;
        enemyHpTarget = 1f;
        enemyHpVisible = 1f;
        enemyHpTrailVisible = 1f;
        SetBarWidth(enemyHpFillRect, 1f);
        SetBarWidth(enemyHpTrailFillRect, 1f);
        UpdateEnemyArt(template);
        StartEnemyIdle();
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
        if (playerHpBarText) playerHpBarText.text = "玩家生命 " + playerHp + "/" + playerMaxHp;
        if (growthText) growthText.text = "Lv" + level + "  XP " + xp + "/" + (level * 60) + "  金币 " + gold + "  提示 " + hintCharges;
        if (relicText) relicText.text = "遗物: " + RelicSummary();
        if (enemyHPText) enemyHPText.text = currentEnemy.name;
        if (enemyHpBarText) enemyHpBarText.text = currentEnemy.name + "  " + currentEnemy.hp + "/" + currentEnemy.maxHp;
        if (enemyMetaText && currentEnemyTemplate != null)
            enemyMetaText.text = RuleDescription(CurrentRule());

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
        timerFill.color = timerTarget < 0.25f ? RuntimeUI.Coral : RuntimeUI.Hex("56B8FF");
    }

    private void SmoothBars()
    {
        float speed = Time.deltaTime * 9f;
        float trailSpeed = Time.deltaTime * 1.7f;
        enemyHpVisible = Mathf.MoveTowards(enemyHpVisible, enemyHpTarget, speed);
        enemyHpTrailVisible = enemyHpTrailVisible < enemyHpTarget ? enemyHpTarget : Mathf.MoveTowards(enemyHpTrailVisible, enemyHpTarget, trailSpeed);
        playerHpVisible = Mathf.MoveTowards(playerHpVisible, playerHpTarget, speed);
        playerHpTrailVisible = playerHpTrailVisible < playerHpTarget ? playerHpTarget : Mathf.MoveTowards(playerHpTrailVisible, playerHpTarget, trailSpeed);
        SetBarWidth(enemyHpFillRect, enemyHpVisible);
        SetBarWidth(enemyHpTrailFillRect, enemyHpTrailVisible);
        SetBarWidth(playerHpFillRect, playerHpVisible);
        SetBarWidth(playerHpTrailFillRect, playerHpTrailVisible);
        if (timerFill)
            timerFill.fillAmount = Mathf.MoveTowards(timerFill.fillAmount, timerTarget, Time.deltaTime * 10f);
    }

    private void SetBarWidth(RectTransform fill, float value)
    {
        if (fill == null)
            return;

        Vector2 max = fill.anchorMax;
        max.x = Mathf.Clamp01(value);
        fill.anchorMax = max;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
    }

    private void SnapPlayerHpBar()
    {
        float value = playerMaxHp > 0 ? Mathf.Clamp01((float)playerHp / playerMaxHp) : 0f;
        playerHpTarget = value;
        playerHpVisible = value;
        playerHpTrailVisible = value;
        SetBarWidth(playerHpFillRect, value);
        SetBarWidth(playerHpTrailFillRect, value);
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

    private void StartEnemyIdle()
    {
        if (enemyIdleRoutine != null)
            StopCoroutine(enemyIdleRoutine);
        enemyIdleRoutine = StartCoroutine(EnemyIdleLoop());
    }

    private IEnumerator EnemyIdleLoop()
    {
        if (enemyImage == null)
            yield break;

        RectTransform rt = enemyImage.GetComponent<RectTransform>();
        Vector2 basePos = rt.anchoredPosition;
        Vector3 baseScale = Vector3.one;
        float seed = UnityEngine.Random.Range(0f, 6f);

        while (enemyImage != null)
        {
            float wave = Mathf.Sin(Time.time * 1.8f + seed);
            rt.anchoredPosition = basePos + new Vector2(0f, wave * 7f);
            if (!waitingNext)
                enemyImage.transform.localScale = baseScale * (1f + wave * 0.015f);
            yield return null;
        }
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

    private IEnumerator ScreenShake(float strength, float duration)
    {
        if (canvasRect == null)
            yield break;

        Vector2 original = canvasRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 1f - elapsed / duration;
            canvasRect.anchoredPosition = original + UnityEngine.Random.insideUnitCircle * strength * t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasRect.anchoredPosition = original;
    }

    private void SpawnFloatingText(RectTransform parent, string value, Color color, int size)
    {
        if (parent == null)
            return;

        TMP_Text text = RuntimeUI.Text(parent, "Float Text", value, size, color, TextAlignmentOptions.Center);
        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 90f);
        rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(20f, 90f));
        StartCoroutine(FloatAndFade(text));
    }

    private IEnumerator FloatAndFade(TMP_Text text)
    {
        RectTransform rt = text.GetComponent<RectTransform>();
        Color start = text.color;
        Vector2 origin = rt.anchoredPosition;
        float duration = 0.75f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rt.anchoredPosition = origin + new Vector2(0f, 80f * t);
            text.color = new Color(start.r, start.g, start.b, 1f - t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(text.gameObject);
    }

    private void SpawnParticleBurst(RectTransform parent, Color color, int count)
    {
        if (parent == null)
            return;

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Hit Spark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            float size = UnityEngine.Random.Range(8f, 20f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(0f, 70f));
            Vector2 velocity = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(60f, 170f);
            StartCoroutine(ParticleFade(image, rt, velocity));
        }
    }

    private IEnumerator ParticleFade(Image image, RectTransform rt, Vector2 velocity)
    {
        Color start = image.color;
        float duration = 0.45f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rt.anchoredPosition += velocity * Time.deltaTime;
            rt.localScale = Vector3.one * (1f - t * 0.65f);
            image.color = new Color(start.r, start.g, start.b, 1f - t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(image.gameObject);
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
            List<int> activeRows = PickRowsForDepth(depth, depthCount, rows, bossRow, rng);
            for (int i = 0; i < activeRows.Count; i++)
            {
                int row = activeRows[i];
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
            List<MapNode> nextNodes = NodesAtDepth(nodes, nextDepth);
            if (nextNodes.Count == 0)
                continue;
            List<string> nextIds = new List<string>();
            MapNode primary = nextIsBoss ? ClosestNode(nextNodes, bossRow) : ClosestNode(nextNodes, Mathf.Clamp(node.row + rng.Next(-1, 2), 0, rows - 1));
            nextIds.Add(primary.id);
            if (!nextIsBoss && rng.NextDouble() < 0.12 && nextNodes.Count > 1)
            {
                MapNode extra = nextNodes[rng.Next(0, nextNodes.Count)];
                if (!nextIds.Contains(extra.id))
                    nextIds.Add(extra.id);
            }
            node.nextCsv = string.Join(",", nextIds.ToArray());
        }

        return nodes.ToArray();
    }

    private List<int> PickRowsForDepth(int depth, int depthCount, int rows, int bossRow, System.Random rng)
    {
        List<int> result = new List<int>();
        if (depth == depthCount - 1)
        {
            result.Add(bossRow);
            return result;
        }

        if (depth == 0)
        {
            result.Add(Mathf.Clamp(bossRow - 1, 0, rows - 1));
            result.Add(bossRow);
            return UniqueRows(result);
        }

        int count = depth < 3 ? 2 : (rng.NextDouble() < 0.86 ? 2 : 3);
        while (result.Count < count)
        {
            int row = rng.Next(0, rows);
            if (!result.Contains(row))
                result.Add(row);
        }
        result.Sort();
        return result;
    }

    private List<int> UniqueRows(List<int> rows)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < rows.Count; i++)
        {
            if (!result.Contains(rows[i]))
                result.Add(rows[i]);
        }
        return result;
    }

    private List<MapNode> NodesAtDepth(List<MapNode> nodes, int depth)
    {
        List<MapNode> result = new List<MapNode>();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].depth == depth)
                result.Add(nodes[i]);
        }
        return result;
    }

    private MapNode ClosestNode(List<MapNode> nodes, int row)
    {
        MapNode best = nodes[0];
        int bestDistance = Mathf.Abs(best.row - row);
        for (int i = 1; i < nodes.Count; i++)
        {
            int distance = Mathf.Abs(nodes[i].row - row);
            if (distance < bestDistance)
            {
                best = nodes[i];
                bestDistance = distance;
            }
        }
        return best;
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

        TooltipTrigger.HideAll();
        mapRoot.SetActive(true);
        SetPlayInteractable(false);
        if (nextButton) nextButton.interactable = false;
        if (mapTitleText) mapTitleText.text = routeComplete ? "路线完成" : "选择路线";
        if (mapStatusText) mapStatusText.text = status + "  |  " + LongTermProgress.Summary();
        Canvas.ForceUpdateCanvases();
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
            bool hidden = hideUnvisitedMapNodes && !completed && node.type != MapNodeType.Boss;
            Button button = CreateMapStampButton(node, completed, reachable, hidden);
            RectTransform rt = button.GetComponent<RectTransform>();
            Vector2 pos = MapLocalPosition(node);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = node.type == MapNodeType.Boss ? new Vector2(120, 120) : new Vector2(82, 82);
            rt.anchoredPosition = pos;
            button.interactable = reachable && !completed && !routeComplete;
            MapNode captured = node;
            button.onClick.AddListener(() => SelectMapNode(captured));
            AddTooltip(button, MapNodeTypeLabel(node.type));
        }
    }

    private Button CreateMapStampButton(MapNode node, bool completed, bool reachable, bool hidden)
    {
        GameObject go = new GameObject("Node " + node.id, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(mapContent, false);
        Image hitArea = go.GetComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = null;

        RawImage stamp = RuntimeUI.RawImage(go.transform, "Stamp", mapStampTexture);
        stamp.raycastTarget = false;
        stamp.uvRect = StampUv(StampIndex(node, completed, hidden));
        stamp.color = completed ? new Color(0.88f, 0.72f, 0.42f, 1f) : (reachable ? new Color(1f, 0.97f, 0.86f, 1f) : new Color(0.30f, 0.27f, 0.20f, 0.84f));
        RuntimeUI.SetRect(stamp, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        if (completed || reachable || node.type == MapNodeType.Boss)
        {
            TMP_Text label = RuntimeUI.Text(go.transform, "Stamp Label", MapNodeLabel(node, completed, false), 18, RuntimeUI.Paper, TextAlignmentOptions.Center);
            label.raycastTarget = false;
            RuntimeUI.SetRect(label, new Vector2(0f, -0.24f), new Vector2(1f, 0.12f), Vector2.zero, Vector2.zero);
        }

        return button;
    }

    private int StampIndex(MapNode node, bool completed, bool hidden)
    {
        if (node == null)
            return 9;

        switch (node.type)
        {
            case MapNodeType.Elite:
                return 3;
            case MapNodeType.Rest:
                return 5;
            case MapNodeType.Shop:
                return 4;
            case MapNodeType.Event:
                return 8;
            case MapNodeType.Chest:
                return 7;
            case MapNodeType.Review:
                return 6;
            case MapNodeType.Boss:
                return 5;
            default:
                return Mathf.Abs(node.depth + node.row) % 3;
        }
    }

    private Rect StampUv(int index)
    {
        int columns = 5;
        int rows = 2;
        int col = Mathf.Clamp(index % columns, 0, columns - 1);
        int row = Mathf.Clamp(index / columns, 0, rows - 1);
        float width = 1f / columns;
        float height = 1f / rows;
        float y = row == 0 ? height : 0f;
        return new Rect(col * width, y, width, height);
    }

    private void CreateMapLine(MapNode from, MapNode to, bool active)
    {
        Vector2 a = MapLocalPosition(from);
        Vector2 b = MapLocalPosition(to);
        Vector2 mid = (a + b) * 0.5f + new Vector2(0f, StableNoise(from.depth + to.depth, from.row - to.row) * 10f);
        float distance = Vector2.Distance(a, b);
        int dots = Mathf.Clamp(Mathf.RoundToInt(distance / 92f), 3, 7);
        Color color = active ? new Color(0.13f, 0.11f, 0.09f, 0.86f) : new Color(0.15f, 0.14f, 0.12f, 0.46f);

        for (int i = 1; i < dots; i++)
        {
            float t = (float)i / dots;
            Vector2 p = Quadratic(a, mid, b, t);
            GameObject dot = new GameObject("Path Dot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(mapContent, false);
            Image image = dot.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RectTransform rt = dot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            float size = active ? 7f : 5f;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = p;
        }
    }

    private Vector2 Quadratic(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private Vector2 MapLocalPosition(MapNode node)
    {
        RectTransform rect = mapContent.GetComponent<RectTransform>();
        Vector2 size = rect.rect.size;
        if (size.x < 100f || size.y < 100f)
            size = new Vector2(1600f, 600f);

        int rows = Mathf.Max(2, mapRows);
        int depthCount = Mathf.Max(4, mapDepthCount);
        float x = depthCount <= 1 ? 0.5f : Mathf.Lerp(0.18f, 0.88f, (float)node.depth / (depthCount - 1));
        float y = rows <= 1 ? 0.5f : Mathf.Lerp(0.26f, 0.74f, (float)node.row / (rows - 1));
        if (node.depth > 0 && node.depth < depthCount - 1)
        {
            x += StableNoise(node.depth, node.row) * 0.006f;
            y += StableNoise(node.row, node.depth) * 0.022f;
        }
        x = Mathf.Clamp(x, 0.15f, 0.91f);
        y = Mathf.Clamp(y, 0.20f, 0.80f);
        return new Vector2((x - 0.5f) * size.x, (y - 0.5f) * size.y);
    }

    private float StableNoise(int a, int b)
    {
        float value = Mathf.Sin((currentSeed * 0.0137f + a * 12.9898f + b * 78.233f) * 43758.5453f);
        return Mathf.Repeat(value, 1f) * 2f - 1f;
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

    private string MapNodeLabel(MapNode node, bool completed, bool hidden)
    {
        if (node == null)
            return "?";
        if (completed)
            return MapNodeTypeLabel(node.type) + "\n已完成";
        if (node.type == MapNodeType.Boss)
            return "深处";
        return hidden ? "未知" : MapNodeTypeLabel(node.type);
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
        if (wordDb != null && currentEntry != null)
            return wordDb.GetDisplayAnswer(currentEntry);
        return currentAnswers != null && currentAnswers.Length > 0 ? string.Join(" / ", currentAnswers) : "没有答案";
    }

    private string GetCurrentHintAnswer()
    {
        if (wordDb != null && currentEntry != null)
            return wordDb.GetHintAnswer(currentEntry);
        return currentAnswers != null && currentAnswers.Length > 0 ? currentAnswers[0] : "";
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

    private void StartBackgroundMusic()
    {
        if (string.IsNullOrWhiteSpace(backgroundMusicResource))
            return;

        GameObject existing = GameObject.Find("WordQuest BGM");
        if (existing != null)
        {
            musicSource = existing.GetComponent<AudioSource>();
            if (musicSource != null && !musicSource.isPlaying)
                musicSource.Play();
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(backgroundMusicResource);
        if (clip == null)
        {
            Debug.LogWarning("Background music not found: " + backgroundMusicResource);
            return;
        }

        GameObject go = new GameObject("WordQuest BGM");
        DontDestroyOnLoad(go);
        musicSource = go.AddComponent<AudioSource>();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = Mathf.Clamp01(backgroundMusicVolume);
        musicSource.Play();
    }

    private void EnsureEnemyTemplates()
    {
        enemyTemplates = new[]
        {
            new EnemyTemplate { name = "Ink Slime", baseMaxHp = 3, isBoss = false, artResource = "Art/enemy_ink_slime", faction = "Study Ooze", ruleType = "slime", minKills = 0, tier = 1 },
            new EnemyTemplate { name = "Goblin Scribe", baseMaxHp = 3, isBoss = false, artResource = "Art/Enemies/enemy_goblin", faction = "Western Fantasy", ruleType = "normal", minKills = 0, tier = 1 },
            new EnemyTemplate { name = "Dire Wolf", baseMaxHp = 4, isBoss = false, artResource = "Art/Enemies/enemy_dire_wolf", faction = "Western Fantasy", ruleType = "ninja", minKills = 1, tier = 1 },
            new EnemyTemplate { name = "Kappa Trickster", baseMaxHp = 4, isBoss = false, artResource = "Art/Enemies/enemy_kappa", faction = "Japanese Myth", ruleType = "trickster", minKills = 1, tier = 1 },
            new EnemyTemplate { name = "Paper Tengu", baseMaxHp = 5, isBoss = false, artResource = "Art/Enemies/enemy_tengu", faction = "Japanese Myth", ruleType = "ninja", minKills = 2, tier = 2 },
            new EnemyTemplate { name = "Wyvern Whelp", baseMaxHp = 5, isBoss = false, artResource = "Art/Enemies/enemy_wyvern", faction = "Western Fantasy", ruleType = "pressure", minKills = 2, tier = 2 },
            new EnemyTemplate { name = "Kitsune Adept", baseMaxHp = 6, isBoss = false, artResource = "Art/Enemies/enemy_kitsune", faction = "Japanese Myth", ruleType = "mage", minKills = 3, tier = 2 },
            new EnemyTemplate { name = "Griffin Guard", baseMaxHp = 7, isBoss = false, artResource = "Art/Enemies/enemy_griffin", faction = "Western Fantasy", ruleType = "guard", minKills = 4, tier = 3 },
            new EnemyTemplate { name = "Yuki-onna", baseMaxHp = 7, isBoss = false, artResource = "Art/Enemies/enemy_yuki_onna", faction = "Japanese Myth", ruleType = "ghost", minKills = 4, tier = 3 },
            new EnemyTemplate { name = "Lich Librarian", baseMaxHp = 10, isBoss = true, artResource = "Art/Enemies/enemy_lich", faction = "Western Boss", ruleType = "boss", minKills = 5, tier = 3 },
            new EnemyTemplate { name = "Oni Warlord", baseMaxHp = 11, isBoss = true, artResource = "Art/Enemies/enemy_oni", faction = "Japanese Boss", ruleType = "boss", minKills = 6, tier = 3 },
            new EnemyTemplate { name = "Nue Chimera", baseMaxHp = 12, isBoss = true, artResource = "Art/Enemies/enemy_nue", faction = "Japanese Boss", ruleType = "boss", minKills = 7, tier = 3 },
            new EnemyTemplate { name = "Archdemon", baseMaxHp = 13, isBoss = true, artResource = "Art/Enemies/enemy_archdemon", faction = "Western Boss", ruleType = "boss", minKills = 8, tier = 3 }
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
            hintCharges = hintCharges,
            nextBattleTimeBonus = nextBattleTimeBonus,
            nextBattleTimePenalty = nextBattleTimePenalty,
            playerMaxHp = playerMaxHp,
            playerHp = playerHp,
            completedCsv = string.Join(",", new List<string>(completedNodes).ToArray()),
            relicCsv = string.Join(",", relicSlots.ToArray())
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
        hintCharges = progress.hintCharges > 0 ? progress.hintCharges : 2;
        nextBattleTimeBonus = progress.nextBattleTimeBonus;
        nextBattleTimePenalty = progress.nextBattleTimePenalty;
        playerMaxHp = progress.playerMaxHp > 0 ? progress.playerMaxHp : startPlayerMaxHp;
        playerHp = Mathf.Clamp(progress.playerHp, 1, playerMaxHp);
        SnapPlayerHpBar();
        completedNodes = ParseCompleted(progress.completedCsv);
        relicSlots = ParseCsvList(progress.relicCsv);
        relics = new HashSet<string>(relicSlots);
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

    private List<string> ParseCsvList(string csv)
    {
        List<string> list = new List<string>();
        if (string.IsNullOrWhiteSpace(csv))
            return list;

        string[] parts = csv.Split(',');
        int limit = Mathf.Max(1, maxRelicSlots);
        for (int i = 0; i < parts.Length && list.Count < limit; i++)
        {
            string value = parts[i].Trim();
            if (!string.IsNullOrWhiteSpace(value) && !list.Contains(value))
                list.Add(value);
        }
        return list;
    }

    private void ClearProgress()
    {
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();
    }
}
