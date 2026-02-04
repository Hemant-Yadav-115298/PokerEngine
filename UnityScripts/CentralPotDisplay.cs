using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays the main pot in the center of the poker table.
/// </summary>
public class CentralPotDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI potAmountText;
    [SerializeField] private GameObject potContainer;
    [SerializeField] private Image potBackground;

    private void Start()
    {
        // Style the pot display
        if (potBackground != null)
        {
            potBackground.color = new Color(0.8f, 0.6f, 0.2f, 0.9f); // Gold background
        }
    }

    public void UpdatePot(decimal totalPot)
    {
        if (totalPot > 0)
        {
            if (potAmountText != null)
                potAmountText.text = $"POT\n${totalPot}";
            
            if (potContainer != null)
                potContainer.SetActive(true);
        }
        else
        {
            Hide();
        }
    }

    public void Hide()
    {
        if (potContainer != null)
            potContainer.SetActive(false);
    }

    public void Show()
    {
        if (potContainer != null)
            potContainer.SetActive(true);
    }
}