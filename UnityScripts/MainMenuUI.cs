using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main menu UI controller.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI titleText;

    private void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnPlayClicked()
    {
        SceneController.Instance.LoadGame();
    }

    private void OnQuitClicked()
    {
        SceneController.Instance.QuitGame();
    }
}
