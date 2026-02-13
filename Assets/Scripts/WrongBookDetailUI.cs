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

    void Start()
    {
        id = PlayerPrefs.GetString("wrong_selected_id", "");

        if (backButton) backButton.onClick.AddListener(() => SceneManager.LoadScene("WrongBook"));
        if (masteredButton) masteredButton.onClick.AddListener(MarkMastered);

        Refresh();
    }

    private void Refresh()
    {
        if (WrongBook.Instance == null)
        {
            promptText.text = "WrongBook.Instance is NULL";
            answersText.text = "";
            return;
        }

        var list = WrongBook.Instance.GetAll();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].id == id)
            {
                promptText.text = $"[{list[i].lang}] {list[i].prompt}";
                answersText.text = (list[i].answers != null) ? string.Join("\n", list[i].answers) : "";
                return;
            }
        }

        promptText.text = "Not found: " + id;
        answersText.text = "";
    }

    private void MarkMastered()
    {
        if (WrongBook.Instance != null && !string.IsNullOrEmpty(id))
            WrongBook.Instance.RemoveById(id);

        SceneManager.LoadScene("WrongBook");
    }
}
