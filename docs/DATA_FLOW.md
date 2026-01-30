# Data Flow

## Overview

Data flows through the engine in a unidirectional pipeline: **Intent → Validation → Mutation → Observation**. This ensures predictable state transitions and clear responsibility boundaries.

```
┌───────────────┐      ┌─────────────────┐      ┌─────────────────┐      ┌───────────────┐
│ External Call │ ──▶  │ ActionValidator │ ──▶  │   GameEngine    │ ──▶  │ IGameObserver │
│ (PlayerAction)│      │ (Rules Check)   │      │ (State Mutation)│      │ (Event Sink)  │
└───────────────┘      └─────────────────┘      └─────────────────┘      └───────────────┘
                               │                        │
                               │ ValidationResult      │ Updated GameState
                               ▼                        ▼
                        ┌─────────────────┐      ┌─────────────────┐
                        │ Reject (errors) │      │ Proceed (valid) │
                        └─────────────────┘      └─────────────────┘
```

## Detailed Flow

### 1. Intent Creation

External caller constructs `PlayerAction`:

```csharp
var action = PlayerAction.Raise(playerId, amount);
```

`PlayerAction` is immutable; it captures **what** the player wants to do, not whether it's legal.

### 2. Validation

`GameEngine.ApplyAction` delegates to `ActionValidator.Validate`:

| Check                  | Description |
|------------------------|-------------|
| Hand complete?         | Reject if hand already finished. |
| Phase valid?           | Reject if phase ≥ Showdown. |
| Player exists?         | Reject unknown player ID. |
| Player folded?         | Reject if already folded. |
| Player all-in?         | Reject if already all-in (cannot act). |
| Turn ownership?        | Reject if not this player's turn. |
| Action-specific rules  | Bet sizing, call availability, raise increment, stack sufficiency. |

Result: `ValidationResult` with `IsValid` flag and `Errors` list.

### 3. Mutation (if valid)

`GameEngine` applies the action:

| Action  | Mutation |
|---------|----------|
| Fold    | Mark player folded; remove from contesting. |
| Check   | No chip movement; advance turn. |
| Call    | Commit chips to match current bet; record contribution. |
| Bet     | Commit chips; set current bet and raise increment. |
| Raise   | Commit additional chips; update current bet and raise increment. |
| AllIn   | Commit entire stack; update bet if exceeds current. |

Managers involved:

- `RoundState` tracks per-round contributions, current bet, last aggressor, last raise amount.
- `PotManager` tracks total contributions for side-pot construction.
- `TurnManager` determines next seat; checks round-close condition.
- `RoundManager` advances phase and deals community cards when round closes.

### 4. State Update

`GameState` is mutated:

- `CurrentSeatToAct` updated.
- `Phase` may advance.
- `CommunityCards` may grow.
- `TotalContributions` accumulate.
- `HandComplete` set if only one player remains or showdown settles.

### 5. Observation (future hook)

`IGameObserver` implementations receive notifications (to be wired):

- `OnActionApplied(PlayerAction, ValidationResult)`
- `OnPhaseAdvanced(GamePhase)`
- `OnPotUpdated(List<Pot>)`
- `OnHandComplete(Dictionary<Guid, decimal> payouts)`

Observers **must not** mutate state; they exist for logging, UI sync, or analytics.

## Contribution Tracking

```
Player commits chips
       │
       ▼
RoundState.RecordContribution(playerId, amount)
       │
       ▼
PotManager.ApplyContribution(state, playerId, amount)
       │
       ▼
GameState.TotalContributions[playerId] += amount
```

At showdown, `PotManager.BuildPots` iterates contributions to create side pots ordered by contribution level, then `Settle` distributes winnings.

## Early Win (Folds)

If all but one player folds:

1. Identify remaining player.
2. Sum `TotalContributions`.
3. `Player.ReceivePayout(total)`.
4. Set `HandComplete = true`, `Phase = Complete`.

No showdown or hand evaluation required.

## Showdown Settlement

1. `PotManager.BuildPots` creates side pots from contributions.
2. External caller provides `handRanks: Dictionary<Guid, int>` (lower = better).
3. For each pot, find eligible contenders with best rank; split evenly.
4. Apply payouts to player stacks.

## Thread Safety

Current implementation is **not thread-safe**. For concurrent games:

- Use separate `GameState` instances per game.
- Synchronize access if shared state is required.
- Consider immutable snapshots for read-only consumers.
