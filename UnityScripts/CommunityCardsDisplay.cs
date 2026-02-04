using System.Collections.Generic;
using UnityEngine;
using PokerEngine.Core;
using PokerEngine.State;

/// <summary>
/// Displays the 5 community cards (flop, turn, river) progressively based on game phase.
/// Shows card backs initially, then flips to reveal cards as phases progress.
/// </summary>
public class CommunityCardsDisplay : MonoBehaviour
{
    [SerializeField] private CardVisual[] cardVisuals = new CardVisual[5];

    public void UpdateCards(IReadOnlyList<Card> communityCards, GamePhase currentPhase)
    {
        // Always show all 5 card slots
        for (int i = 0; i < cardVisuals.Length; i++)
        {
            if (cardVisuals[i] == null) continue;

            if (i < communityCards.Count)
            {
                // Determine if this card should be face up based on phase
                bool shouldShowFaceUp = ShouldCardBeVisible(i, currentPhase);
                
                // Set the card (face up or face down)
                cardVisuals[i].SetCard(communityCards[i], shouldShowFaceUp);
            }
            else
            {
                // Show card back for empty slots
                cardVisuals[i].SetFaceUp(false);
                cardVisuals[i].gameObject.SetActive(true);
            }
        }
    }

    private bool ShouldCardBeVisible(int cardIndex, GamePhase phase)
    {
        return phase switch
        {
            GamePhase.NotStarted => false,      // All cards face down
            GamePhase.PreFlop => false,         // All cards face down
            GamePhase.Flop => cardIndex < 3,    // First 3 cards face up
            GamePhase.Turn => cardIndex < 4,    // First 4 cards face up
            GamePhase.River => true,            // All 5 cards face up
            GamePhase.Showdown => true,         // All 5 cards face up
            GamePhase.Complete => true,         // All 5 cards face up
            _ => false
        };
    }

    public void ShowAllCardBacks()
    {
        // Show all 5 card backs (for start of hand)
        for (int i = 0; i < cardVisuals.Length; i++)
        {
            if (cardVisuals[i] != null)
            {
                cardVisuals[i].SetFaceUp(false);
                cardVisuals[i].gameObject.SetActive(true);
            }
        }
    }

    public void Clear()
    {
        foreach (var cardVisual in cardVisuals)
        {
            if (cardVisual != null)
                cardVisual.Clear();
        }
    }

    // Legacy method for backward compatibility
    public void UpdateCards(IReadOnlyList<Card> communityCards)
    {
        // Default to showing all cards face up (fallback)
        UpdateCards(communityCards, GamePhase.River);
    }
}
