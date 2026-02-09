using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PokerEngine.Core;
using PokerEngine.Engine;
using PokerEngine.RNG;
using PokerEngine.Rules;
using PokerEngine.State;

public class PokerGameManager : MonoBehaviour
{
    [Header("UI - Pot & Phase")]
    public TMP_Text potText;
    public TMP_Text phaseText;
    public TMP_Text winnerText;

    [Header("Community Cards")]
    public CardDisplay[] communityCardSlots = new CardDisplay[5];

    [Header("Player Hole Cards")]
    public CardDisplay[] playerCardSlots = new CardDisplay[2];
    public CardDisplay[] player2CardSlots = new CardDisplay[2];
    public CardDisplay[] player3CardSlots = new CardDisplay[2];

    [Header("Player Panels")]
    public TMP_Text player1NameText;
    public TMP_Text player1StackText;
    public TMP_Text player1BetText;

    public TMP_Text player2NameText;
    public TMP_Text player2StackText;
    public TMP_Text player2BetText;

    public TMP_Text player3NameText;
    public TMP_Text player3StackText;
    public TMP_Text player3BetText;

    [Header("Action Buttons")]
    public Button foldButton;
    public Button checkButton;
    public Button callButton;
    public Button betButton;
    public Button raiseButton;
    public Button allInButton;
    public TMP_InputField betAmountInput;

    private GameEngine _engine;
    private GameState _gameState;
    private SecureRandom _rng;
    private ShuffleService _shuffle;
    private HandEvaluatorWrapper _evaluator;
    private Guid _localPlayerId;
    
    // Deadlock prevention flags
    private bool _isProcessingShowdown = false;
    private bool _isBotTurnRunning = false;

    void Start()
    {
        Debug.Log("Initializing Poker Game...");
        InitializeEngine();
        SetupButtons();
        StartNewGame();
    }

    void OnDestroy()
    {
        _rng?.Dispose();
    }

    void InitializeEngine()
    {
        _engine = new GameEngine();
        _rng = new SecureRandom();
        _shuffle = new ShuffleService();
        _evaluator = new HandEvaluatorWrapper();
    }

    void SetupButtons()
    {
        foldButton.onClick.AddListener(OnFoldClicked);
        checkButton.onClick.AddListener(OnCheckClicked);
        callButton.onClick.AddListener(OnCallClicked);
        betButton.onClick.AddListener(OnBetClicked);
        raiseButton.onClick.AddListener(OnRaiseClicked);
        allInButton.onClick.AddListener(OnAllInClicked);
    }

    void StartNewGame()
    {
        var players = new List<Player>
        {
            new Player(Guid.NewGuid(), "You", 0, 1000m),
            new Player(Guid.NewGuid(), "Bot1", 1, 1000m),
            new Player(Guid.NewGuid(), "Bot2", 2, 1000m)
        };

        _localPlayerId = players[0].Id;

        _gameState = _engine.CreateGameState(
            players,
            smallBlind: 5m,
            bigBlind: 10m,
            dealerSeat: 0,
            _rng,
            _shuffle
        );

        StartNewHand();
    }

    void StartNewHand()
    {
        _isProcessingShowdown = false;
        _isBotTurnRunning = false;
        
        _engine.StartHand(_gameState);
        Debug.Log("Hand started!");
        UpdateAllUI();

        if (!IsLocalPlayerTurn())
        {
            ProcessBotTurns();
        }
    }

    void OnFoldClicked()
    {
        if (!IsLocalPlayerTurn()) return;
        ApplyAction(PlayerAction.Fold(_localPlayerId));
    }

    void OnCheckClicked()
    {
        if (!IsLocalPlayerTurn()) return;
        ApplyAction(PlayerAction.Check(_localPlayerId));
    }

    void OnCallClicked()
    {
        if (!IsLocalPlayerTurn()) return;
        ApplyAction(PlayerAction.Call(_localPlayerId));
    }

    void OnBetClicked()
    {
        if (!IsLocalPlayerTurn()) return;

        if (decimal.TryParse(betAmountInput.text, out decimal amount))
        {
            ApplyAction(PlayerAction.Bet(_localPlayerId, amount));
        }
        else
        {
            Debug.LogError("Invalid bet amount");
        }
    }

    void OnRaiseClicked()
    {
        if (!IsLocalPlayerTurn()) return;

        if (decimal.TryParse(betAmountInput.text, out decimal amount))
        {
            ApplyAction(PlayerAction.Raise(_localPlayerId, amount));
        }
        else
        {
            Debug.LogError("Invalid raise amount");
        }
    }

    void OnAllInClicked()
    {
        if (!IsLocalPlayerTurn()) return;
        var player = _gameState.GetPlayerById(_localPlayerId);
        ApplyAction(PlayerAction.AllIn(_localPlayerId, player.Stack));
    }

    void ApplyAction(PlayerAction action)
    {
        var result = _engine.ApplyAction(_gameState, action);

        if (!result.IsValid)
        {
            Debug.LogError("Invalid action: " + string.Join(", ", result.Errors));
            return;
        }

        Debug.Log($"Action applied! Phase: {_gameState.Phase}, HandComplete: {_gameState.HandComplete}");
        UpdateAllUI();

        CheckAndHandleGameProgression();
    }
    
    // Centralized logic to prevent duplicate showdown/bot coroutines
    void CheckAndHandleGameProgression()
    {
        if (_isProcessingShowdown)
        {
            return;
        }

        if (_gameState.HandComplete || _gameState.Phase == GamePhase.Showdown)
        {
            StartCoroutine(ShowdownCoroutine());
            return;
        }

        if (!IsLocalPlayerTurn())
        {
            ProcessBotTurns();
        }
    }
    
    // Prevent multiple bot coroutines from running simultaneously
    void ProcessBotTurns()
    {
        if (_isBotTurnRunning)
        {
            Debug.Log("Bot turn already running, skipping duplicate call");
            return;
        }
        StartCoroutine(BotTurnCoroutine());
    }

    IEnumerator BotTurnCoroutine()
    {
        _isBotTurnRunning = true;
        yield return new WaitForSeconds(1f);

        int maxIterations = 20; // Prevent infinite loops
        int iterations = 0;
        int lastSeat = -1;
        int sameSeatCounter = 0;

        while (iterations < maxIterations)
        {
            iterations++;

            // Exit conditions - check FIRST before any processing
            if (_gameState.HandComplete || 
                _gameState.Phase == GamePhase.Showdown || 
                _gameState.Phase == GamePhase.Complete ||
                IsLocalPlayerTurn())
            {
                Debug.Log($"Bot loop exit: HandComplete={_gameState.HandComplete}, Phase={_gameState.Phase}, IsPlayerTurn={IsLocalPlayerTurn()}");
                break;
            }

            var currentPlayer = _gameState.GetPlayerBySeat(_gameState.CurrentSeatToAct);

            // Validate player can act
            if (currentPlayer == null)
            {
                Debug.LogError("CurrentSeatToAct points to null player!");
                break;
            }

            if (currentPlayer.IsFolded || currentPlayer.IsAllIn || currentPlayer.Stack <= 0)
            {
                Debug.LogWarning($"{currentPlayer.Name} can't act (Folded={currentPlayer.IsFolded}, AllIn={currentPlayer.IsAllIn}, Stack={currentPlayer.Stack})");
                break;
            }

            // CRITICAL: Detect if turn is stuck (engine bug workaround)
            if (_gameState.CurrentSeatToAct == lastSeat)
            {
                sameSeatCounter++;
                if (sameSeatCounter > 2)
                {
                    Debug.LogError($"Turn stuck at seat {lastSeat} for {sameSeatCounter} iterations! Breaking to prevent deadlock.");
                    break;
                }
            }
            else
            {
                sameSeatCounter = 0;
            }
            lastSeat = _gameState.CurrentSeatToAct;

            // Execute bot action
            var botAction = DecideBotAction(currentPlayer);
            Debug.Log($"{currentPlayer.Name} performs {botAction.Type}");

            var result = _engine.ApplyAction(_gameState, botAction);
            
            if (!result.IsValid)
            {
                Debug.LogError($"Bot action failed: {string.Join(", ", result.Errors)}");
                break;
            }

            UpdateAllUI();
            yield return new WaitForSeconds(1f);

            // Check if game ended after this action
            if (_gameState.HandComplete || 
                _gameState.Phase == GamePhase.Showdown || 
                _gameState.Phase == GamePhase.Complete)
            {
                Debug.Log($"Game ended after bot action: Phase={_gameState.Phase}");
                break;
            }

            // Check if now player's turn
            if (IsLocalPlayerTurn())
            {
                Debug.Log("Player's turn, exiting bot loop");
                break;
            }
        }

        if (iterations >= maxIterations)
        {
            Debug.LogError("Bot loop exceeded max iterations - prevented infinite loop!");
        }

        _isBotTurnRunning = false;
        CheckAndHandleGameProgression();
    }

    PlayerAction DecideBotAction(Player bot)
    {
        var round = _gameState.RoundState;
        var contribution = round.GetContribution(bot.Id);
        var toCall = round.CurrentBet - contribution;

        var random = UnityEngine.Random.value;

        if (toCall == 0)
        {
            return random > 0.7f
                ? PlayerAction.Bet(bot.Id, _gameState.BigBlind)
                : PlayerAction.Check(bot.Id);
        }
        else if (toCall > bot.Stack * 0.5m)
        {
            return random > 0.3f
                ? PlayerAction.Fold(bot.Id)
                : PlayerAction.Call(bot.Id);
        }
        else
        {
            return random > 0.2f
                ? PlayerAction.Call(bot.Id)
                : PlayerAction.Fold(bot.Id);
        }
    }

    IEnumerator ShowdownCoroutine()
    {
        if (_isProcessingShowdown)
        {
            Debug.LogWarning("Showdown already in progress, skipping duplicate");
            yield break;
        }

        _isProcessingShowdown = true;
        Debug.Log("=== SHOWDOWN START ===");
        yield return new WaitForSeconds(1.5f);

        var remaining = _gameState.Players.Where(p => !p.IsFolded).ToList();

        if (remaining.Count == 1)
        {
            var winner = remaining[0];
            var amount = _gameState.TotalContributions.Values.Sum();
            if (winnerText != null)
                winnerText.text = $"{winner.Name} wins ${amount}!";
            Debug.Log($"{winner.Name} wins ${amount} (others folded)");
        }
        else if (_gameState.CommunityCards.Count >= 5)
        {
            var handsToEvaluate = remaining
                .Where(p => p.HoleCards.Count == 2)
                .Select(p => (p.Id, p.HoleCards))
                .ToList();

            var handRanks = _evaluator.EvaluateAllHands(handsToEvaluate, _gameState.CommunityCards);
            var payouts = _engine.Showdown(_gameState, handRanks);

            foreach (var (playerId, amount) in payouts.Where(p => p.Value > 0))
            {
                var player = _gameState.GetPlayerById(playerId);
                var handName = _evaluator.GetHandName(player.HoleCards, _gameState.CommunityCards);
                if (winnerText != null)
                    winnerText.text = $"{player.Name} wins ${amount} with {handName}!";
                Debug.Log($"{player.Name} wins ${amount} with {handName}");
            }
        }

        UpdateAllUI();
        yield return new WaitForSeconds(3f);

        if (winnerText != null)
            winnerText.text = "";

        RotateDealer();
        ResetPlayers();
        
        Debug.Log("=== SHOWDOWN END - Starting new hand ===");
        StartNewHand();
    }

    void RotateDealer()
    {
        var seats = _gameState.Players.Select(p => p.SeatIndex).OrderBy(s => s).ToList();
        var currentIndex = seats.IndexOf(_gameState.DealerSeat);
        _gameState.DealerSeat = seats[(currentIndex + 1) % seats.Count];
    }

    void ResetPlayers()
    {
        foreach (var p in _gameState.Players)
        {
            p.ResetForNewHand();
        }
    }

    void UpdateAllUI()
    {
        UpdatePotAndPhase();
        UpdatePlayerPanels();
        UpdateCommunityCards();
        UpdateButtons();
    }

    void UpdatePotAndPhase()
    {
        potText.text = $"Pot: ${_gameState.TotalContributions.Values.Sum()}";
        phaseText.text = $"Phase: {_gameState.Phase}";
    }

    void UpdatePlayerPanels()
    {
        var players = _gameState.Players.ToList();

        player1NameText.text = players[0].Name;
        player1StackText.text = $"${players[0].Stack}";
        player1BetText.text = $"Bet: ${_gameState.RoundState.GetContribution(players[0].Id)}";

        player2NameText.text = players[1].Name;
        player2StackText.text = $"${players[1].Stack}";
        player2BetText.text = $"Bet: ${_gameState.RoundState.GetContribution(players[1].Id)}";

        player3NameText.text = players[2].Name;
        player3StackText.text = $"${players[2].Stack}";
        player3BetText.text = $"Bet: ${_gameState.RoundState.GetContribution(players[2].Id)}";
    }

    void UpdateCommunityCards()
    {
        for (int i = 0; i < communityCardSlots.Length; i++)
        {
            if (i < _gameState.CommunityCards.Count)
                communityCardSlots[i].SetCard(_gameState.CommunityCards[i]);
            else
                communityCardSlots[i].ShowCardBack();
        }

        var players = _gameState.Players.ToList();

        for (int i = 0; i < playerCardSlots.Length; i++)
        {
            if (i < players[0].HoleCards.Count)
                playerCardSlots[i].SetCard(players[0].HoleCards[i]);
            else
                playerCardSlots[i].ShowCardBack();
        }

        for (int i = 0; i < player2CardSlots.Length; i++)
        {
            if (i < players[1].HoleCards.Count)
                player2CardSlots[i].SetCard(players[1].HoleCards[i]);
            else
                player2CardSlots[i].ShowCardBack();
        }

        for (int i = 0; i < player3CardSlots.Length; i++)
        {
            if (i < players[2].HoleCards.Count)
                player3CardSlots[i].SetCard(players[2].HoleCards[i]);
            else
                player3CardSlots[i].ShowCardBack();
        }
    }

    void UpdateButtons()
    {
        bool isPlayerTurn = IsLocalPlayerTurn();
        var player = _gameState.GetPlayerById(_localPlayerId);
        var round = _gameState.RoundState;
        var contribution = round.GetContribution(player.Id);
        var toCall = round.CurrentBet - contribution;

        if (player.IsFolded || player.IsAllIn || !isPlayerTurn)
        {
            DisableAllButtons();
            return;
        }

        foldButton.interactable = true;
        checkButton.interactable = (toCall == 0);
        callButton.interactable = (toCall > 0 && player.Stack >= toCall);
        betButton.interactable = (round.CurrentBet == 0 && player.Stack >= _gameState.BigBlind);
        raiseButton.interactable = (round.CurrentBet > 0 && player.Stack > toCall);
        allInButton.interactable = (player.Stack > 0);

        if (toCall > 0 && callButton.GetComponentInChildren<TMP_Text>() != null)
        {
            callButton.GetComponentInChildren<TMP_Text>().text = $"Call ${toCall}";
        }
    }

    void DisableAllButtons()
    {
        foldButton.interactable = false;
        checkButton.interactable = false;
        callButton.interactable = false;
        betButton.interactable = false;
        raiseButton.interactable = false;
        allInButton.interactable = false;
    }

    bool IsLocalPlayerTurn()
    {
        var currentPlayer = _gameState.GetPlayerBySeat(_gameState.CurrentSeatToAct);
        return currentPlayer != null && currentPlayer.Id == _localPlayerId;
    }
}