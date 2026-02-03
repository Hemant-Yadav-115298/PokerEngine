using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerEngine.Core;

/// <summary>
/// UI component for displaying a single player's information.
/// </summary>
public class PlayerUIPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image highlightImage;
    [SerializeField] private CardVisual[] cardVisuals = new CardVisual[2];
    [SerializeField] private BetDisplay betDisplay;
    [SerializeField] private DealerButtonDisplay dealerButton;

    public void UpdatePlayer(Player player, bool isActive, bool showCards = false, decimal currentBet = 0, bool isDealer = false)
    {
        if (player == null) return;

        // Update name
        if (playerNameText != null)
            playerNameText.text = player.Name;

        // Update stack
        if (stackText != null)
            stackText.text = $"${player.Stack}";

        // Update status
        if (statusText != null)
        {
            if (player.IsFolded)
                statusText.text = "Folded";
            else if (player.IsAllIn)
                statusText.text = "All-In";
            else
                statusText.text = "";
        }

        // Highlight active player
        if (highlightImage != null)
        {
            highlightImage.enabled = isActive;
            if (isActive)
                highlightImage.color = new Color(1f, 0.8f, 0f, 0.3f); // Yellow highlight
        }

        // Update bet display
        if (betDisplay != null)
        {
            betDisplay.SetBet(currentBet);
        }

        // Update dealer button
        if (dealerButton != null)
        {
            dealerButton.SetActive(isDealer);
        }

        // Update cards
        UpdateCardDisplay(player, showCards);
    }

    private void UpdateCardDisplay(Player player, bool showCards)
    {
        if (cardVisuals == null || cardVisuals.Length < 2) return;

        for (int i = 0; i < cardVisuals.Length; i++)
        {
            if (cardVisuals[i] == null) continue;

            if (i < player.HoleCards.Count && !player.IsFolded)
            {
                cardVisuals[i].SetCard(player.HoleCards[i], showCards);
            }
            else
            {
                cardVisuals[i].Clear();
            }
        }
    }

    public void ClearCards()
    {
        if (cardVisuals == null) return;
        
        foreach (var cardVisual in cardVisuals)
        {
            if (cardVisual != null)
                cardVisual.Clear();
        }
    }
}
