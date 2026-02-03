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
    [SerializeField] private GameObject[] cardSlots;

    public void UpdatePlayer(Player player, bool isActive)
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
            highlightImage.enabled = isActive;

        // Update cards (show back of cards for now)
        UpdateCardDisplay(player);
    }

    private void UpdateCardDisplay(Player player)
    {
        if (cardSlots == null) return;

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (i < player.HoleCards.Count)
                cardSlots[i].SetActive(true);
            else
                cardSlots[i].SetActive(false);
        }
    }
}
