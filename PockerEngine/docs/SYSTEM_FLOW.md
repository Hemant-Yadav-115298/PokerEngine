# System Flow (Hand Lifecycle)

## Overview

A complete Texas Hold'em hand progresses through setup, betting rounds, and resolution. This document details each step with code entry points and state transitions.

```
┌─────────────────────────────────────────────────────────────────────┐
│                         HAND LIFECYCLE                              │
├─────────────────────────────────────────────────────────────────────┤
│  1. Setup         │ CreateGameState, construct players/deck        │
│  2. Start Hand    │ StartHand → deal hole cards, post blinds       │
│  3. Pre-Flop      │ Betting round (UTG first)                      │
│  4. Flop          │ Deal 3 community cards, betting round          │
│  5. Turn          │ Deal 1 community card, betting round           │
│  6. River         │ Deal 1 community card, betting round           │
│  7. Showdown      │ Evaluate hands, distribute pots                │
│  8. Complete      │ Reset for next hand or end session             │
└─────────────────────────────────────────────────────────────────────┘
```

## 1. Setup

### Create Game State

```csharp
using var rng = new SecureRandom();
var shuffle = new ShuffleService();
var engine = new GameEngine();

var players = new[]
{
    new Player(Guid.NewGuid(), "Alice", seatIndex: 0, stack: 1000m),
    new Player(Guid.NewGuid(), "Bob", seatIndex: 1, stack: 1000m),
    new Player(Guid.NewGuid(), "Carol", seatIndex: 2, stack: 1000m),
};

var state = engine.CreateGameState(
    players,
    smallBlind: 5m,
    bigBlind: 10m,
    dealerSeat: 0,
    rng,
    shuffle
);
```

**State after setup:**
- `Phase = NotStarted`
- `Deck` shuffled with 52 cards
- `Players` ordered by seat index
- `TotalContributions` initialized to zero

## 2. Start Hand

```csharp
engine.StartHand(state);
```

**Internal steps:**

1. Reset `HandComplete = false`, clear `Pots`, zero `TotalContributions`.
2. `RoundManager.StartHand`:
   - Set `Phase = PreFlop`.
   - Clear community cards.
   - Reset `RoundState` with all players contesting.
   - Deal 2 hole cards to each player with chips.
3. `PostBlinds`:
   - Small blind (seat after dealer) commits `SmallBlind`.
   - Big blind (seat after SB) commits `BigBlind`.
   - Record contributions; set `CurrentBet = BigBlind`, `LastRaiseAmount = BigBlind`.
4. `FirstToActAfterBlinds`: Set `CurrentSeatToAct` to seat after BB (UTG).

**State after start:**
- `Phase = PreFlop`
- Each player has 2 hole cards
- Blinds deducted from stacks and recorded
- `CurrentSeatToAct = UTG seat`

## 3. Betting Rounds

### Apply Action Loop

```csharp
while (!state.HandComplete && state.Phase < GamePhase.Showdown)
{
    var currentPlayer = state.GetPlayerBySeat(state.CurrentSeatToAct);
    var action = GetPlayerDecision(currentPlayer, state); // external input
    var result = engine.ApplyAction(state, action);
    
    if (!result.IsValid)
    {
        // Handle invalid action (prompt again or auto-fold)
    }
}
```

### Round Close Detection

After each action, `TurnManager.ShouldCloseRound` checks:

- All contesting players have matched `CurrentBet` or are all-in.
- No player needs to act further.

If round closes → `RoundManager.AdvancePhase`.

### Phase Advancement

| From      | To        | Action |
|-----------|-----------|--------|
| PreFlop   | Flop      | Burn 1, deal 3 community cards. |
| Flop      | Turn      | Burn 1, deal 1 community card. |
| Turn      | River     | Burn 1, deal 1 community card. |
| River     | Showdown  | No cards dealt; ready for evaluation. |

After each advance, `RoundState` resets:
- `CurrentBet = 0`
- `LastRaiseAmount = 0`
- `Contributions` cleared
- All non-folded players re-enter as contesting

### First to Act by Phase

| Phase    | First to Act |
|----------|--------------|
| PreFlop  | UTG (seat after BB) |
| Post-flop| First active seat after dealer |

## 4. Early Win

If only one player remains (others folded):

```csharp
if (state.Players.Count(p => !p.IsFolded) <= 1)
{
    var winner = state.Players.First(p => !p.IsFolded);
    var pot = state.TotalContributions.Values.Sum();
    winner.ReceivePayout(pot);
    state.HandComplete = true;
    state.Phase = GamePhase.Complete;
}
```

No showdown required.

## 5. Showdown

When `Phase = Showdown` (or reached via `ApplyAction` advancing):

```csharp
// External: evaluate hands and provide ranks (lower = better)
var handRanks = EvaluateHands(state); // Dictionary<Guid, int>

var payouts = engine.Showdown(state, handRanks);
// payouts: Dictionary<Guid, decimal> showing each player's winnings
```

**Internal steps:**

1. `PotManager.BuildPots`: Create side pots from `TotalContributions`.
2. For each pot:
   - Filter eligible players (contributed and not folded).
   - Find best rank among eligible.
   - Split pot among winners.
3. Apply payouts via `Player.ReceivePayout`.
4. Set `HandComplete = true`, `Phase = Complete`.

## 6. Next Hand

To play another hand:

1. Rotate dealer: `state.DealerSeat = NextSeat(state.DealerSeat)`.
2. Create a fresh `Deck` (via RNG).
3. Call `engine.StartHand(state)`.

Or create a new `GameState` for a fresh session.

## State Transition Diagram

```
NotStarted ──StartHand──▶ PreFlop ──RoundClose──▶ Flop
                              │                     │
                         EarlyWin              RoundClose
                              │                     │
                              ▼                     ▼
                          Complete              Turn
                                                   │
                                              RoundClose
                                                   │
                                                   ▼
                                                River
                                                   │
                                              RoundClose
                                                   │
                                                   ▼
                                               Showdown
                                                   │
                                               Settle
                                                   │
                                                   ▼
                                               Complete
```

## Error Handling

- Invalid actions return `ValidationResult` with errors; state unchanged.
- Exceptions thrown only for true invariant violations (e.g., deck exhausted unexpectedly).
- Callers should handle invalid results gracefully (re-prompt, auto-fold, logging).
