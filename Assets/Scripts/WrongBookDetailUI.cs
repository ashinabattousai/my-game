using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WrongBookDetailUI : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text answersText;
    [SerializeField] private Button masteredButton;
    [SerializeField] private Button backButton;

    private string id;

    private void Start()
    {
        id = PlayerPrefs.GetString("wrong_selected_id", string.Empty);

        if (backButton != null)
            backButton.onClick.AddListener(() => SceneManager.LoadScene("WrongBook"));
        if (masteredButton != null)
            masteredButton.onClick.AddListener(MarkMastered);

        Refresh();
    }

    private void Refresh()
    {
        if (WrongBook.Instance == null)
        {
            SetText(promptText, "Wrong book data is not available.");
            SetText(answersText, string.Empty);
            return;
        }

        var list = WrongBook.Instance.GetAll();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].id != id)
                continue;

            SetText(promptText, $"[{list[i].lang}] {list[i].prompt}");
            SetText(answersText, list[i].answers != null ? string.Join("\n", list[i].answers) : string.Empty);
            return;
        }

        SetText(promptText, "This wrong-book item was not found.");
        SetText(answersText, string.Empty);
    }

    private void MarkMastered()
    {
        if (WrongBook.Instance != null && !string.IsNullOrEmpty(id))
            WrongBook.Instance.RemoveById(id);

        SceneManager.LoadScene("WrongBook");
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
