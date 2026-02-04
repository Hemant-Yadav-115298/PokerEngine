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
    [SerializeField] private CommunityCardsDisplay communityCardsDisplay;
    [SerializeField] private CentralPotDisplay centralPotDisplay;

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

        // Update central pot
        if (centralPotDisplay != null)
        {
            var totalPot = state.TotalContributions.Values.Sum();
            centralPotDisplay.UpdatePot(totalPot);
        }

        // Update top pot text (keep for backup/debug)
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

        // Update community cards with current phase
        if (communityCardsDisplay != null)
        {
            if (state.Phase == GamePhase.NotStarted)
            {
                // Show all card backs at start
                communityCardsDisplay.ShowAllCardBacks();
            }
            else
            {
                // Update cards based on phase
                communityCardsDisplay.UpdateCards(state.CommunityCards, state.Phase);
            }
        }

        // Update player panels (normal mode - only show human cards)
        UpdatePlayerPanels(state, false);

        // Update button states
        UpdateButtonStates(state);
    }

    public void UpdateGameStateShowdown(GameState state)
    {
        if (state == null) return;

        // Update central pot
        if (centralPotDisplay != null)
        {
            var totalPot = state.TotalContributions.Values.Sum();
            centralPotDisplay.UpdatePot(totalPot);
        }

        // Update top pot text (keep for backup/debug)
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

        // Update community cards with current phase
        if (communityCardsDisplay != null)
        {
            communityCardsDisplay.UpdateCards(state.CommunityCards, state.Phase);
        }

        // Update player panels (showdown mode - show all active players' cards)
        UpdatePlayerPanels(state, true);

        // Update button states
        UpdateButtonStates(state);
    }

    private void UpdatePlayerPanels(GameState state, bool isShowdown = false)
    {
        if (playerPanels == null) return;

        for (int i = 0; i < playerPanels.Length && i < state.Players.Count; i++)
        {
            var player = state.Players[i];
            bool isActive = state.CurrentSeatToAct == i;
            bool isDealer = (state.DealerSeat == i);
            
            // Determine if we should show this player's cards
            bool showCards = false;
            if (isShowdown)
            {
                // During showdown, show cards for all active (non-folded) players
                showCards = !player.IsFolded;
            }
            else
            {
                // Normal play: only show human player's cards (seat 0)
                showCards = (i == 0);
            }
            
            // Get current ROUND bet for this player (not total contributions)
            decimal currentRoundBet = 0;
            if (state.RoundState != null && !state.HandComplete)
            {
                currentRoundBet = state.RoundState.GetContribution(player.Id);
            }
            
            playerPanels[i].UpdatePlayer(player, isActive, showCards, currentRoundBet, isDealer);
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
