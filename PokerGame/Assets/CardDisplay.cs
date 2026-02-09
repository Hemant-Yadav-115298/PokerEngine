using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PokerEngine.Core;

public class CardDisplay : MonoBehaviour
{
    [Header("Card Sprites")]
    public Sprite cardBackSprite;
    public bool useCardSprites = true;

    [Header("Text Display")]
    public TextMeshProUGUI label;

    [Header("Animation Settings")]
    public float flipDuration = 0.3f;
    public float dealDuration = 0.4f;
    public float highlightPulseDuration = 0.5f;

    [Header("Visual Effects")]
    public Color winGlowColor = new Color(1f, 0.84f, 0f, 1f);
    public Color loseColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);

    private Image cardImage;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Outline cardOutline;
    private Shadow cardShadow;
    private Card? currentCard;
    private bool isShowingBack = true;
    private Coroutine currentAnimation;
    private Vector3 originalScale;

    void Awake()
    {
        cardImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

        // Add CanvasGroup for fade effects
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Add outline for glow effects
        cardOutline = GetComponent<Outline>();
        if (cardOutline == null) cardOutline = gameObject.AddComponent<Outline>();
        cardOutline.effectColor = Color.clear;
        cardOutline.effectDistance = new Vector2(3f, -3f);

        // Add shadow for depth
        cardShadow = GetComponent<Shadow>();
        if (cardShadow == null) cardShadow = gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        cardShadow.effectDistance = new Vector2(4f, -4f);

        if (cardImage != null)
        {
            cardImage.preserveAspect = true;
        }
    }

    public void SetCard(Card card, bool animate = true)
    {
        currentCard = card;
        
        if (animate && isShowingBack)
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(FlipToFrontAnimation(card));
        }
        else
        {
            DisplayCardFront(card);
        }
        isShowingBack = false;
    }

    public void SetCardInstant(Card card)
    {
        currentCard = card;
        isShowingBack = false;
        DisplayCardFront(card);
    }

    private void DisplayCardFront(Card card)
    {
        var usedSprite = false;

        if (useCardSprites && cardImage != null)
        {
            if (CardSpriteLibrary.TryGetSprite(card, out var sprite) && sprite != null)
            {
                cardImage.sprite = sprite;
                cardImage.color = Color.white;
                usedSprite = true;
            }
        }

        if (label != null)
        {
            if (usedSprite)
            {
                label.text = "";
            }
            else
            {
                label.text = $"{GetRankDisplay(card.Rank)}{GetSuitSymbol(card.Suit)}";
                label.color = GetCardColor(card.Suit);
                label.fontSize = 32;
                label.fontStyle = FontStyles.Bold;
            }
        }

        if (!usedSprite && cardImage != null)
        {
            cardImage.color = Color.white;
        }
    }

    public void ShowCardBack(bool animate = false)
    {
        if (animate && !isShowingBack && currentCard.HasValue)
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(FlipToBackAnimation());
        }
        else
        {
            DisplayCardBack();
        }
        isShowingBack = true;
        currentCard = null;
    }

    private void DisplayCardBack()
    {
        if (cardImage == null) return;

        if (cardBackSprite != null)
        {
            cardImage.sprite = cardBackSprite;
            cardImage.color = Color.white;
        }
        else
        {
            // Fancy card back pattern
            cardImage.sprite = null;
            cardImage.color = new Color(0.15f, 0.25f, 0.55f, 1f); // Royal blue back
        }

        if (label != null)
        {
            label.text = "";
        }
    }

    private IEnumerator FlipToFrontAnimation(Card card)
    {
        float elapsed = 0f;
        float halfDuration = flipDuration / 2f;

        // First half: scale X to 0 (flip away)
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scaleX = Mathf.Lerp(1f, 0f, t);
            rectTransform.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
            yield return null;
        }

        // Switch to front at midpoint
        DisplayCardFront(card);

        // Second half: scale X back to 1 (flip to view)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scaleX = Mathf.Lerp(0f, 1f, t);
            rectTransform.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
            yield return null;
        }

        rectTransform.localScale = originalScale;
        currentAnimation = null;
    }

    private IEnumerator FlipToBackAnimation()
    {
        float elapsed = 0f;
        float halfDuration = flipDuration / 2f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scaleX = Mathf.Lerp(1f, 0f, t);
            rectTransform.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
            yield return null;
        }

        DisplayCardBack();

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scaleX = Mathf.Lerp(0f, 1f, t);
            rectTransform.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
            yield return null;
        }

        rectTransform.localScale = originalScale;
        currentAnimation = null;
    }

    public void DealAnimation(Vector3 fromPosition)
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(DealFromPositionAnimation(fromPosition));
    }

    private IEnumerator DealFromPositionAnimation(Vector3 fromPosition)
    {
        Vector3 targetPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = fromPosition;
        canvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < dealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dealDuration);
            rectTransform.anchoredPosition = Vector3.Lerp(fromPosition, targetPosition, t);
            canvasGroup.alpha = t;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        canvasGroup.alpha = 1f;
        currentAnimation = null;
    }

    public void ShowWinEffect()
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(WinGlowAnimation());
    }

    private IEnumerator WinGlowAnimation()
    {
        float elapsed = 0f;
        int pulseCount = 0;
        
        while (pulseCount < 3)
        {
            // Pulse glow on
            elapsed = 0f;
            while (elapsed < highlightPulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / highlightPulseDuration;
                cardOutline.effectColor = Color.Lerp(Color.clear, winGlowColor, t);
                float scale = Mathf.Lerp(1f, 1.1f, t);
                rectTransform.localScale = originalScale * scale;
                yield return null;
            }

            // Pulse glow off
            elapsed = 0f;
            while (elapsed < highlightPulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / highlightPulseDuration;
                cardOutline.effectColor = Color.Lerp(winGlowColor, Color.clear, t);
                float scale = Mathf.Lerp(1.1f, 1f, t);
                rectTransform.localScale = originalScale * scale;
                yield return null;
            }

            pulseCount++;
        }

        cardOutline.effectColor = Color.clear;
        rectTransform.localScale = originalScale;
        currentAnimation = null;
    }

    public void ShowLoseEffect()
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(LoseFadeAnimation());
    }

    private IEnumerator LoseFadeAnimation()
    {
        float elapsed = 0f;
        Color originalColor = cardImage.color;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.5f;
            cardImage.color = Color.Lerp(originalColor, loseColor, t);
            canvasGroup.alpha = Mathf.Lerp(1f, 0.6f, t);
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        // Reset
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;
            cardImage.color = Color.Lerp(loseColor, Color.white, t);
            canvasGroup.alpha = Mathf.Lerp(0.6f, 1f, t);
            yield return null;
        }

        currentAnimation = null;
    }

    public void ResetEffects()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        if (cardOutline != null) cardOutline.effectColor = Color.clear;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (rectTransform != null) rectTransform.localScale = originalScale;
        if (cardImage != null) cardImage.color = Color.white;
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
            ? new Color(0.85f, 0.1f, 0.1f) // Rich red
            : new Color(0.1f, 0.1f, 0.1f);  // Deep black
    }
}
