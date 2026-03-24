using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Text highScoreText;
    [SerializeField] Button playBtn;
    [SerializeField] Button quitBtn;

    void Start()
    {
        int hs = SaveManager.LoadHighScore();
        if (highScoreText != null)
            highScoreText.text = "Рекорд: " + hs;

        playBtn.onClick.AddListener(OnPlay);
        quitBtn.onClick.AddListener(OnQuit);
    }

    void OnPlay()
    {
        SceneManager.LoadScene("Game");
    }

    void OnQuit()
    {
        Application.Quit();
    }
}
