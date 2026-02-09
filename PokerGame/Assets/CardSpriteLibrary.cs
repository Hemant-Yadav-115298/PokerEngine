using System.Collections.Generic;
using UnityEngine;
using PokerEngine.Core;

public static class CardSpriteLibrary
{
    private static readonly Dictionary<string, Sprite> SpriteByName = new Dictionary<string, Sprite>();
    private static bool _initialized;
    private static Sprite _cardBackSprite;

    public static bool TryGetSprite(Card card, out Sprite sprite)
    {
        EnsureInitialized();
        
        // Try multiple key formats
        var keys = new[]
        {
            BuildSpriteKey(card.Rank, card.Suit),
            BuildAlternateKey(card.Rank, card.Suit),
            BuildSimpleKey(card.Rank, card.Suit)
        };

        foreach (var key in keys)
        {
            if (SpriteByName.TryGetValue(key, out sprite) && sprite != null)
            {
                return true;
            }
        }

        sprite = null;
        return false;
    }

    public static Sprite GetCardBack()
    {
        EnsureInitialized();
        return _cardBackSprite;
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        Debug.Log("[CardSpriteLibrary] Initializing card sprites...");

        // Try loading from Resources first
        LoadFromResources("Sprites/Cards");
        LoadFromResources("Cards");
        LoadFromResources("");

#if UNITY_EDITOR
        // In editor, also load from asset database for faster iteration
        if (SpriteByName.Count < 52)
        {
            LoadFromAssetDatabase("Assets/Sprites/Cards");
        }
#endif

        Debug.Log($"[CardSpriteLibrary] Loaded {SpriteByName.Count} card sprites.");
        
        // Log available keys for debugging
        if (SpriteByName.Count > 0 && SpriteByName.Count < 10)
        {
            foreach (var kvp in SpriteByName)
            {
                Debug.Log($"  Available: {kvp.Key}");
            }
        }
    }

    private static void LoadFromResources(string path)
    {
        Sprite[] sprites;
        if (string.IsNullOrEmpty(path))
        {
            sprites = Resources.LoadAll<Sprite>("");
        }
        else
        {
            sprites = Resources.LoadAll<Sprite>(path);
        }
        
        if (sprites == null || sprites.Length == 0) return;

        Debug.Log($"[CardSpriteLibrary] Found {sprites.Length} sprites in {path}");

        foreach (var sprite in sprites)
        {
            if (sprite == null) continue;
            
            // Get the original sprite name
            var originalName = sprite.name;
            var key = NormalizeName(originalName);
            
            // Also try removing trailing _0, _1 etc. for multi-sprite sheets
            var cleanKey = System.Text.RegularExpressions.Regex.Replace(key, @"_\d+$", "");
            
            Debug.Log($"[CardSpriteLibrary] Sprite: {originalName} -> key: {key}, cleanKey: {cleanKey}");
            
            // Check for card back
            if (key.Contains("back") || key.Contains("cardback"))
            {
                _cardBackSprite = sprite;
            }
            
            // Store with original key
            if (!SpriteByName.ContainsKey(key))
            {
                SpriteByName[key] = sprite;
            }
            
            // Also store with cleaned key (without _0 suffix)
            if (cleanKey != key && !SpriteByName.ContainsKey(cleanKey))
            {
                SpriteByName[cleanKey] = sprite;
            }
        }
    }

#if UNITY_EDITOR
    private static void LoadFromAssetDatabase(string folderPath)
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            
            // Load all sprites from this texture (handles multi-sprite textures)
            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in allAssets)
            {
                var sprite = asset as Sprite;
                if (sprite == null) continue;
                
                var originalName = sprite.name;
                var key = NormalizeName(originalName);
                var cleanKey = System.Text.RegularExpressions.Regex.Replace(key, @"_\d+$", "");
                
                if (key.Contains("back") || key.Contains("cardback"))
                {
                    _cardBackSprite = sprite;
                }
                
                if (!SpriteByName.ContainsKey(key))
                {
                    SpriteByName[key] = sprite;
                }
                
                if (cleanKey != key && !SpriteByName.ContainsKey(cleanKey))
                {
                    SpriteByName[cleanKey] = sprite;
                }
            }
        }
    }
#endif

    private static string BuildSpriteKey(Rank rank, Suit suit)
    {
        return NormalizeName($"{RankToName(rank)}_of_{SuitToName(suit)}");
    }

    private static string BuildAlternateKey(Rank rank, Suit suit)
    {
        // e.g., "ace_spades" without "of"
        return NormalizeName($"{RankToName(rank)}_{SuitToName(suit)}");
    }

    private static string BuildSimpleKey(Rank rank, Suit suit)
    {
        // e.g., "as" for Ace of Spades
        var rankChar = rank switch
        {
            Rank.Ace => "a",
            Rank.King => "k",
            Rank.Queen => "q",
            Rank.Jack => "j",
            Rank.Ten => "t",
            _ => ((int)rank).ToString()
        };
        var suitChar = suit switch
        {
            Suit.Clubs => "c",
            Suit.Diamonds => "d",
            Suit.Hearts => "h",
            Suit.Spades => "s",
            _ => "x"
        };
        return NormalizeName($"{rankChar}{suitChar}");
    }

    private static string RankToName(Rank rank)
    {
        return rank switch
        {
            Rank.Ace => "ace",
            Rank.King => "king",
            Rank.Queen => "queen",
            Rank.Jack => "jack",
            Rank.Ten => "10",
            Rank.Nine => "9",
            Rank.Eight => "8",
            Rank.Seven => "7",
            Rank.Six => "6",
            Rank.Five => "5",
            Rank.Four => "4",
            Rank.Three => "3",
            Rank.Two => "2",
            _ => ((int)rank).ToString()
        };
    }

    private static string SuitToName(Suit suit)
    {
        return suit switch
        {
            Suit.Clubs => "clubs",
            Suit.Diamonds => "diamonds",
            Suit.Hearts => "hearts",
            Suit.Spades => "spades",
            _ => "unknown"
        };
    }

    private static string NormalizeName(string name)
    {
        return name.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
    }

    // Force reload (useful for editor testing)
    public static void Reload()
    {
        _initialized = false;
        SpriteByName.Clear();
        _cardBackSprite = null;
        EnsureInitialized();
    }
}
