using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Button continueBtn;
    [SerializeField] Button menuBtn;

    void Start()
    {
        panel.SetActive(false);
        continueBtn.onClick.AddListener(OnContinue);
        menuBtn.onClick.AddListener(OnMenu);
    }

    public void Show()
    {
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    void OnContinue()
    {
        GameManager.Instance.ResumeGame();
    }

    void OnMenu()
    {
        GameManager.Instance.GoToMenu();
    }
}
