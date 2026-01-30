# Architecture

## Overview

PokerEngine is a headless, backend-only Texas Hold'em domain engine. It enforces game rules, manages state, and provides hooks for external consumers (UI, networking, persistence) without embedding any presentation or transport logic.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        External Consumer                            │
│              (Unity Client / Server / Test Harness)                 │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ PlayerAction
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          GameEngine                                 │
│   Orchestrates hand lifecycle, delegates to managers, mutates state │
└───────┬───────────────┬───────────────┬───────────────┬─────────────┘
        │               │               │               │
        ▼               ▼               ▼               ▼
┌───────────────┐ ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
│ActionValidator│ │ TurnManager   │ │ RoundManager  │ │  PotManager   │
│ (Rules layer) │ │ (Turn order)  │ │ (Phases/Deal) │ │ (Pots/Settle) │
└───────┬───────┘ └───────┬───────┘ └───────┬───────┘ └───────┬───────┘
        │                 │                 │                 │
        └─────────────────┴────────┬────────┴─────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           GameState                                 │
│      Single source of truth: players, deck, pots, phase, etc.       │
└─────────────────────────────────────────────────────────────────────┘
```

## Layer Breakdown

### Core (`PokerEngine/Core/`)

Domain primitives with no external dependencies or side effects.

| Class   | Responsibility |
|---------|----------------|
| `Card`  | Immutable value object representing rank + suit. |
| `Rank`  | Enum: Two–Ace (values 2–14 for comparison). |
| `Suit`  | Enum: Clubs, Diamonds, Hearts, Spades. |
| `Deck`  | Ordered card collection; draw/burn operations; built via RNG. |
| `Player`| Entity with ID, seat, stack, fold/all-in flags, hole cards. |

### State (`PokerEngine/State/`)

Aggregates that represent the authoritative game snapshot.

| Class       | Responsibility |
|-------------|----------------|
| `GameState` | Root aggregate: players, deck, community cards, blinds, dealer/turn pointers, pots, contributions, phase, completion flag. |
| `RoundState`| Per-betting-round data: current bet, last aggressor, last raise increment, contributions map, contesting players. |
| `GamePhase` | Enum: NotStarted → PreFlop → Flop → Turn → River → Showdown → Complete. |

### Rules (`PokerEngine/Rules/`)

Validation and intent modeling; no mutations.

| Class              | Responsibility |
|--------------------|----------------|
| `ActionType`       | Enum: Fold, Check, Call, Bet, Raise, AllIn. |
| `PlayerAction`     | Immutable intent DTO (playerId, type, amount). |
| `ActionValidator`  | Validates actions against state: turn order, stack sufficiency, min-raise, phase constraints. Returns `ValidationResult`. |
| `ValidationResult` | Success/failure container with error list. |
| `HandEvaluatorWrapper` | Adapter to plug external hand-ranking logic (stub). |

### Engine (`PokerEngine/Engine/`)

Orchestration and mutation logic.

| Class          | Responsibility |
|----------------|----------------|
| `GameEngine`   | Entry point: create state, start hand, apply actions, invoke showdown. Coordinates all managers. |
| `TurnManager`  | Determines next seat to act; detects when a betting round can close. |
| `RoundManager` | Advances phases (PreFlop→Flop→Turn→River→Showdown); deals community cards; resets round state. |
| `PotManager`   | Tracks contributions; builds side pots; settles winnings among eligible players. |
| `Pot`          | Value object: amount + set of eligible player IDs. |

### RNG (`PokerEngine/RNG/`)

Centralized entropy; no other class generates randomness.

| Class          | Responsibility |
|----------------|----------------|
| `SecureRandom` | Crypto-grade RNG wrapper (`RandomNumberGenerator`). |
| `ShuffleService` | Fisher–Yates shuffle using `SecureRandom`. |

### Interfaces (`PokerEngine/Interfaces/`)

Contracts for external observation.

| Interface       | Responsibility |
|-----------------|----------------|
| `IGameObserver` | Event sink for state transitions; implementations must not mutate state. |

## Design Principles

1. **Single Source of Truth**: `GameState` is authoritative; consumers read, only `GameEngine` writes.
2. **Validation First**: No mutation without prior approval from `ActionValidator`.
3. **Determinism**: Given the same RNG seed and action sequence, outcomes are reproducible.
4. **Separation of Concerns**: Rules validate; engine mutates; observers consume.
5. **No UI/Network**: Keeps the core portable and testable.
6. **Crypto Randomness**: Production shuffles use `SecureRandom`; seeds allowed only for testing/replay.

## Extension Points

- **Hand Evaluation**: Implement/inject a ranker via `HandEvaluatorWrapper`.
- **Observers**: Implement `IGameObserver` for logging, analytics, or UI sync.
- **Variants**: Extend `ActionValidator`/`GameEngine` for antes, straddles, or limit betting.
- **Persistence**: Serialize `GameState` and action logs for replay/audit.

## Security Considerations

- Crypto RNG prevents predictable shuffles.
- Validation guards prevent illegal state transitions.
- Single mutation path through `GameEngine` reduces attack surface.
- Immutable intent objects (`PlayerAction`, `Card`) prevent tampering.
