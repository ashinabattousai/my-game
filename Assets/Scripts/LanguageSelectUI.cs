using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LanguageSelectUI : MonoBehaviour
{
    [SerializeField] private Button btnJP;
    [SerializeField] private Button btnEN;
    [SerializeField] private string battleSceneName = "main";
    [SerializeField] private bool useRuntimeUi = true;

    private const string KeyLang = "selected_lang";
    private const string StartNewRunKey = "word_quest_start_new_run";

    private void Start()
    {
        if (useRuntimeUi)
            BuildRuntimeUi();

        if (btnJP) btnJP.onClick.AddListener(() => SelectAndStart("jp"));
        if (btnEN) btnEN.onClick.AddListener(() => SelectAndStart("en"));
    }

    private void BuildRuntimeUi()
    {
        Canvas canvas = RuntimeUI.CreateCanvas("Runtime Mode Canvas");
        RuntimeUI.Background(canvas.transform, RuntimeUI.LoadTexture("Art/library_battle_bg"));
        RuntimeUI.Panel(canvas.transform, "Shade", new Color(0.03f, 0.06f, 0.10f, 0.58f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).raycastTarget = false;

        Image root = RuntimeUI.Panel(canvas.transform, "Mode Root", new Color(0f, 0f, 0f, 0f), new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(root.gameObject, 20, 26);

        TMP_Text title = RuntimeUI.Text(root.transform, "Title", "选择训练模式", 64, Color.white, TextAlignmentOptions.Center);
        RuntimeUI.Layout(title.gameObject, 96, 110);

        GameObject row = new GameObject("Mode Cards", typeof(RectTransform));
        row.transform.SetParent(root.transform, false);
        RuntimeUI.AddHorizontalLayout(row, 0, 28);
        RuntimeUI.Layout(row, 360, 420);

        btnJP = CreateModeCard(row.transform, "JP", "日语训练", "假名 / 罗马音 / 汉字都可命中", RuntimeUI.Coral);
        btnEN = CreateModeCard(row.transform, "EN", "英语训练", "用英文拼写击破敌人", RuntimeUI.Teal);

        TMP_Text tip = RuntimeUI.Text(root.transform, "Tip", "每次答错会自动进入错题本，之后可以单独复习。", 28, RuntimeUI.Paper, TextAlignmentOptions.Center);
        RuntimeUI.Layout(tip.gameObject, 64, 80);
    }

    private Button CreateModeCard(Transform parent, string code, string title, string detail, Color accent)
    {
        Image card = RuntimeUI.Panel(parent, code + " Card", new Color(1f, 0.96f, 0.84f, 0.93f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(card.gameObject, 30, 18);
        RuntimeUI.Layout(card.gameObject, 320, 380, 1);

        TMP_Text codeText = RuntimeUI.Text(card.transform, "Code", code, 76, accent, TextAlignmentOptions.Center);
        RuntimeUI.Layout(codeText.gameObject, 110, 120);
        TMP_Text titleText = RuntimeUI.Text(card.transform, "Title", title, 42, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(titleText.gameObject, 70, 82);
        TMP_Text detailText = RuntimeUI.Text(card.transform, "Detail", detail, 26, RuntimeUI.Hex("5B6472"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(detailText.gameObject, 82, 100);

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card;
        return button;
    }

    private void SelectAndStart(string lang)
    {
        PlayerPrefs.SetString(KeyLang, lang);
        PlayerPrefs.SetInt(StartNewRunKey, 1);
        PlayerPrefs.Save();

        string targetScene = string.IsNullOrWhiteSpace(battleSceneName) || battleSceneName == "Main" ? "main" : battleSceneName;
        SceneManager.LoadScene(targetScene);
    }
}
