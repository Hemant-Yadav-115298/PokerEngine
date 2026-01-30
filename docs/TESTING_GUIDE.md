# Testing Guide

## Overview

This document outlines testing strategies, scenarios, and recommendations for the PokerEngine.

## Test Categories

### 1. Unit Tests

Test individual classes in isolation.

| Class              | Test Focus |
|--------------------|------------|
| `Card`             | Equality, hashing, string representation. |
| `Deck`             | Draw, burn, exhaustion exceptions. |
| `Player`           | Stack management, fold/reset, payout. |
| `PlayerAction`     | Factory methods, immutability. |
| `ActionValidator`  | All validation rules (see scenarios below). |
| `RoundState`       | Contribution tracking, reset, bet updates. |
| `SecureRandom`     | Range bounds, distribution (statistical). |
| `ShuffleService`   | All cards present post-shuffle, no duplicates. |
| `PotManager`       | Side pot construction, settlement math. |
| `TurnManager`      | Next seat selection, round-close detection. |
| `RoundManager`     | Phase transitions, card dealing. |

### 2. Integration Tests

Test `GameEngine` orchestrating full hand flows.

- Complete hand with showdown.
- Early win by folds.
- All-in scenarios with side pots.
- Multi-hand session with dealer rotation.

### 3. Edge Case Tests

Cover boundary conditions and unusual states.

## ActionValidator Scenarios

### Turn Ownership

| Scenario | Expected |
|----------|----------|
| Correct player acts | Valid |
| Wrong player acts | Invalid: "Not this player's turn" |

### Fold

| Scenario | Expected |
|----------|----------|
| Player in hand folds | Valid |
| Already folded player folds | Invalid: "Player already folded" |

### Check

| Scenario | Expected |
|----------|----------|
| No bet to match | Valid |
| Facing a bet | Invalid: "Cannot check facing a bet" |

### Call

| Scenario | Expected |
|----------|----------|
| Bet exists, player has chips | Valid |
| No bet to call | Invalid: "Nothing to call" |
| Player has no chips | Invalid: "Insufficient stack to call" |

### Bet

| Scenario | Expected |
|----------|----------|
| No current bet, amount ≥ BB | Valid |
| Current bet exists | Invalid: "Bet not allowed when a bet exists" |
| Amount < BB | Invalid: "Bet must be at least big blind" |
| Amount > stack | Invalid: "Bet exceeds stack. Use all-in" |

### Raise

| Scenario | Expected |
|----------|----------|
| Bet exists, amount ≥ currentBet + lastRaise | Valid |
| Amount < min raise target | Invalid: "Raise must meet or exceed last increment" |
| Amount ≤ current bet | Invalid: "Raise must exceed current bet" |
| Amount > stack | Invalid: "Raise exceeds stack. Use all-in" |
| No bet to raise | Invalid: "No bet to raise" |

### AllIn

| Scenario | Expected |
|----------|----------|
| Player has chips | Valid |
| Player already all-in | Invalid: "Player is all-in and cannot act" |
| Amount > stack | Invalid (but engine commits full stack) |

## Pot Scenarios

### Single Pot

- 3 players, equal contributions → single pot split.

### Side Pots

- Player A: 100, Player B: 200, Player C: 200.
- Main pot: 300 (100 × 3), eligible: A, B, C.
- Side pot: 200 (100 × 2), eligible: B, C.

### Folded Contributions

- Player folds after contributing.
- Contributions remain in pot but player ineligible for winnings.

## Hand Flow Scenarios

### Scenario: Complete Hand with Showdown

1. Setup: 3 players, blinds 5/10.
2. PreFlop: UTG raises to 30, SB folds, BB calls.
3. Flop: BB checks, UTG bets 40, BB calls.
4. Turn: Both check.
5. River: Both check.
6. Showdown: Evaluate hands, distribute pot.

### Scenario: Early Win by Folds

1. PreFlop: UTG raises, all others fold.
2. UTG wins pot immediately.

### Scenario: All-In with Side Pot

1. Player A (short stack) goes all-in for 50.
2. Player B raises to 200.
3. Player C calls 200.
4. Main pot: 150 (50 × 3).
5. Side pot: 300 (150 × 2).
6. Showdown: A can win main pot only; B/C contest side pot.

## Recommended Test Framework

- **xUnit** or **NUnit** for .NET.
- **Moq** or **NSubstitute** for mocking (e.g., `SecureRandom` for deterministic tests).

## Deterministic Testing

Seed `SecureRandom` or inject a mock RNG for reproducible shuffles:

```csharp
// Example: Mock RNG that returns predictable sequence
public class DeterministicRandom : SecureRandom
{
    private int _counter;
    public override int NextInt(int max) => _counter++ % max;
}
```

## Coverage Goals

| Area | Target |
|------|--------|
| Validation rules | 100% |
| Pot math | 100% |
| Phase transitions | 100% |
| Edge cases | 90%+ |
| Overall | 80%+ |

## Test File Structure

```
PokerEngine.Tests/
├── Core/
│   ├── CardTests.cs
│   ├── DeckTests.cs
│   └── PlayerTests.cs
├── Rules/
│   ├── ActionValidatorTests.cs
│   └── PlayerActionTests.cs
├── Engine/
│   ├── GameEngineTests.cs
│   ├── PotManagerTests.cs
│   ├── RoundManagerTests.cs
│   └── TurnManagerTests.cs
├── State/
│   ├── GameStateTests.cs
│   └── RoundStateTests.cs
├── RNG/
│   ├── SecureRandomTests.cs
│   └── ShuffleServiceTests.cs
└── Integration/
    ├── FullHandTests.cs
    └── MultiHandSessionTests.cs
```
