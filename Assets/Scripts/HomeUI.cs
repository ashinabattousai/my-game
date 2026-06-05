using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeUI : MonoBehaviour
{
    private const string ProgressKey = "word_quest_run_progress_v1";
    private const string StartNewRunKey = "word_quest_start_new_run";

    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button guideButton;
    [SerializeField] private Button wrongBookButton;
    [SerializeField] private bool useRuntimeUi = true;

    private GameObject guidePanel;

    private void Start()
    {
        EnsureSystems();

        if (useRuntimeUi)
            BuildRuntimeUi();

        if (startButton) startButton.onClick.AddListener(StartNewAdventure);
        if (continueButton) continueButton.onClick.AddListener(ContinueAdventure);
        if (guideButton) guideButton.onClick.AddListener(() => SetGuideVisible(true));
        if (wrongBookButton) wrongBookButton.onClick.AddListener(() => SceneManager.LoadScene("WrongBook"));
    }

    private void BuildRuntimeUi()
    {
        Canvas canvas = RuntimeUI.CreateCanvas("Runtime Home Canvas");
        Texture2D bg = RuntimeUI.LoadTexture("Art/library_battle_bg");
        RuntimeUI.Background(canvas.transform, bg);

        Image shade = RuntimeUI.Panel(canvas.transform, "Shade", new Color(0.04f, 0.07f, 0.12f, 0.48f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        shade.raycastTarget = false;

        Image panel = RuntimeUI.Panel(canvas.transform, "Home Panel", new Color(1f, 0.96f, 0.84f, 0.90f), new Vector2(0.08f, 0.12f), new Vector2(0.44f, 0.88f), Vector2.zero, Vector2.zero);
        GameObject panelGo = panel.gameObject;
        RuntimeUI.AddVerticalLayout(panelGo, 42, 22);

        TMP_Text title = RuntimeUI.Text(panelGo.transform, "Title", "Word Quest", 72, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(title.gameObject, 110, 120);
        TMP_Text subtitle = RuntimeUI.Text(panelGo.transform, "Subtitle", "单词战斗训练", 34, RuntimeUI.Hex("5B6472"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(subtitle.gameObject, 56, 64);

        startButton = RuntimeUI.Button(panelGo.transform, "Start Button", "开始冒险", RuntimeUI.Teal, Color.white);
        RuntimeUI.Layout(startButton.gameObject, 78, 88);
        continueButton = RuntimeUI.Button(panelGo.transform, "Continue Button", "继续进度", RuntimeUI.Coral, Color.white);
        RuntimeUI.Layout(continueButton.gameObject, 78, 88);
        continueButton.gameObject.SetActive(HasProgress());
        guideButton = RuntimeUI.Button(panelGo.transform, "Guide Button", "游玩说明", RuntimeUI.Hex("44516A"), Color.white);
        RuntimeUI.Layout(guideButton.gameObject, 78, 88);
        wrongBookButton = RuntimeUI.Button(panelGo.transform, "Wrong Book Button", "错题本", RuntimeUI.Gold, RuntimeUI.Ink);
        RuntimeUI.Layout(wrongBookButton.gameObject, 78, 88);

        TMP_Text footer = RuntimeUI.Text(panelGo.transform, "Footer", "选择路线，击败怪物，收集错题继续变强。", 26, RuntimeUI.Hex("5B6472"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(footer.gameObject, 70, 82);

        BuildGuide(canvas.transform);
    }

    private void BuildGuide(Transform parent)
    {
        Image shade = RuntimeUI.Panel(parent, "Guide Shade", new Color(0f, 0f, 0f, 0.58f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        guidePanel = shade.gameObject;

        Image panel = RuntimeUI.Panel(shade.transform, "Guide Panel", new Color(1f, 0.96f, 0.84f, 0.97f), new Vector2(0.44f, 0.08f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(panel.gameObject, 28, 14);
        TMP_Text title = RuntimeUI.Text(panel.transform, "Guide Title", "游玩说明", 52, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(title.gameObject, 70, 84);
        TMP_Text body = RuntimeUI.Text(panel.transform, "Guide Body",
            "冒险目标\n选择路线进入关卡，用单词作答来攻击怪物。击败 Boss 后本轮路线通关，可以继续生成新路线挑战更高难度。\n\n答题战斗\n根据中文提示输入日语读法或英文拼写。答得越快伤害越高，连续答对会累积连击，并可能触发暴击、回血或额外奖励。答错会显示正确写法，并把词加入错题本；普通错误会短暂停顿后继续，死亡或特殊挑战才需要手动确认。\n\n地图节点\n普通关适合积累金币和经验；精英关更危险，但胜利后更容易获得遗物；商店可以花金币回血、买遗物、删错题或给下一战加时；事件会给你风险选择；宝箱提供奖励；复习节点会考错题，答对后能清理错题并获得收益；深处是 Boss。\n\n怪物规则\n不同怪物会改变打法：史莱姆答错后会要求重答同题，幽灵会禁用提示，忍者会压缩时间但奖励更多，法师会遮蔽部分提示，Boss 会切换规则。进入战斗前先看怪物说明，再决定是否用提示或保守作答。\n\n成长与错题\n金币用于商店消费，经验会提升等级，遗物会改变本轮构筑。最多可装备 3 个遗物，满了以后新遗物会替换最早的一个。错题本不是惩罚，而是复习资源；复习节点和部分遗物会把错题变成战斗收益。\n\n保存\n路线进度会自动保存。返回首页后可以继续进度，也可以生成新路线重新开始。",
            24, RuntimeUI.Hex("44516A"), TextAlignmentOptions.TopLeft);
        RuntimeUI.Layout(body.gameObject, 560, 650);
        Button close = RuntimeUI.Button(panel.transform, "Close Guide", "关闭", RuntimeUI.Teal, Color.white);
        RuntimeUI.Layout(close.gameObject, 66, 80);
        close.onClick.AddListener(() => SetGuideVisible(false));
        SetGuideVisible(false);
    }

    private void SetGuideVisible(bool visible)
    {
        if (guidePanel)
            guidePanel.SetActive(visible);
    }

    private bool HasProgress()
    {
        string json = PlayerPrefs.GetString(ProgressKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        RunProgress progress = JsonUtility.FromJson<RunProgress>(json);
        return progress != null && progress.active;
    }

    private void StartNewAdventure()
    {
        PlayerPrefs.SetInt(StartNewRunKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("ModeSelect");
    }

    private void ContinueAdventure()
    {
        PlayerPrefs.SetInt(StartNewRunKey, 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("main");
    }

    private void EnsureSystems()
    {
        if (WrongBook.Instance == null && FindObjectOfType<WrongBook>() == null)
        {
            GameObject go = new GameObject("WrongBook");
            go.AddComponent<WrongBook>();
        }
    }
}
