using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public Slider hpBar;
    public Text scoreText;
    public Text highScoreText;

    PlayerController player;
    ScoreManager scoreMgr;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        scoreMgr = FindObjectOfType<ScoreManager>();

        int hs = SaveManager.LoadHighScore();
        if (highScoreText != null)
            highScoreText.text = "Best: " + hs;
    }

    void Update()
    {
        if (player != null && hpBar != null)
        {
            hpBar.maxValue = player.GetMaxHealth();
            hpBar.value = player.GetHealth();
        }

        if (scoreMgr != null && scoreText != null)
            scoreText.text = "Score: " + (int)scoreMgr.GetScore();
    }
}
