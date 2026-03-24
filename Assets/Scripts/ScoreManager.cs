using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    float score = 0f;
    [SerializeField] float scorePerSecond = 10f;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        score += scorePerSecond * Time.deltaTime;
    }

    public void AddBonusScore(int pts)
    {
        score += pts;
    }

    public float GetScore() { return score; }
}
