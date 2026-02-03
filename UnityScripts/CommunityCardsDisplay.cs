using System.Collections.Generic;
using UnityEngine;
using PokerEngine.Core;

/// <summary>
/// Displays the 5 community cards (flop, turn, river).
/// </summary>
public class CommunityCardsDisplay : MonoBehaviour
{
    [SerializeField] private CardVisual[] cardVisuals = new CardVisual[5];

    public void UpdateCards(IReadOnlyList<Card> communityCards)
    {
        // Show cards that exist
        for (int i = 0; i < cardVisuals.Length; i++)
        {
            if (i < communityCards.Count)
            {
                cardVisuals[i].SetCard(communityCards[i], true);
            }
            else
            {
                cardVisuals[i].Clear();
            }
        }
    }

    public void Clear()
    {
        foreach (var cardVisual in cardVisuals)
        {
            cardVisual.Clear();
        }
    }
}
