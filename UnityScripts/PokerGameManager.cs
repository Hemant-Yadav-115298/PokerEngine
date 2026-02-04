using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PokerEngine.Core;
using PokerEngine.Engine;
using PokerEngine.Rules;
using PokerEngine.State;
using PokerEngine.RNG;
using PokerEngine.Interfaces;

/// <summary>
/// Main Unity controller for the poker game.
/// Bridges the PokerEngine with Unity's MonoBehaviour lifecycle.
/// </summary>
public class PokerGameManager : MonoBehaviour, IGameObserver
{
    [Header("Game Settings")]
    [SerializeField] private int numberOfPlayers = 6;
    [SerializeField] private float startingStack = 1000f;
    [SerializeField] private float smallBlind = 5f;
    [SerializeField] private float bigBlind = 10f;
    [SerializeField] private float aiThinkTime = 1f;

    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private WinnerCelebration winnerCelebration;

    private GameEngine gameEngine;
    private GameState gameState;
    private SecureRandom secureRandom;
    private ShuffleService shuffleService;
    private bool isProcessingTurn = false;
    private int handsPlayed = 0;

    // Human player is always seat 0
    private const int HUMAN_PLAYER_SEAT = 0;

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Initialize RNG services
        secureRandom = new SecureRandom();
        shuffleService = new ShuffleService();
        gameEngine = new GameEngine();

        // Create players
        var players = new Player[numberOfPlayers];
        for (int i = 0; i < numberOfPlayers; i++)
        {
            string playerName = i == HUMAN_PLAYER_SEAT ? "You" : $"AI Player {i}";
            players[i] = new Player(
                Guid.NewGuid(),
                playerName,
                seatIndex: i,
                stack: (decimal)startingStack
            );
        }

        // Create game state
        gameState = gameEngine.CreateGameState(
            players,
            (decimal)smallBlind,
            (decimal)bigBlind,
            dealerSeat: 0,
            secureRandom,
            shuffleService
        );

        Debug.Log("Poker game initialized with " + numberOfPlayers + " players");
        
        if (uiManager != null)
        {
            uiManager.UpdateGameState(gameState);
        }
    }

    public void StartNewHand()
    {
        if (gameState == null)
        {
            Debug.LogError("Game state not initialized!");
            return;
        }

        gameEngine.StartHand(gameState);
        handsPlayed++;
        Debug.Log($"Hand #{handsPlayed} started. Phase: {gameState.Phase}");

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateGameState(gameState);
        }

        // Start AI turn processing
        StartCoroutine(ProcessTurns());
    }

    private IEnumerator ProcessTurns()
    {
        isProcessingTurn = true;

        while (!gameState.HandComplete && gameState.Phase != GamePhase.NotStarted && gameState.Phase != GamePhase.Showdown)
        {
            var currentPlayer = gameState.GetPlayerBySeat(gameState.CurrentSeatToAct);

            if (currentPlayer.IsFolded || currentPlayer.IsAllIn)
            {
                // Skip folded or all-in players
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // Check if it's human player's turn
            if (gameState.CurrentSeatToAct == HUMAN_PLAYER_SEAT)
            {
                Debug.Log("Your turn!");
                uiManager?.EnablePlayerActions(true);
                
                // Wait for human action
                yield return new WaitUntil(() => !IsHumanPlayerTurn());
            }
            else
            {
                // AI player turn
                Debug.Log($"{currentPlayer.Name}'s turn");
                uiManager?.EnablePlayerActions(false);
                
                yield return new WaitForSeconds(aiThinkTime);
                
                var aiAction = GetAIAction(currentPlayer);
                ProcessPlayerAction(aiAction);
            }

            yield return new WaitForSeconds(0.2f);
        }

        // Hand complete - check for showdown
        if (gameState.Phase == GamePhase.Showdown)
        {
            Debug.Log("Showdown!");
            yield return new WaitForSeconds(1f);
            PerformShowdown();
            
            // Show winner celebration
            if (winnerCelebration != null)
            {
                yield return StartCoroutine(winnerCelebration.CelebrateWinner(null));
            }
        }
        else if (gameState.HandComplete)
        {
            Debug.Log("Hand complete - winner by fold");
            yield return new WaitForSeconds(1f);
            
            // Find winner
            var winner = gameState.Players.FirstOrDefault(p => !p.IsFolded);
            if (winner != null)
            {
                Debug.Log($"{winner.Name} wins by fold!");
                
                // Show winner celebration
                if (winnerCelebration != null)
                {
                    yield return StartCoroutine(winnerCelebration.CelebrateWinner(null));
                }
            }
        }

        isProcessingTurn = false;
        
        // Update UI one final time
        if (uiManager != null)
        {
            uiManager.UpdateGameState(gameState);
        }

        // Check for game over BEFORE auto-starting next hand
        var playersWithChips = gameState.Players.Count(p => p.Stack > 0);
        if (playersWithChips <= 1)
        {
            // Game Over!
            yield return new WaitForSeconds(2f);
            ShowGameOver();
            yield break; // Stop here, don't auto-start
        }

        // Auto-start next hand after delay
        yield return new WaitForSeconds(3f);
        
        // Rotate dealer
        gameState.DealerSeat = (gameState.DealerSeat + 1) % gameState.Players.Count;
        
        StartNewHand();
    }

    private bool IsHumanPlayerTurn()
    {
        return !gameState.HandComplete && 
               gameState.CurrentSeatToAct == HUMAN_PLAYER_SEAT &&
               !gameState.GetPlayerBySeat(HUMAN_PLAYER_SEAT).IsFolded;
    }

    public void ProcessPlayerAction(PlayerAction action)
    {
        if (gameState == null || gameState.HandComplete)
        {
            Debug.LogWarning("Cannot process action - hand is complete or game not started");
            return;
        }

        var result = gameEngine.ApplyAction(gameState, action);

        if (!result.IsValid)
        {
            Debug.LogWarning("Invalid action: " + string.Join(", ", result.Errors));
            return;
        }

        var player = gameState.GetPlayerById(action.PlayerId);
        Debug.Log($"Action processed: {action.Type} by {player.Name}");

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateGameState(gameState);
        }

        // Check if hand is complete
        if (gameState.HandComplete)
        {
            Debug.Log("Hand complete!");
        }
    }

    private PlayerAction GetAIAction(Player player)
    {
        // Simple AI logic
        var round = gameState.RoundState;
        var contribution = round.GetContribution(player.Id);
        var toCall = round.CurrentBet - contribution;

        // Random decision making
        var random = UnityEngine.Random.value;

        // If no bet to call, check or bet small
        if (toCall == 0)
        {
            if (random > 0.7f && player.Stack > (decimal)bigBlind * 2)
            {
                // Bet
                var betAmount = round.CurrentBet + (decimal)bigBlind;
                return PlayerAction.Bet(player.Id, betAmount);
            }
            else
            {
                // Check
                return PlayerAction.Check(player.Id);
            }
        }
        else
        {
            // There's a bet to call
            if (random > 0.6f && player.Stack >= toCall)
            {
                // Call
                return PlayerAction.Call(player.Id);
            }
            else if (random > 0.8f && player.Stack > toCall + (decimal)bigBlind)
            {
                // Raise
                var raiseAmount = round.CurrentBet + (decimal)bigBlind;
                return PlayerAction.Raise(player.Id, raiseAmount);
            }
            else
            {
                // Fold
                return PlayerAction.Fold(player.Id);
            }
        }
    }

    public void PerformShowdown()
    {
        if (gameState == null)
        {
            Debug.LogWarning("Cannot perform showdown - game state is null");
            return;
        }

        Debug.Log("=== SHOWDOWN ===");

        // Evaluate hands
        var evaluator = new HandEvaluatorWrapper();
        var activePlayers = gameState.Players.Where(p => !p.IsFolded).ToList();

        if (activePlayers.Count == 0)
        {
            Debug.LogWarning("No active players for showdown");
            return;
        }

        var handRanks = new Dictionary<Guid, int>();
        
        Debug.Log($"Community Cards: {string.Join(", ", gameState.CommunityCards)}");
        
        foreach (var player in activePlayers)
        {
            var rank = evaluator.EvaluateHand(player.HoleCards, gameState.CommunityCards);
            handRanks[player.Id] = rank;
            
            var handName = evaluator.GetHandName(player.HoleCards, gameState.CommunityCards);
            var cards = string.Join(", ", player.HoleCards);
            Debug.Log($"{player.Name}: {cards} - {handName} (rank: {rank})");
        }

        // Distribute winnings
        var payouts = gameEngine.Showdown(gameState, handRanks);

        Debug.Log("=== WINNERS ===");
        foreach (var payout in payouts.Where(p => p.Value > 0))
        {
            var player = gameState.Players.First(p => p.Id == payout.Key);
            Debug.Log($"💰 {player.Name} wins ${payout.Value}! New stack: ${player.Stack}");
        }

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateGameState(gameState);
        }
    }

    private void ShowGameOver()
    {
        var winner = gameState.Players.FirstOrDefault(p => p.Stack > 0);
        if (winner != null && gameOverUI != null)
        {
            Debug.Log($"Game Over! {winner.Name} wins with ${winner.Stack}!");
            gameOverUI.Show(winner.Name, winner.Stack);
        }
    }

    // Public methods for UI buttons
    public void OnFoldClicked()
    {
        if (!IsHumanPlayerTurn()) return;
        
        var player = gameState.GetPlayerBySeat(HUMAN_PLAYER_SEAT);
        var action = PlayerAction.Fold(player.Id);
        ProcessPlayerAction(action);
    }

    public void OnCheckClicked()
    {
        if (!IsHumanPlayerTurn()) return;
        
        var player = gameState.GetPlayerBySeat(HUMAN_PLAYER_SEAT);
        var action = PlayerAction.Check(player.Id);
        ProcessPlayerAction(action);
    }

    public void OnCallClicked()
    {
        if (!IsHumanPlayerTurn()) return;
        
        var player = gameState.GetPlayerBySeat(HUMAN_PLAYER_SEAT);
        var action = PlayerAction.Call(player.Id);
        ProcessPlayerAction(action);
    }

    public void OnRaiseClicked()
    {
        if (!IsHumanPlayerTurn()) return;
        
        var player = gameState.GetPlayerBySeat(HUMAN_PLAYER_SEAT);
        var round = gameState.RoundState;
        
        // Simple raise: current bet + big blind
        var raiseAmount = round.CurrentBet + (decimal)bigBlind;
        var action = PlayerAction.Raise(player.Id, raiseAmount);
        ProcessPlayerAction(action);
    }

    // IGameObserver implementation
    public void OnGameEvent(string eventType, object data)
    {
        Debug.Log($"Game Event: {eventType}");
    }

    // Public getters for UI
    public GameState GetGameState() => gameState;
    public bool IsHandActive() => gameState != null && !gameState.HandComplete;
    public bool IsHumanTurn() => IsHumanPlayerTurn();
}
