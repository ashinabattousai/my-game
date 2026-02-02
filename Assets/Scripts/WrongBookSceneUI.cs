using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WrongBookSceneUI : MonoBehaviour
{
    [SerializeField] private Button backButton;

    void Start()
    {
        backButton.onClick.AddListener(Back);
    }

    private void Back()
    {
        SceneManager.LoadScene("Home");
    }
}
