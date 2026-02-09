using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerEngine.Core;

public class CardDisplay : MonoBehaviour
{
    [Header("Card Sprites - Assign manually or load from Resources")]
    public Sprite cardBackSprite;

    [Header("Text Display")]
    public TextMeshProUGUI label; // Link the Text (TMP) child in Inspector

    private Image cardImage;
    private TextMeshProUGUI rankText;
    private TextMeshProUGUI suitText;

    void Awake()
    {
        cardImage = GetComponent<Image>();

        // Find or create rank and suit text children
        var rankObj = transform.Find("RankText");
        if (rankObj != null) rankText = rankObj.GetComponent<TextMeshProUGUI>();

        var suitObj = transform.Find("SuitText");
        if (suitObj != null) suitText = suitObj.GetComponent<TextMeshProUGUI>();
    }

    public void SetCard(Card card)
    {
        // Use the label field if assigned
        if (label != null)
        {
            label.text = $"{GetRankDisplay(card.Rank)}{GetSuitSymbol(card.Suit)}";
            label.color = GetCardColor(card.Suit);
        }

        cardImage.color = Color.white; // White background for card
    }

    public void ShowCardBack()
    {
        if (cardBackSprite != null)
        {
            cardImage.sprite = cardBackSprite;
        }
        else
        {
            cardImage.color = new Color(0.8f, 0.2f, 0.2f); // Red back
        }

        if (label != null)
            label.text = "";
    }

    string GetRankDisplay(Rank rank)
    {
        return rank switch
        {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            Rank.Ten => "10",
            _ => ((int)rank).ToString()
        };
    }

    string GetSuitSymbol(Suit suit)
    {
        return suit switch
        {
            Suit.Clubs => "♣",
            Suit.Diamonds => "♦",
            Suit.Hearts => "♥",
            Suit.Spades => "♠",
            _ => "?"
        };
    }

    Color GetCardColor(Suit suit)
    {
        return (suit == Suit.Hearts || suit == Suit.Diamonds)
            ? Color.red
            : Color.black;
    }
}
