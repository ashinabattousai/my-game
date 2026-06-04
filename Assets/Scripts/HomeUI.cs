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

        Image panel = RuntimeUI.Panel(shade.transform, "Guide Panel", new Color(1f, 0.96f, 0.84f, 0.96f), new Vector2(0.52f, 0.16f), new Vector2(0.90f, 0.86f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(panel.gameObject, 34, 18);
        TMP_Text title = RuntimeUI.Text(panel.transform, "Guide Title", "游玩说明", 52, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(title.gameObject, 78, 92);
        TMP_Text body = RuntimeUI.Text(panel.transform, "Guide Body",
            "1. 选择日语或英语后进入路线地图。\n2. 每个圆点是一场关卡，路线会随机生成。\n3. 普通怪、精英和首领会逐步提高词汇难度。\n4. 输入正确答案会攻击敌人，并显示正确写法。\n5. 答错或超时会扣生命，并自动记录到错题本。\n6. 进度会自动保存，回到首页可继续。",
            30, RuntimeUI.Hex("44516A"), TextAlignmentOptions.TopLeft);
        RuntimeUI.Layout(body.gameObject, 360, 430);
        Button close = RuntimeUI.Button(panel.transform, "Close Guide", "关闭", RuntimeUI.Teal, Color.white);
        RuntimeUI.Layout(close.gameObject, 72, 84);
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
