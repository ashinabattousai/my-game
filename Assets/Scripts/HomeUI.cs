using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button wrongBookButton;

    void Start()
    {
        if (startButton) startButton.onClick.AddListener(() => SceneManager.LoadScene("ModeSelect"));
        if (wrongBookButton) wrongBookButton.onClick.AddListener(() => SceneManager.LoadScene("WrongBook"));
    }
}
