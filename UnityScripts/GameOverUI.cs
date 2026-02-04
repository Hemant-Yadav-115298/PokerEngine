using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Game over screen UI controller.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        
        Hide();
    }

    public void Show(string winner, decimal finalStack)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        
        if (winnerText != null)
            winnerText.text = $"{winner} Wins!";
        
        if (resultText != null)
            resultText.text = $"Final Stack: ${finalStack}";
    }

    public void Hide()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnPlayAgainClicked()
    {
        SceneController.Instance.LoadGame();
    }

    private void OnMainMenuClicked()
    {
        SceneController.Instance.LoadMainMenu();
    }
}
