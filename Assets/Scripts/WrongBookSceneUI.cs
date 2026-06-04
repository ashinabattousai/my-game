using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WrongBookSceneUI : MonoBehaviour
{
    [SerializeField] private Button backButton;

    private void Start()
    {
        if (FindObjectOfType<WrongBookListUI>() == null)
            gameObject.AddComponent<WrongBookListUI>();

        if (backButton)
            backButton.onClick.AddListener(() => SceneManager.LoadScene("Home"));
    }
}
