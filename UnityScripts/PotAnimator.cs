using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Animates pot transfer from center to winner.
/// </summary>
public class PotAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private GameObject potChipPrefab; // Optional: chip/money visual
    
    [Header("References")]
    [SerializeField] private Transform centerPotPosition;
    [SerializeField] private TextMeshProUGUI centerPotText;

    public IEnumerator AnimatePotToWinner(Transform winnerPosition, decimal amount)
    {
        if (centerPotPosition == null || winnerPosition == null)
        {
            Debug.LogWarning("Missing pot animation positions");
            yield break;
        }

        // Create visual effect for pot transfer
        GameObject potVisual = CreatePotVisual(amount);
        if (potVisual != null)
        {
            // Animate pot moving from center to winner
            yield return StartCoroutine(MovePotVisual(potVisual, centerPotPosition.position, winnerPosition.position));
            
            // Destroy the visual after animation
            Destroy(potVisual);
        }

        // Update center pot display to show 0
        if (centerPotText != null)
        {
            centerPotText.text = "POT: $0";
        }
    }

    private GameObject CreatePotVisual(decimal amount)
    {
        // Create a simple text visual for the pot amount
        GameObject potVisual = new GameObject("PotTransfer");
        potVisual.transform.SetParent(transform);
        
        // Add text component
        var textComponent = potVisual.AddComponent<TextMeshProUGUI>();
        textComponent.text = $"${amount}";
        textComponent.fontSize = 24;
        textComponent.color = Color.yellow;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        // Position at center pot
        potVisual.transform.position = centerPotPosition.position;
        
        return potVisual;
    }

    private IEnumerator MovePotVisual(GameObject potVisual, Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            float curveValue = movementCurve.Evaluate(progress);
            
            // Move the pot visual
            potVisual.transform.position = Vector3.Lerp(startPos, endPos, curveValue);
            
            // Optional: Add some scaling or rotation effects
            float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.2f;
            potVisual.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        // Ensure final position
        potVisual.transform.position = endPos;
        potVisual.transform.localScale = Vector3.one;
    }

    public IEnumerator AnimateStackUpdate(Transform playerPosition, decimal oldAmount, decimal newAmount)
    {
        // Create a visual effect showing the stack increase
        GameObject stackVisual = new GameObject("StackIncrease");
        stackVisual.transform.SetParent(transform);
        
        var textComponent = stackVisual.AddComponent<TextMeshProUGUI>();
        decimal increase = newAmount - oldAmount;
        textComponent.text = $"+${increase}";
        textComponent.fontSize = 20;
        textComponent.color = Color.green;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        // Position at player
        stackVisual.transform.position = playerPosition.position;
        
        // Animate upward movement and fade
        Vector3 startPos = playerPosition.position;
        Vector3 endPos = startPos + Vector3.up * 50f; // Move up
        
        float elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 2f;
            
            // Move up
            stackVisual.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            // Fade out
            Color color = textComponent.color;
            color.a = 1f - progress;
            textComponent.color = color;
            
            yield return null;
        }
        
        Destroy(stackVisual);
    }
}