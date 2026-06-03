using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WrongItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text answersText;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button detailButton;

    private string id;
    private Action onRemoved;

    public void Bind(WrongEntry entry, Action onRemovedCallback = null)
    {
        if (entry == null)
            return;

        id = entry.id;
        onRemoved = onRemovedCallback;

        if (promptText != null)
            promptText.text = $"[{entry.lang}] {entry.prompt}";

        if (answersText != null)
            answersText.text = "Answers: " + (entry.answers != null ? string.Join(" / ", entry.answers) : "");

        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(OnRemoveClicked);
        }

        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(OpenDetail);
        }
    }

    private void OnRemoveClicked()
    {
        if (WrongBook.Instance != null)
            WrongBook.Instance.RemoveById(id);

        onRemoved?.Invoke();
        Destroy(gameObject);
    }

    private void OpenDetail()
    {
        PlayerPrefs.SetString("wrong_selected_id", id);
        PlayerPrefs.Save();
        SceneManager.LoadScene("WrongBookDetail");
    }
}
