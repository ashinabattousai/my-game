using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LanguageSelectUI : MonoBehaviour
{
    [SerializeField] private Button btnJP;
    [SerializeField] private Button btnEN;
    [SerializeField] private string battleSceneName = "main";

    private const string KeyLang = "selected_lang";

    private void Start()
    {
        BindLanguageButton(btnJP, "jp");
        BindLanguageButton(btnEN, "en");
    }

    private void BindLanguageButton(Button button, string lang)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectAndStart(lang));
    }

    private void SelectAndStart(string lang)
    {
        PlayerPrefs.SetString(KeyLang, lang);
        PlayerPrefs.Save();
        SceneManager.LoadScene(battleSceneName);
    }
}
