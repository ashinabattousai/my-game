using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WrongBookListUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private WrongItemView itemPrefab;
    [SerializeField] private bool useRuntimeUi = true;

    private TMP_Text emptyText;

    private void Start()
    {
        EnsureWrongBook();
        if (useRuntimeUi)
            BuildRuntimeUi();
        Refresh();
    }

    private void OnEnable()
    {
        if (content != null)
            Refresh();
    }

    public void Refresh()
    {
        if (content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var list = WrongBook.Instance != null ? WrongBook.Instance.GetAll() : null;
        if (list == null || list.Count == 0)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(true);
            return;
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(false);

        for (int i = 0; i < list.Count; i++)
            CreateRuntimeRow(list[i]);
    }

    private void BuildRuntimeUi()
    {
        Canvas canvas = RuntimeUI.CreateCanvas("Runtime WrongBook Canvas");
        RuntimeUI.Background(canvas.transform, RuntimeUI.LoadTexture("Art/library_battle_bg"));
        RuntimeUI.Panel(canvas.transform, "Shade", new Color(0.02f, 0.04f, 0.08f, 0.58f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).raycastTarget = false;

        Image root = RuntimeUI.Panel(canvas.transform, "WrongBook Root", new Color(1f, 0.96f, 0.84f, 0.92f), new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.90f), Vector2.zero, Vector2.zero);
        RuntimeUI.AddVerticalLayout(root.gameObject, 30, 18);

        TMP_Text title = RuntimeUI.Text(root.transform, "Title", "错题本", 58, RuntimeUI.Ink, TextAlignmentOptions.Center);
        RuntimeUI.Layout(title.gameObject, 80, 96);

        GameObject toolbar = new GameObject("Toolbar", typeof(RectTransform));
        toolbar.transform.SetParent(root.transform, false);
        RuntimeUI.AddHorizontalLayout(toolbar, 0, 14);
        RuntimeUI.Layout(toolbar, 68, 80);

        Button back = RuntimeUI.Button(toolbar.transform, "Back", "返回主页", RuntimeUI.Hex("44516A"), Color.white);
        back.onClick.AddListener(() => SceneManager.LoadScene("Home"));
        Button clear = RuntimeUI.Button(toolbar.transform, "Clear", "清空错题", RuntimeUI.Coral, Color.white);
        clear.onClick.AddListener(() => { if (WrongBook.Instance != null) WrongBook.Instance.ClearAll(); Refresh(); });

        ScrollRect scroll = RuntimeUI.Scroll(root.transform, "Wrong Entries");
        RuntimeUI.Layout(scroll.gameObject, 560, 620);
        content = scroll.content;

        emptyText = RuntimeUI.Text(scroll.viewport, "Empty", "还没有错题。去战斗里答错几题，这里就会自动记录。", 34, RuntimeUI.Ink, TextAlignmentOptions.Center);
    }

    private void CreateRuntimeRow(WrongEntry entry)
    {
        Image row = RuntimeUI.Panel(content, "Wrong Row", new Color(0.06f, 0.10f, 0.16f, 0.88f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RuntimeUI.AddHorizontalLayout(row.gameObject, 18, 16);
        RuntimeUI.Layout(row.gameObject, 112, 128);

        string answers = entry.answers != null ? string.Join(" / ", entry.answers) : "";
        TMP_Text text = RuntimeUI.Text(row.transform, "Summary", $"[{entry.lang.ToUpper()}] {entry.prompt}\n答案: {answers}\n错误次数: {Mathf.Max(1, entry.timesWrong)}", 26, RuntimeUI.Paper, TextAlignmentOptions.MidlineLeft);
        RuntimeUI.Layout(text.gameObject, 98, 112, 1);

        Button detail = RuntimeUI.Button(row.transform, "Detail", "详情", RuntimeUI.Gold, RuntimeUI.Ink);
        RuntimeUI.Layout(detail.gameObject, 84, 96);
        detail.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("wrong_selected_id", entry.id);
            PlayerPrefs.Save();
            SceneManager.LoadScene("WrongBookDetail");
        });

        Button remove = RuntimeUI.Button(row.transform, "Remove", "掌握", RuntimeUI.Teal, Color.white);
        RuntimeUI.Layout(remove.gameObject, 84, 96);
        remove.onClick.AddListener(() =>
        {
            if (WrongBook.Instance != null)
                WrongBook.Instance.RemoveById(entry.id);
            Refresh();
        });
    }

    private void EnsureWrongBook()
    {
        if (WrongBook.Instance == null && FindObjectOfType<WrongBook>() == null)
            new GameObject("WrongBook").AddComponent<WrongBook>();
    }
}
