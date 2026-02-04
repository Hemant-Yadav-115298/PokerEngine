using UnityEngine;
using TMPro;

/// <summary>
/// Displays a player's current bet amount for the round.
/// </summary>
public class BetDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private GameObject betContainer;

    public void SetBet(decimal amount)
    {
        if (amount > 0)
        {
            if (betText != null)
                betText.text = $"${amount}";
            
            if (betContainer != null)
                betContainer.SetActive(true);
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        if (betContainer != null)
            betContainer.SetActive(false);
    }

    // Clear bet when new round starts
    public void ClearForNewRound()
    {
        Clear();
    }
}
