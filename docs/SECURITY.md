# Security Considerations

## Overview

PokerEngine is designed as a backend-only game logic library. Security focuses on preventing cheating, ensuring fair play, and maintaining data integrity.

## Threat Model

| Threat | Mitigation |
|--------|------------|
| Predictable shuffle | Crypto RNG (`SecureRandom`). |
| Illegal state mutations | Validation-first architecture. |
| Turn manipulation | Turn ownership checks in `ActionValidator`. |
| Pot tampering | Single mutation path through `PotManager`. |
| Action replay attacks | Immutable `PlayerAction` objects; state includes phase context. |
| Information leakage | Hole cards only exposed through `Player.HoleCards`; consumers control visibility. |

## Cryptographic Randomness

### Implementation

```csharp
internal sealed class SecureRandom : IDisposable
{
    private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
    
    public int NextInt(int maxExclusive)
    {
        Span<byte> buffer = stackalloc byte[4];
        _rng.GetBytes(buffer);
        var value = BitConverter.ToUInt32(buffer);
        return (int)(value % (uint)maxExclusive);
    }
}
```

### Guarantees

- Uses `System.Security.Cryptography.RandomNumberGenerator`.
- Unpredictable output; no seed exposure.
- Shuffle cannot be reverse-engineered from observable state.

### Testing Mode

For deterministic tests, inject a mock RNG. **Never** use weak RNG in production.

## Validation-First Architecture

### Principle

No state mutation occurs without prior validation approval.

```
PlayerAction → ActionValidator.Validate() → if (valid) GameEngine.Apply()
```

### Validation Checks

| Check | Purpose |
|-------|---------|
| `state.HandComplete` | Prevent actions on finished hands. |
| `state.Phase >= Showdown` | Block actions during resolution. |
| `player.IsFolded` | Block folded players. |
| `player.IsAllIn` | Block all-in players. |
| `player.SeatIndex == state.CurrentSeatToAct` | Enforce turn order. |
| Action-specific rules | Bet sizing, raise increments, stack sufficiency. |

### Rejection Handling

- Invalid actions return `ValidationResult` with errors.
- State remains unchanged; no partial updates.
- Caller must handle rejection (re-prompt, logging, etc.).

## Single Mutation Path

### Design

Only `GameEngine` mutates `GameState`. All other components either:

- Read state (validators, managers for decision logic).
- Produce new objects (immutable DTOs).

### Benefit

- Audit trail: All mutations flow through one class.
- Easier to add logging/hooks.
- Prevents scattered state changes.

## Input Hardening

### GameState Construction

```csharp
if (players == null) throw new ArgumentNullException(nameof(players));
if (_players.Count < 2) throw new ArgumentException("At least two players required.");
if (_players.Select(p => p.SeatIndex).Distinct().Count() != _players.Count)
    throw new ArgumentException("Seat indices must be unique.");
if (_players.Select(p => p.Id).Distinct().Count() != _players.Count)
    throw new ArgumentException("Player IDs must be unique.");
if (smallBlind <= 0) throw new ArgumentOutOfRangeException(nameof(smallBlind));
if (bigBlind <= 0 || bigBlind < smallBlind) throw new ArgumentOutOfRangeException(nameof(bigBlind));
```

### Player Construction

```csharp
if (seatIndex < 0) throw new ArgumentOutOfRangeException(nameof(seatIndex));
if (stack < 0) throw new ArgumentOutOfRangeException(nameof(stack));
```

## Information Hiding

### Hole Cards

- `Player.HoleCards` is internal; consumers must explicitly expose.
- Consider: Return empty collection to opponents; full collection to owner.

### Deck State

- `Deck.Cards` exposes full list (for debugging); production consumers should not display undealt cards.
- Consider: Remove or restrict `Cards` property in release builds.

## Concurrency

### Current State

- **Not thread-safe**: Single-threaded assumption.
- Each game session should use a separate `GameState`.

### Recommendations

- Use separate instances for concurrent games.
- Add locking if shared state is required.
- Consider immutable snapshots for read-only consumers.

## Logging & Audit

### Recommendations

- Log all `PlayerAction` attempts and results.
- Log phase transitions.
- Log pot distributions.
- Store action history for replay/dispute resolution.

### Implementation (future)

Wire `IGameObserver` to emit events:

```csharp
public interface IGameObserver
{
    void OnActionAttempted(PlayerAction action, ValidationResult result);
    void OnStateChanged(GameState snapshot);
    void OnHandComplete(Dictionary<Guid, decimal> payouts);
}
```

## Anti-Cheat

### In-Scope

- Deterministic logic prevents outcome manipulation.
- Crypto RNG prevents shuffle prediction.
- Validation prevents illegal moves.

### Out-of-Scope

- Client-side anti-cheat (UI tampering).
- Bot detection.
- Collusion detection.
- Network-level security (TLS, authentication).

These require additional systems layered on top of the engine.

## Security Checklist

- [ ] Use `SecureRandom` for all shuffles in production.
- [ ] Never expose `Deck.Cards` to players.
- [ ] Validate all actions before mutation.
- [ ] Log all action attempts.
- [ ] Use separate `GameState` per concurrent game.
- [ ] Restrict `Player.HoleCards` visibility per player.
- [ ] Review and harden all public entry points.
