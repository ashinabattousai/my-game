using UnityEngine;

public class WrongBookListUI : MonoBehaviour
{
    [SerializeField] private Transform content;        // ScrollView/Viewport/Content
    [SerializeField] private WrongItemView itemPrefab; // WrongItem.prefab 上的组件

    void Start()
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

        // 清空旧列表
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        // 读取错题本
        var list = WrongBook.Instance != null ? WrongBook.Instance.GetAll() : null;
        if (list == null)
        {
            Debug.LogWarning("WrongBook.Instance is null in WrongBook scene.");
            return;
        }

        // 生成每一条
        for (int i = 0; i < list.Count; i++)
        {
            var view = Instantiate(itemPrefab, content);
            view.Bind(list[i], Refresh);
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

}
