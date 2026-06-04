using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WrongBookDetailUI : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text answersText;
    [SerializeField] private TMP_Text metaText;
    [SerializeField] private Button masteredButton;
    [SerializeField] private Button backButton;
    [SerializeField] private bool useRuntimeUi = true;

    private string id;

    private void Start()
    {
        EnsureWrongBook();
        id = PlayerPrefs.GetString("wrong_selected_id", "");

        if (useRuntimeUi)
            BuildRuntimeUi();

        if (backButton) backButton.onClick.AddListener(() => SceneManager.LoadScene("WrongBook"));
        if (masteredButton) masteredButton.onClick.AddListener(MarkMastered);
        Refresh();
    }

    private void BuildRuntimeUi()
    {
        Canvas canvas = RuntimeUI.CreateCanvas("Runtime WrongBook Detail Canvas");
        RuntimeUI.Background(canvas.transform, RuntimeUI.LoadTexture("Art/library_battle_bg"));
        RuntimeUI.Panel(canvas.transform, "Shade", new Color(0.02f, 0.04f, 0.08f, 0.62f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).raycastTarget = false;

        Image card = RuntimeUI.Panel(canvas.transform, "Detail Card", new Color(1f, 0.96f, 0.84f, 0.94f), new Vector2(0.20f, 0.14f), new Vector2(0.80f, 0.86f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(card.gameObject, 42, 22);

        promptText = RuntimeUI.Text(card.transform, "Prompt", "", 58, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(promptText.gameObject, 120, 150);
        answersText = RuntimeUI.Text(card.transform, "Answers", "", 42, RuntimeUI.Teal, TextAlignmentOptions.Center);
        RuntimeUI.Layout(answersText.gameObject, 180, 220);
        metaText = RuntimeUI.Text(card.transform, "Meta", "", 28, RuntimeUI.Hex("5B6472"), TextAlignmentOptions.Center);
        RuntimeUI.Layout(metaText.gameObject, 90, 110);

        GameObject buttons = new GameObject("Buttons", typeof(RectTransform));
        buttons.transform.SetParent(card.transform, false);
        RuntimeUI.AddHorizontalLayout(buttons, 0, 16);
        RuntimeUI.Layout(buttons, 76, 92);
        backButton = RuntimeUI.Button(buttons.transform, "Back", "返回列表", RuntimeUI.Hex("44516A"), Color.white);
        masteredButton = RuntimeUI.Button(buttons.transform, "Mastered", "已掌握", RuntimeUI.Teal, Color.white);
    }

    private void Refresh()
    {
        WrongEntry entry = WrongBook.Instance != null ? WrongBook.Instance.GetById(id) : null;
        if (entry == null)
        {
            promptText.text = "未找到错题";
            answersText.text = "";
            if (metaText) metaText.text = "可能已经被删除。";
            return;
        }

        promptText.text = $"[{entry.lang.ToUpper()}] {entry.prompt}";
        answersText.text = entry.answers != null ? string.Join("\n", entry.answers) : "";
        if (metaText) metaText.text = "错误次数: " + Mathf.Max(1, entry.timesWrong);
    }

    private void MarkMastered()
    {
        if (WrongBook.Instance != null && !string.IsNullOrEmpty(id))
            WrongBook.Instance.RemoveById(id);

        SceneManager.LoadScene("WrongBook");
    }

    private void EnsureWrongBook()
    {
        if (WrongBook.Instance == null && FindObjectOfType<WrongBook>() == null)
            new GameObject("WrongBook").AddComponent<WrongBook>();
    }
}
