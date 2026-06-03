using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button wrongBookButton;

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(() => SceneManager.LoadScene("ModeSelect"));
        if (wrongBookButton != null)
            wrongBookButton.onClick.AddListener(() => SceneManager.LoadScene("WrongBook"));
    }
}
