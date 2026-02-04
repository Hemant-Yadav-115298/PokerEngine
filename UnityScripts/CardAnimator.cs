using System.Collections;
using UnityEngine;

/// <summary>
/// Handles card dealing and movement animations.
/// </summary>
public class CardAnimator : MonoBehaviour
{
    [SerializeField] private float dealDuration = 0.3f;
    [SerializeField] private AnimationCurve dealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public IEnumerator DealCardToPosition(Transform card, Vector3 targetPosition, float delay = 0f)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        Vector3 startPosition = card.position;
        float elapsed = 0f;

        while (elapsed < dealDuration)
        {
            elapsed += Time.deltaTime;
            float t = dealCurve.Evaluate(elapsed / dealDuration);
            card.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        card.position = targetPosition;
    }

    public IEnumerator FlipCard(CardVisual cardVisual, bool faceUp, float delay = 0f)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        Transform cardTransform = cardVisual.transform;
        float flipDuration = 0.2f;
        float elapsed = 0f;

        // Flip to 90 degrees
        while (elapsed < flipDuration / 2)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Lerp(0, 90, elapsed / (flipDuration / 2));
            cardTransform.localRotation = Quaternion.Euler(0, angle, 0);
            yield return null;
        }

        // Change card face
        cardVisual.SetFaceUp(faceUp);

        // Flip back to 0 degrees
        elapsed = 0f;
        while (elapsed < flipDuration / 2)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Lerp(90, 0, elapsed / (flipDuration / 2));
            cardTransform.localRotation = Quaternion.Euler(0, angle, 0);
            yield return null;
        }

        cardTransform.localRotation = Quaternion.identity;
    }
}
