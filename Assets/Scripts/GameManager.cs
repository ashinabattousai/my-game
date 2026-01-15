using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 游戏主控：负责把“词库/输入/UI/战斗状态”串起来
public class GameManager : MonoBehaviour
{
    // Unity 的 Inspector 里显示的分组标题（只是视觉分组，不影响代码）
    [Header("Scene Refs")]
    [Header("config")]

    // === 场景引用（需要你在 Inspector 里拖进去）===

    [SerializeField] private WordDatabase wordDb;          // 词库来源（负责从 JSON 读取并提供随机单词）
    [SerializeField] private Button submitButton;          // 提交按钮
    [SerializeField] private Button restartButton;         // 重开按钮
    [SerializeField] private Button nextButton;            // Next 按钮引用
    [SerializeField] private TMP_InputField answerInput;   // 输入框（玩家打字的地方）
    [SerializeField] private EnemyTemplate[] enemyTemplates; // 怪物“模板表”（Slime/Goblin/Boss 等配置）
    [SerializeField] private TMP_Text wordText;            // 显示当前要输入的单词
    [SerializeField] private TMP_Text hintText;            // 显示 Correct/Wrong/Victory/Game Over 等提示
    [SerializeField] private TMP_Text scoreText;           // 显示分数
    [SerializeField] private TMP_Text comboText;           // 显示连击
    [SerializeField] private TMP_Text enemyHPText;         // 显示当前敌人血量
    [SerializeField] private TMP_Text killsText;           // 显示击杀数
    [SerializeField] private TMP_Text playerHpText;        // 显示玩家血量
    [SerializeField] private TMP_Text modeText;            // 显示当前模式 JP/EN

    // === 可调参数（可以在 Inspector 里改数值）===

    [SerializeField] private int startPlayerMaxHp = 5;     // 玩家初始最大血量
    [SerializeField] private int startEnemyMaxHp = 3;      // 当没有 enemyTemplates 时，默认敌人的血量
    [SerializeField] private int baseScorePerHit = 10;     // 基础得分（会乘以 combo）
    [SerializeField] private int bossEveryKills = 5;       //每打 5 只怪必出 Boss（规则刷怪）

    // === 运行时逻辑/状态（游戏进行过程中会变化）===

    private BattleLogic logic = new BattleLogic();         // 判定逻辑（输入是否等于目标单词）
    private Entry currentEntry = null;
    private string[] currentAnswers = System.Array.Empty<string>(); //空回答

    private string selectedLang = "jp"; // 默认日语


    private int score = 0;                                 // 当前分数
    private int combo = 0;                                 // 当前连击数（答对+1，答错归零）

    private EnemyState currentEnemy = new EnemyState();     // 当前这一只敌人的“状态”（名字/最大血量/当前血量）
    private int damageToEnemy = 1;                          // 答对一次对敌人造成的伤害

    private int kills = 0;                                  // 击杀数（敌人血量打到 0 的次数）

    private int playerMaxHp = 5;                            // 玩家最大血量（本局会从 startPlayerMaxHp 初始化）
    private int playerHp = 5;                               // 玩家当前血量（答错会扣）
    private bool gameOver = false;                          // 是否已经 Game Over（为 true 时不再允许提交）

    private bool waitingNext = false;                       // 当为 true 时，表示“当前处于答错后等待 Next”的状态

    // Unity：场景开始运行时调用一次
    void Start()
    {
        // 给按钮绑定点击事件：点按钮会调用 Submit()/RestartGame()
        submitButton.onClick.AddListener(Submit);
        restartButton.onClick.AddListener(RestartGame);

        nextButton.onClick.AddListener(NextQuestion);

        // 初始化一局（开局/重开都走同一个函数，避免重复代码）
        ResetRun();
    }

    // Unity：每帧调用
    void Update()
    {
        // 支持按回车提交（键盘输入体验更好）
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Submit();
    }

    // 提交答案：判定对错 → 改状态（分数/连击/血量）→ 刷新 HUD → 出下一题
    private void Submit()
    {
        // 如果已经 Game Over，直接不做任何事
        if (gameOver) return;

        // 如果正在等待 Next，就不允许继续提交（必须先点 Next）
        if (waitingNext) return;

        // 读取输入框内容
        string typed = answerInput.text;

        // 判定是否命中（输入 == 当前单词）
        bool hit = CheckHitAny(typed, currentAnswers);

        // 先给一个提示（后面也可以把提示逻辑写得更丰富）
        hintText.text = hit ? "Correct" : "Wrong";

        if (hit == true)
        {
            // 命中：连击+1
            combo++;

            // 得分：基础分 * 连击
            score += baseScorePerHit * combo;

            // 敌人扣血
            currentEnemy.hp -= damageToEnemy;

            // 敌人死亡：击杀数+1，刷下一只敌人
            if (currentEnemy.hp <= 0)
            {
                kills += 1;
                hintText.text = "Victory";
                SpawnEnemy(); // 生成下一只（随机模板 + 难度增益）
            }
        }
        else
        {
            // 没命中：连击清零
            combo = 0;

            // 玩家扣血
            playerHp -= 1;

            // 玩家死亡：Game Over，锁定输入与提交（通过 gameOver + 直接 return）
            if (playerHp <= 0)
            {
                playerHp = 0;          // 强制归零，避免显示 1/5 这种边界问题
                gameOver = true;
                hintText.text = "Game Over";
                UpdateHud();           // 立刻刷新 HUD
                return;                // 结束 Submit，不再出新词
            }
        }

        // 答错：显示正确答案，但不出下一题，等待玩家按 Next
        string show = (currentAnswers != null && currentAnswers.Length > 0) ? string.Join(" / ", currentAnswers)
    : "(no answer)";
        hintText.text = "Wrong. Answer: " + show;

        // 进入等待 Next 状态
        waitingNext = true;

        // 暂时禁止提交与输入（可选，但体验更明确）
        // 如果你希望玩家还能在输入框里编辑但不能提交，也可以只禁用 submitButton
        submitButton.interactable = false;
        answerInput.interactable = false;

        // 允许 Next 按钮点击
        nextButton.interactable = true;

        UpdateHud();
        return; // 关键：直接 return，保证不执行后面的 NextWord()

    }

    // 出新词：从词库随机取一个并显示到 wordText
    private void NextWord()
    {
        currentEntry = wordDb.GetRandomEntryByLang(selectedLang);

        if (currentEntry == null)
        {
            currentAnswers = System.Array.Empty<string>();
            wordText.text = "(No entries for lang: " + selectedLang + ")";
            return;
        }

        wordText.text = currentEntry.prompt;
        currentAnswers = (currentEntry.answers != null) ? currentEntry.answers : System.Array.Empty<string>();
    }

    // 刷新 HUD：把“内存里的状态变量”同步到 UI 文本上
    private void UpdateHud()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (comboText != null) comboText.text = "Combo: " + combo;

        if (enemyHPText != null)
            enemyHPText.text = $"{currentEnemy.name} HP: {currentEnemy.hp}/{currentEnemy.maxHp}";

        if (playerHpText != null)
            playerHpText.text = "Player HP: " + playerHp + "/" + playerMaxHp;

        if (killsText != null)
            killsText.text = "kills : " + kills;
    }

    // 点击 Restart 按钮时调用：重置本局状态
    private void RestartGame()
    {
        ResetRun();
    }

    // 重置一局：把所有“会变化的状态”恢复到初始值
    // 这就是一局开始时的“初始状态”
    private void ResetRun()
    {
        selectedLang = PlayerPrefs.GetString("selected_lang", "jp");
        UpdateModeLabel();

        Debug.Log("ResetRun selectedLang = " + selectedLang);

        Debug.Log("Before set modeText: " + (modeText ? modeText.text : "modeText is NULL"));

   /*     if (modeText != null)
        {
            modeText.text = "MODE >>> " + selectedLang.ToUpper() + " <<<";
        }
   */
        Debug.Log("After set modeText: " + (modeText ? modeText.text : "modeText is NULL"));


     //   if (modeText != null)
     //     modeText.text = (selectedLang == "en") ? "Mode: EN" : "Mode: JP";

        gameOver = false;

        // 清空进度
        score = 0;
        combo = 0;
        kills = 0;

        // 初始化玩家血量
        playerMaxHp = startPlayerMaxHp;
        playerHp = playerMaxHp;

        // 生成第一只敌人
        SpawnEnemy();

        // UI 允许操作（如果上一局 GameOver 禁用了，这里会恢复）
        submitButton.interactable = true;
        answerInput.interactable = true;

        // UI 提示文字
        hintText.text = "Enter Or Submit";

        // 刷新界面 & 出第一题
        UpdateHud();
        NextWord();

        // 清空并聚焦输入框
        answerInput.text = "";
        answerInput.ActivateInputField();

        waitingNext = false;
        nextButton.interactable = false;
        submitButton.interactable = true;
        answerInput.interactable = true;

    }

    // 生成敌人：从模板数组随机抽一个，并根据 kills 给难度加成
    private void SpawnEnemy()
    {
        // 如果没有配置模板（Inspector 没填），就用默认 Slime
        if (enemyTemplates == null || enemyTemplates.Length == 0)
        {
            currentEnemy.name = "Slime";
            currentEnemy.maxHp = startEnemyMaxHp;
            currentEnemy.hp = currentEnemy.maxHp;
            return;
        }

        EnemyTemplate t = null;

        // 规则：每 bossEveryKills 次击杀必出 Boss
        bool shouldSpawnBoss = (bossEveryKills > 0) && (kills > 0) && (kills % bossEveryKills == 0);

        if (shouldSpawnBoss)
        {
            // 从模板里找一个 isBoss == true 的
            for (int i = 0; i < enemyTemplates.Length; i++)
            {
                if (enemyTemplates[i] != null && enemyTemplates[i].isBoss)
                {
                    t = enemyTemplates[i];
                    break;
                }
            }
        }

        // 如果不该出 Boss 或没找到 Boss 模板，就随机刷一个非 Boss
        if (t == null)
        {
            // 尝试随机找非Boss，最多试 20 次避免死循环
            for (int tries = 0; tries < 20; tries++)
            {
                int idx = UnityEngine.Random.Range(0, enemyTemplates.Length);
                if (enemyTemplates[idx] != null && !enemyTemplates[idx].isBoss)
                {
                    t = enemyTemplates[idx];
                    break;
                }
            }

            // 如果全是Boss（极端情况），那就随便选一个
            if (t == null)
                t = enemyTemplates[UnityEngine.Random.Range(0, enemyTemplates.Length)];
        }


        // 把模板信息“拷贝”到当前敌人状态里（模板不变，状态会变）
        currentEnemy.name = t.name;

        // 难度加成：Boss 增益更大，普通怪增益更小
        // kills 越多，敌人越强（血越厚）
        int bonus = t.isBoss ? kills : (kills / 2);
        currentEnemy.maxHp = t.baseMaxHp + bonus;

        // 当前血量重置为满血
        currentEnemy.hp = currentEnemy.maxHp;
    }//生成怪

    private bool CheckHitAny(string input, string[] answers)//检查回答
    {
        if (string.IsNullOrWhiteSpace(input) || answers == null || answers.Length == 0)
            return false;

        string typed = input.Trim();
        for(int i = 0; i < answers.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(answers[i]))
                continue;
            if (string.Equals(typed, answers[i].Trim(), System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void NextQuestion()
    {
        if (!waitingNext)
            return;

        waitingNext = false;

        submitButton.interactable = true;
        answerInput.interactable = true;

        nextButton.interactable = false;

        // 清提示（你也可以保留上一条提示）
        hintText.text = "Enter Or Submit";

        // 清空输入框，出下一题
        answerInput.text = "";
        NextWord(); // 你目前函数名叫 NextWord，但它实际上是 NextQuestion 出题
        UpdateHud();

        // 重新聚焦输入框
        answerInput.ActivateInputField();
    }

    private void UpdateModeLabel()
    {
        if (modeText == null) return;
        modeText.text = (selectedLang == "en") ? "Mode: EN" : "Mode: JP";
    }

}
