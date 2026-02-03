using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerEngine.State;

/// <summary>
/// Manages all UI updates for the poker game.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI potText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Button startHandButton;
    [SerializeField] private Button foldButton;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button callButton;
    [SerializeField] private Button raiseButton;

    [Header("Player UI")]
    [SerializeField] private PlayerUIPanel[] playerPanels;

    private PokerGameManager gameManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PokerGameManager>();
        
        // Setup button listeners
        if (startHandButton != null)
            startHandButton.onClick.AddListener(() => gameManager?.StartNewHand());
        if (foldButton != null)
            foldButton.onClick.AddListener(() => gameManager?.OnFoldClicked());
        if (checkButton != null)
            checkButton.onClick.AddListener(() => gameManager?.OnCheckClicked());
        if (callButton != null)
            callButton.onClick.AddListener(() => gameManager?.OnCallClicked());
        if (raiseButton != null)
            raiseButton.onClick.AddListener(() => gameManager?.OnRaiseClicked());
    }

    public void EnablePlayerActions(bool enable)
    {
        if (foldButton != null) foldButton.interactable = enable;
        if (checkButton != null) checkButton.interactable = enable;
        if (callButton != null) callButton.interactable = enable;
        if (raiseButton != null) raiseButton.interactable = enable;
    }

    public void UpdateGameState(GameState state)
    {
        if (state == null) return;

        // Update pot
        if (potText != null)
        {
            var totalPot = state.TotalContributions.Values.Sum();
            potText.text = $"Pot: ${totalPot}";
        }

        // Update phase
        if (phaseText != null)
        {
            phaseText.text = $"Phase: {state.Phase}";
        }

        // Update player panels
        UpdatePlayerPanels(state);

        // Update button states
        UpdateButtonStates(state);
    }

    private void UpdatePlayerPanels(GameState state)
    {
        if (playerPanels == null) return;

        for (int i = 0; i < playerPanels.Length && i < state.Players.Count; i++)
        {
            var player = state.Players[i];
            playerPanels[i].UpdatePlayer(player, state.CurrentSeatToAct == i);
        }
    }

    private void UpdateButtonStates(GameState state)
    {
        bool isActive = !state.HandComplete && state.Phase != GamePhase.NotStarted;
        bool isHumanTurn = gameManager != null && gameManager.IsHumanTurn();
        
        if (foldButton != null) foldButton.interactable = isActive && isHumanTurn;
        if (checkButton != null) checkButton.interactable = isActive && isHumanTurn;
        if (callButton != null) callButton.interactable = isActive && isHumanTurn;
        if (raiseButton != null) raiseButton.interactable = isActive && isHumanTurn;
        if (startHandButton != null) startHandButton.interactable = !isActive;
    }
}
