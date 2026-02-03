using System;
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
    [SerializeField] private decimal startingStack = 1000m;
    [SerializeField] private decimal smallBlind = 5m;
    [SerializeField] private decimal bigBlind = 10m;

    [Header("References")]
    [SerializeField] private UIManager uiManager;

    private GameEngine gameEngine;
    private GameState gameState;
    private SecureRandom secureRandom;
    private ShuffleService shuffleService;

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
            players[i] = new Player(
                Guid.NewGuid(),
                $"Player {i + 1}",
                seatIndex: i,
                stack: startingStack
            );
        }

        // Create game state
        gameState = gameEngine.CreateGameState(
            players,
            smallBlind,
            bigBlind,
            dealerSeat: 0,
            secureRandom,
            shuffleService
        );

        Debug.Log("Poker game initialized with " + numberOfPlayers + " players");
    }

    public void StartNewHand()
    {
        if (gameState == null)
        {
            Debug.LogError("Game state not initialized!");
            return;
        }

        gameEngine.StartHand(gameState);
        Debug.Log("New hand started. Phase: " + gameState.Phase);

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateGameState(gameState);
        }
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

    public void PerformShowdown()
    {
        if (gameState == null || gameState.Phase != GamePhase.Showdown)
        {
            Debug.LogWarning("Cannot perform showdown - not in showdown phase");
            return;
        }

        // Evaluate hands
        var evaluator = new HandEvaluatorWrapper();
        var activePlayers = gameState.Players.Where(p => !p.IsFolded).ToList();

        var handRanks = new Dictionary<Guid, int>();
        foreach (var player in activePlayers)
        {
            var rank = evaluator.EvaluateHand(player.HoleCards, gameState.CommunityCards);
            handRanks[player.Id] = rank;
            
            var handName = evaluator.GetHandName(player.HoleCards, gameState.CommunityCards);
            Debug.Log($"{player.Name} has {handName}");
        }

        // Distribute winnings
        var payouts = gameEngine.Showdown(gameState, handRanks);

        foreach (var payout in payouts)
        {
            var player = gameState.Players.First(p => p.Id == payout.Key);
            Debug.Log($"{player.Name} wins {payout.Value}");
        }

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateGameState(gameState);
        }
    }

    // IGameObserver implementation
    public void OnGameEvent(string eventType, object data)
    {
        Debug.Log($"Game Event: {eventType}");
    }

    // Public getters for UI
    public GameState GetGameState() => gameState;
    public bool IsHandActive() => gameState != null && !gameState.HandComplete;
}
