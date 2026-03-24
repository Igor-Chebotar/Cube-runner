using UnityEngine;

public static class SaveManager
{
    const string KEY = "HighScore";

    public static int LoadHighScore()
    {
        return PlayerPrefs.GetInt(KEY, 0);
    }

    public static void TrySaveHighScore(int score)
    {
        int prev = LoadHighScore();
        if (score > prev)
        {
            PlayerPrefs.SetInt(KEY, score);
            PlayerPrefs.Save();
        }
    }
}
