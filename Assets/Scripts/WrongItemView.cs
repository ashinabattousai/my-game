using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WrongItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text answersText;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button detailButton;


    private string id;
    private Action onRemoved; // ✅删除后通知列表刷新（可选增强）

    // e: 这条错题数据
    // onRemovedCallback: 删除后要做什么（比如列表刷新）
    public void Bind(WrongEntry e, Action onRemovedCallback = null)
    {
        id = e.id;
        onRemoved = onRemovedCallback;

        if (promptText)
            promptText.text = $"[{e.lang}] {e.prompt}";

        if (answersText)
            answersText.text = "Answers: " + (e.answers != null ? string.Join(" / ", e.answers) : "");

        if (removeButton)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(OnRemoveClicked);
        }

        if (detailButton)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(OpenDetail);
        }

        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(OnDetailClicked);
        }

    }

    private void OnRemoveClicked()
    {
        if (WrongBook.Instance != null)
            WrongBook.Instance.RemoveById(id);

        onRemoved?.Invoke();   // ⭐删除后通知列表刷新
        Destroy(gameObject);
    }

    private void OpenDetail()
{
    PlayerPrefs.SetString("wrong_selected_id", id);
    PlayerPrefs.Save();
    UnityEngine.SceneManagement.SceneManager.LoadScene("WrongBookDetail");
}

    private void OnDetailClicked()
    {
        // 把当前条目的 id 存起来，详情页用它来查数据
        PlayerPrefs.SetString("wrong_selected_id", id);
        PlayerPrefs.Save();

        // 跳转到详情场景（确保场景已加入 Build Settings）
        SceneManager.LoadScene("WrongBookDetail");
    }

}
