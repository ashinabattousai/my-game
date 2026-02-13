using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LanguageSelectUI : MonoBehaviour
{
    [SerializeField] private Button btnJP;   // 学日语按钮
    [SerializeField] private Button btnEN;   // 学英语按钮

    // 战斗场景名字
    [SerializeField] private string battleSceneName = "main";

    // PlayerPrefs 存储用的 key（固定字符串）
    private const string KEY_LANG = "selected_lang";

    void Start()
    {
        // 绑定按钮点击事件：点按钮就保存选择并切场景
        btnJP.onClick.AddListener(() => SelectAndStart("jp"));
        btnEN.onClick.AddListener(() => SelectAndStart("en"));
    }

    /*   private void SelectAndStart(string lang)
       {
           // 保存选择：下一个场景可以读取
           PlayerPrefs.SetString(KEY_LANG, lang);
           PlayerPrefs.Save();

           // 切换到战斗场景
           SceneManager.LoadScene(battleSceneName);
       }
    */

    private void SelectAndStart(string lang)
    {
        Debug.Log("ModeSelect clicked: " + lang);

        PlayerPrefs.SetString("selected_lang", lang);
        PlayerPrefs.Save();

        Debug.Log("ModeSelect saved: " + PlayerPrefs.GetString("selected_lang", "NONE"));

        SceneManager.LoadScene(battleSceneName);
    }

}
