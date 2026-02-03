using System.Collections.Generic;
using UnityEngine;
using PokerEngine.Core;

/// <summary>
/// Loads and caches card sprites from Resources.
/// </summary>
public static class CardSpriteLoader
{
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static bool isInitialized = false;

    public static Sprite GetCardSprite(Card card)
    {
        if (!isInitialized)
            Initialize();

        string cardName = GetCardFileName(card);
        
        if (spriteCache.TryGetValue(cardName, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"Card sprite not found: {cardName}");
        return null;
    }

    private static void Initialize()
    {
        // Load all card sprites from Resources/Cards folder
        Sprite[] sprites = Resources.LoadAll<Sprite>("Cards");
        
        foreach (var sprite in sprites)
        {
            spriteCache[sprite.name] = sprite;
        }

        Debug.Log($"Loaded {spriteCache.Count} card sprites");
        isInitialized = true;
    }

    private static string GetCardFileName(Card card)
    {
        // Convert to file naming convention: c01, d05, h13, s10, etc.
        char suitChar = card.Suit switch
        {
            Suit.Clubs => 'c',
            Suit.Diamonds => 'd',
            Suit.Hearts => 'h',
            Suit.Spades => 's',
            _ => 'c'
        };

        // Rank: Ace=1, 2-10=2-10, Jack=11, Queen=12, King=13
        int rankNum = card.Rank switch
        {
            Rank.Ace => 1,
            Rank.Two => 2,
            Rank.Three => 3,
            Rank.Four => 4,
            Rank.Five => 5,
            Rank.Six => 6,
            Rank.Seven => 7,
            Rank.Eight => 8,
            Rank.Nine => 9,
            Rank.Ten => 10,
            Rank.Jack => 11,
            Rank.Queen => 12,
            Rank.King => 13,
            _ => 1
        };

        return $"{suitChar}{rankNum:D2}";
    }

    public static void ClearCache()
    {
        spriteCache.Clear();
        isInitialized = false;
    }
}
