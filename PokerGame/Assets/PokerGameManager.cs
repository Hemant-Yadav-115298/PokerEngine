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

    [Header("UI Theme")]
    public Color feltColor = new Color(0.06f, 0.28f, 0.19f, 1f);
    public Color tableEdgeColor = new Color(0.03f, 0.12f, 0.08f, 0.95f);
    public Color panelColor = new Color(0.12f, 0.12f, 0.12f, 0.82f);
    public Color accentColor = new Color(0.94f, 0.78f, 0.25f, 1f);
    public Color actionColor = new Color(0.12f, 0.45f, 0.28f, 1f);
    public Color dangerColor = new Color(0.65f, 0.14f, 0.14f, 1f);
    public Color neutralColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color activePlayerColor = new Color(0.94f, 0.78f, 0.25f, 1f);
    public Color inactivePlayerColor = new Color(0.12f, 0.12f, 0.12f, 0.82f);

    [Header("Player Panels GameObjects")]
    public GameObject player1Panel;
    public GameObject player2Panel;
    public GameObject player3Panel;

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
    
    // Panel outlines for active player highlight
    private Outline _player1PanelOutline;
    private Outline _player2PanelOutline;
    private Outline _player3PanelOutline;
    private Coroutine _activePlayerPulseCoroutine;

    void Start()
    {
        Debug.Log("Initializing Poker Game...");
        CachePlayerPanels();
        InitializeEngine();
        SetupButtons();
        ApplyTheme();
        StartNewGame();
    }

    void OnDestroy()
    {
        _rng?.Dispose();
        if (_activePlayerPulseCoroutine != null) StopCoroutine(_activePlayerPulseCoroutine);
    }

    void CachePlayerPanels()
    {
        if (player1Panel == null) player1Panel = GameObject.Find("Player1Panel");
        if (player2Panel == null) player2Panel = GameObject.Find("Player2Panel");
        if (player3Panel == null) player3Panel = GameObject.Find("Player3Panel");

        if (player1Panel != null)
        {
            _player1PanelOutline = player1Panel.GetComponent<Outline>();
            if (_player1PanelOutline == null) _player1PanelOutline = player1Panel.AddComponent<Outline>();
            _player1PanelOutline.effectDistance = new Vector2(4f, -4f);
        }
        if (player2Panel != null)
        {
            _player2PanelOutline = player2Panel.GetComponent<Outline>();
            if (_player2PanelOutline == null) _player2PanelOutline = player2Panel.AddComponent<Outline>();
            _player2PanelOutline.effectDistance = new Vector2(4f, -4f);
        }
        if (player3Panel != null)
        {
            _player3PanelOutline = player3Panel.GetComponent<Outline>();
            if (_player3PanelOutline == null) _player3PanelOutline = player3Panel.AddComponent<Outline>();
            _player3PanelOutline.effectDistance = new Vector2(4f, -4f);
        }
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

        // No bet to call - can check or bet
        if (toCall == 0)
        {
            // Check if there's already a bet in this round (need to use raise, not bet)
            if (round.CurrentBet > 0)
            {
                // Someone already bet, we need to raise
                var raiseAmount = _gameState.BigBlind * 2;
                if (bot.Stack >= toCall + raiseAmount)
                {
                    return random > 0.7f
                        ? PlayerAction.Raise(bot.Id, raiseAmount)
                        : PlayerAction.Check(bot.Id);
                }
                else
                {
                    return PlayerAction.Check(bot.Id);
                }
            }
            else
            {
                // First to act, can bet
                return random > 0.7f
                    ? PlayerAction.Bet(bot.Id, _gameState.BigBlind)
                    : PlayerAction.Check(bot.Id);
            }
        }
        // There's a bet to call
        else if (toCall > bot.Stack * 0.5m)
        {
            // Too expensive, mostly fold
            return random > 0.3f
                ? PlayerAction.Fold(bot.Id)
                : PlayerAction.Call(bot.Id);
        }
        else
        {
            // Affordable, mostly call
            if (random > 0.8f && bot.Stack > toCall + _gameState.BigBlind)
            {
                // Occasionally raise
                return PlayerAction.Raise(bot.Id, _gameState.BigBlind * 2);
            }
            else if (random > 0.2f)
            {
                return PlayerAction.Call(bot.Id);
            }
            else
            {
                return PlayerAction.Fold(bot.Id);
            }
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
        ClearActivePlayerHighlight();
        Debug.Log("=== SHOWDOWN START ===");
        yield return new WaitForSeconds(1.5f);

        var remaining = _gameState.Players.Where(p => !p.IsFolded).ToList();
        var players = _gameState.Players.ToList();
        Guid winnerId = Guid.Empty;

        if (remaining.Count == 1)
        {
            var winner = remaining[0];
            winnerId = winner.Id;
            var amount = _gameState.TotalContributions.Values.Sum();
            if (winnerText != null)
            {
                winnerText.text = $"{winner.Name} wins ${amount}!";
                StartCoroutine(WinnerTextAnimation());
            }
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
                winnerId = playerId;
                var handName = _evaluator.GetHandName(player.HoleCards, _gameState.CommunityCards);
                if (winnerText != null)
                {
                    winnerText.text = $"{player.Name} wins ${amount} with {handName}!";
                    StartCoroutine(WinnerTextAnimation());
                }
                Debug.Log($"{player.Name} wins ${amount} with {handName}");
            }
        }

        // Trigger win/lose card animations
        PlayWinLoseAnimations(winnerId, players);

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
        ResetAllCardEffects();
    }

    void UpdateAllUI()
    {
        UpdatePotAndPhase();
        UpdatePlayerPanels();
        UpdateCommunityCards();
        UpdateButtons();
        UpdateActivePlayerHighlight();
    }

    void PlayWinLoseAnimations(Guid winnerId, List<Player> players)
    {
        // Winner cards glow
        if (winnerId != Guid.Empty)
        {
            if (players[0].Id == winnerId)
            {
                foreach (var card in playerCardSlots) card?.ShowWinEffect();
            }
            else
            {
                foreach (var card in playerCardSlots) card?.ShowLoseEffect();
            }

            if (players[1].Id == winnerId)
            {
                foreach (var card in player2CardSlots) card?.ShowWinEffect();
            }
            else
            {
                foreach (var card in player2CardSlots) card?.ShowLoseEffect();
            }

            if (players[2].Id == winnerId)
            {
                foreach (var card in player3CardSlots) card?.ShowWinEffect();
            }
            else
            {
                foreach (var card in player3CardSlots) card?.ShowLoseEffect();
            }
        }
    }

    void ResetAllCardEffects()
    {
        foreach (var card in communityCardSlots) card?.ResetEffects();
        foreach (var card in playerCardSlots) card?.ResetEffects();
        foreach (var card in player2CardSlots) card?.ResetEffects();
        foreach (var card in player3CardSlots) card?.ResetEffects();
    }

    IEnumerator WinnerTextAnimation()
    {
        if (winnerText == null) yield break;

        var originalScale = winnerText.transform.localScale;
        float elapsed = 0f;
        float duration = 0.3f;

        // Scale up
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0.5f, 1.1f, t);
            winnerText.transform.localScale = originalScale * scale;
            yield return null;
        }

        // Scale back
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            float scale = Mathf.Lerp(1.1f, 1f, t);
            winnerText.transform.localScale = originalScale * scale;
            yield return null;
        }

        winnerText.transform.localScale = originalScale;
    }

    void UpdateActivePlayerHighlight()
    {
        var currentSeat = _gameState.CurrentSeatToAct;
        var players = _gameState.Players.ToList();

        // Clear all highlights
        SetPanelHighlight(_player1PanelOutline, false);
        SetPanelHighlight(_player2PanelOutline, false);
        SetPanelHighlight(_player3PanelOutline, false);

        // Highlight active player
        if (players.Count > 0 && players[0].SeatIndex == currentSeat)
        {
            SetPanelHighlight(_player1PanelOutline, true);
        }
        else if (players.Count > 1 && players[1].SeatIndex == currentSeat)
        {
            SetPanelHighlight(_player2PanelOutline, true);
        }
        else if (players.Count > 2 && players[2].SeatIndex == currentSeat)
        {
            SetPanelHighlight(_player3PanelOutline, true);
        }

        // Start pulse animation for active player
        if (_activePlayerPulseCoroutine != null) StopCoroutine(_activePlayerPulseCoroutine);
        _activePlayerPulseCoroutine = StartCoroutine(ActivePlayerPulseAnimation(currentSeat, players));
    }

    void SetPanelHighlight(Outline outline, bool active)
    {
        if (outline == null) return;
        outline.effectColor = active ? activePlayerColor : Color.clear;
    }

    void ClearActivePlayerHighlight()
    {
        if (_activePlayerPulseCoroutine != null)
        {
            StopCoroutine(_activePlayerPulseCoroutine);
            _activePlayerPulseCoroutine = null;
        }
        SetPanelHighlight(_player1PanelOutline, false);
        SetPanelHighlight(_player2PanelOutline, false);
        SetPanelHighlight(_player3PanelOutline, false);
    }

    IEnumerator ActivePlayerPulseAnimation(int currentSeat, List<Player> players)
    {
        Outline activeOutline = null;
        
        if (players.Count > 0 && players[0].SeatIndex == currentSeat) activeOutline = _player1PanelOutline;
        else if (players.Count > 1 && players[1].SeatIndex == currentSeat) activeOutline = _player2PanelOutline;
        else if (players.Count > 2 && players[2].SeatIndex == currentSeat) activeOutline = _player3PanelOutline;

        if (activeOutline == null) yield break;

        float elapsed = 0f;
        float pulseDuration = 0.8f;
        bool pulsingIn = true;

        while (true)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;

            if (pulsingIn)
            {
                activeOutline.effectColor = Color.Lerp(activePlayerColor * 0.6f, activePlayerColor, t);
                activeOutline.effectDistance = Vector2.Lerp(new Vector2(3f, -3f), new Vector2(5f, -5f), t);
            }
            else
            {
                activeOutline.effectColor = Color.Lerp(activePlayerColor, activePlayerColor * 0.6f, t);
                activeOutline.effectDistance = Vector2.Lerp(new Vector2(5f, -5f), new Vector2(3f, -3f), t);
            }

            if (t >= 1f)
            {
                elapsed = 0f;
                pulsingIn = !pulsingIn;
            }

            yield return null;
        }
    }

    void ApplyTheme()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            EnsureBackground(canvas.transform);
            EnsureTableSurface(canvas.transform);
        }

        StyleText(potText, accentColor, 34, FontStyles.Bold);
        StyleText(phaseText, Color.white, 26, FontStyles.Italic);
        if (winnerText != null)
        {
            StyleText(winnerText, accentColor, 64, FontStyles.Bold);
        }

        StyleText(player1NameText, Color.white, 26, FontStyles.Bold);
        StyleText(player1StackText, accentColor, 24, FontStyles.Normal);
        StyleText(player1BetText, Color.white, 22, FontStyles.Normal);

        StyleText(player2NameText, Color.white, 26, FontStyles.Bold);
        StyleText(player2StackText, accentColor, 24, FontStyles.Normal);
        StyleText(player2BetText, Color.white, 22, FontStyles.Normal);

        StyleText(player3NameText, Color.white, 26, FontStyles.Bold);
        StyleText(player3StackText, accentColor, 24, FontStyles.Normal);
        StyleText(player3BetText, Color.white, 22, FontStyles.Normal);

        StylePanel("Player1Panel");
        StylePanel("Player2Panel");
        StylePanel("Player3Panel");

        StyleCardSlots(communityCardSlots, 140, 200);
        StyleCardSlots(playerCardSlots, 120, 170);
        StyleCardSlots(player2CardSlots, 120, 170);
        StyleCardSlots(player3CardSlots, 120, 170);

        StyleButton(foldButton, dangerColor);
        StyleButton(checkButton, neutralColor);
        StyleButton(callButton, actionColor);
        StyleButton(betButton, actionColor);
        StyleButton(raiseButton, actionColor);
        StyleButton(allInButton, dangerColor);

        StyleInputField(betAmountInput);
    }

    void EnsureBackground(Transform canvas)
    {
        var existing = canvas.Find("Background");
        if (existing == null)
        {
            var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(canvas, false);
            bg.transform.SetAsFirstSibling();
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = bg.GetComponent<Image>();
            image.color = tableEdgeColor;
        }
        else
        {
            var image = existing.GetComponent<Image>();
            if (image != null) image.color = tableEdgeColor;
            existing.SetAsFirstSibling();
        }
    }

    void EnsureTableSurface(Transform canvas)
    {
        var existing = canvas.Find("TableSurface");
        if (existing == null)
        {
            var table = new GameObject("TableSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            table.transform.SetParent(canvas, false);
            table.transform.SetSiblingIndex(1);
            var rt = table.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.12f);
            rt.anchorMax = new Vector2(0.9f, 0.78f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = table.GetComponent<Image>();
            image.color = feltColor;
            image.type = Image.Type.Sliced;
        }
        else
        {
            var image = existing.GetComponent<Image>();
            if (image != null)
            {
                image.color = feltColor;
                image.type = Image.Type.Sliced;
            }
            existing.SetSiblingIndex(1);
        }
    }

    void StylePanel(string panelName)
    {
        var panel = GameObject.Find(panelName);
        if (panel == null) return;

        var image = panel.GetComponent<Image>();
        if (image != null)
        {
            image.color = panelColor;
            image.type = Image.Type.Sliced;
        }

        var outline = panel.GetComponent<Outline>();
        if (outline == null) outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    void StyleText(TMP_Text text, Color color, float size, FontStyles style)
    {
        if (text == null) return;
        text.color = color;
        text.fontSize = size;
        text.fontStyle = style;
        text.enableAutoSizing = false;
    }

    void StyleCardSlots(CardDisplay[] slots, float width, float height)
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            var image = slot.GetComponent<Image>();
            if (image != null)
            {
                image.preserveAspect = true;
            }

            var rt = slot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(width, height);
            }
        }
    }

    void StyleButton(Button button, Color baseColor)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = baseColor * 1.15f;
        colors.pressedColor = baseColor * 0.9f;
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        var text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.color = Color.white;
            text.fontSize = 26;
            text.fontStyle = FontStyles.Bold;
        }
    }

    void StyleInputField(TMP_InputField input)
    {
        if (input == null) return;

        var image = input.GetComponent<Image>();
        if (image != null)
        {
            image.color = panelColor;
            image.type = Image.Type.Sliced;
        }

        if (input.textComponent != null)
        {
            input.textComponent.color = Color.white;
            input.textComponent.fontSize = 24;
        }

        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.color = new Color(1f, 1f, 1f, 0.5f);
            if (string.IsNullOrWhiteSpace(placeholder.text))
            {
                placeholder.text = "Bet Amount";
            }
        }
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
            if (communityCardSlots[i] == null) continue;
            
            if (i < _gameState.CommunityCards.Count)
                communityCardSlots[i].SetCard(_gameState.CommunityCards[i], true);
            else
                communityCardSlots[i].ShowCardBack(false);
        }

        var players = _gameState.Players.ToList();

        for (int i = 0; i < playerCardSlots.Length; i++)
        {
            if (playerCardSlots[i] == null) continue;
            
            if (i < players[0].HoleCards.Count)
                playerCardSlots[i].SetCard(players[0].HoleCards[i], true);
            else
                playerCardSlots[i].ShowCardBack(false);
        }

        for (int i = 0; i < player2CardSlots.Length; i++)
        {
            if (player2CardSlots[i] == null) continue;
            
            if (i < players[1].HoleCards.Count)
                player2CardSlots[i].SetCard(players[1].HoleCards[i], true);
            else
                player2CardSlots[i].ShowCardBack(false);
        }

        for (int i = 0; i < player3CardSlots.Length; i++)
        {
            if (player3CardSlots[i] == null) continue;
            
            if (i < players[2].HoleCards.Count)
                player3CardSlots[i].SetCard(players[2].HoleCards[i], true);
            else
                player3CardSlots[i].ShowCardBack(false);
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