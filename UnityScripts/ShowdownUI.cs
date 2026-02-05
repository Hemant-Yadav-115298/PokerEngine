/*  */using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Simple showdown UI that uses existing InfoPanel and adds countdown.
/// </summary>
public class ShowdownUI : MonoBehaviour
{
    [Header("Existing UI References")]
    [SerializeField] private TextMeshProUGUI potText; // From InfoPanel
    [SerializeField] private TextMeshProUGUI phaseText; // From InfoPanel

    [Header("Countdown Display")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Animation Settings")]
    [SerializeField] private float displayDuration = 3f;

    private void Start()
    {
        // Hide countdown panel initially
        if (countdownPanel != null) countdownPanel.SetActive(false);
    }

    public IEnumerator ShowWinner(string playerName, decimal amount, string handName)
    {
        // Use existing InfoPanel to show winner info
        if (potText != null)
            potText.text = $"{playerName} WINS ${amount}!";
        
        if (phaseText != null)
            phaseText.text = handName;

        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);
    }

    public IEnumerator ShowFoldWinner(string playerName, decimal amount)
    {
        yield return StartCoroutine(ShowWinner(playerName, amount, "All Others Folded"));
    }

    public IEnumerator ShowCountdown()
    {
        if (countdownPanel == null || countdownText == null) 
        {
            // Fallback: use InfoPanel for countdown
            yield return StartCoroutine(ShowCountdownInInfoPanel());
            yield break;
        }

        // Show countdown panel
        countdownPanel.SetActive(true);

        // Countdown from 3 to 1
        for (int i = 3; i >= 1; i--)
        {
            countdownText.text = $"Next hand starts in: {i}";
            yield return new WaitForSeconds(1f);
        }

        // "Starting!" message
        countdownText.text = "Starting New Hand!";
        yield return new WaitForSeconds(0.5f);

        // Hide countdown panel
        countdownPanel.SetActive(false);
    }

    private IEnumerator ShowCountdownInInfoPanel()
    {
        // Fallback: use existing InfoPanel for countdown
        for (int i = 3; i >= 1; i--)
        {
            if (phaseText != null)
                phaseText.text = $"Next hand: {i}";
            yield return new WaitForSeconds(1f);
        }

        if (phaseText != null)
            phaseText.text = "Starting!";
        yield return new WaitForSeconds(0.5f);
    }
}