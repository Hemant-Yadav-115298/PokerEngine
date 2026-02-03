using UnityEngine;
using UnityEngine.UI;
using PokerEngine.Core;

/// <summary>
/// Displays a single playing card with the correct sprite.
/// </summary>
public class CardVisual : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Sprite cardBackSprite;
    
    private Card? currentCard;
    private bool isFaceUp = false;

    private void Awake()
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();
    }

    public void SetCard(Card card, bool faceUp = true)
    {
        currentCard = card;
        isFaceUp = faceUp;
        UpdateVisual();
    }

    public void SetFaceUp(bool faceUp)
    {
        isFaceUp = faceUp;
        UpdateVisual();
    }

    public void Clear()
    {
        currentCard = null;
        gameObject.SetActive(false);
    }

    private void UpdateVisual()
    {
        if (!currentCard.HasValue)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (isFaceUp)
        {
            // Load card sprite
            var sprite = CardSpriteLoader.GetCardSprite(currentCard.Value);
            if (sprite != null && cardImage != null)
            {
                cardImage.sprite = sprite;
            }
        }
        else
        {
            // Show card back
            if (cardBackSprite != null && cardImage != null)
            {
                cardImage.sprite = cardBackSprite;
            }
        }
    }
}
