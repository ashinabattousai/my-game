using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WrongItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text answersText;
    [SerializeField] private Button removeButton;

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
    }

    private void OnRemoveClicked()
    {
        if (WrongBook.Instance != null)
            WrongBook.Instance.RemoveById(id);

        onRemoved?.Invoke();  // ✅通知列表：我删了一条
        Destroy(gameObject);
    }
}
