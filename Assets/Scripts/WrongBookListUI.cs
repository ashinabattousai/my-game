using TMPro;
using UnityEngine;

public class WrongBookListUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private WrongItemView itemPrefab;
    [SerializeField] private TMP_Text emptyText;

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (content == null || itemPrefab == null)
        {
            Debug.LogError("WrongBookListUI missing refs (content or itemPrefab).");
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var list = WrongBook.Instance != null ? WrongBook.Instance.GetAll() : null;
        bool isEmpty = list == null || list.Count == 0;
        if (emptyText != null)
            emptyText.text = isEmpty ? "No wrong words yet. Keep practicing!" : string.Empty;

        if (isEmpty)
            return;

        for (int i = 0; i < list.Count; i++)
        {
            WrongItemView view = Instantiate(itemPrefab, content);
            view.Bind(list[i], Refresh);
        }
    }
}
