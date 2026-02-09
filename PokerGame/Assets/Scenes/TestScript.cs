using UnityEngine;
using PokerEngine.Core;
public class TestScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Try creating a card
        var card = new Card(Rank.Ace, Suit.Spades);
        Debug.Log($"Created card: {card.Rank} of {card.Suit}");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
