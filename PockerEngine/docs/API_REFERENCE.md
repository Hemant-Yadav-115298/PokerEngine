# API Reference

## GameEngine

Central orchestrator for hand lifecycle.

### Methods

#### `CreateGameState`

```csharp
public GameState CreateGameState(
    IEnumerable<Player> players,
    decimal smallBlind,
    decimal bigBlind,
    int dealerSeat,
    SecureRandom rng,
    ShuffleService shuffle
)
```

Creates a new `GameState` with shuffled deck and initialized players.

**Parameters:**
- `players`: 2+ players with unique IDs and seat indices.
- `smallBlind`: Small blind amount (positive).
- `bigBlind`: Big blind amount (≥ smallBlind).
- `dealerSeat`: Seat index of the dealer button.
- `rng`: Crypto RNG for shuffling.
- `shuffle`: Shuffle service instance.

**Returns:** Initialized `GameState` with `Phase = NotStarted`.

**Throws:**
- `ArgumentNullException` if players, rng, or shuffle is null.
- `ArgumentException` if < 2 players, duplicate IDs/seats.
- `ArgumentOutOfRangeException` if blinds ≤ 0.

---

#### `StartHand`

```csharp
public void StartHand(GameState state)
```

Begins a new hand: deals hole cards, posts blinds, sets first actor.

**Preconditions:** `GameState` created; previous hand complete or not started.

**Postconditions:**
- `Phase = PreFlop`
- Players have hole cards
- Blinds posted
- `CurrentSeatToAct` set to UTG

---

#### `ApplyAction`

```csharp
public ValidationResult ApplyAction(GameState state, PlayerAction action)
```

Validates and applies a player action.

**Parameters:**
- `state`: Current game state.
- `action`: Player's intended action.

**Returns:** `ValidationResult` indicating success or errors.

**Side Effects (if valid):**
- Player stack adjusted
- Contributions recorded
- Turn advanced or phase changed
- `HandComplete` set if only one player remains

---

#### `Showdown`

```csharp
public Dictionary<Guid, decimal> Showdown(
    GameState state,
    IReadOnlyDictionary<Guid, int> handRanks
)
```

Settles pots based on hand rankings.

**Parameters:**
- `state`: Game state at showdown phase.
- `handRanks`: Player ID → rank (lower = better hand).

**Returns:** Map of player ID → payout amount.

**Postconditions:**
- Payouts applied to player stacks
- `HandComplete = true`
- `Phase = Complete`

---

## PlayerAction

Immutable intent DTO.

### Factory Methods

```csharp
PlayerAction.Fold(Guid playerId)
PlayerAction.Check(Guid playerId)
PlayerAction.Call(Guid playerId)
PlayerAction.Bet(Guid playerId, decimal amount)
PlayerAction.Raise(Guid playerId, decimal amount)
PlayerAction.AllIn(Guid playerId, decimal amount)
```

### Properties

| Property   | Type       | Description |
|------------|------------|-------------|
| `PlayerId` | `Guid`     | Acting player's ID. |
| `Type`     | `ActionType` | Action category. |
| `Amount`   | `decimal`  | Chips involved (0 for fold/check/call). |

---

## ValidationResult

Validation outcome container.

### Properties

| Property  | Type           | Description |
|-----------|----------------|-------------|
| `IsValid` | `bool`         | True if action is legal. |
| `Errors`  | `List<string>` | Error messages if invalid. |

### Static Methods

```csharp
ValidationResult.Success()
ValidationResult.Fail(IEnumerable<string> errors)
ValidationResult.From(IEnumerable<string> errors) // Success if empty
```

---

## GameState

Root aggregate (read properties for external consumers).

### Key Properties

| Property            | Type                      | Description |
|---------------------|---------------------------|-------------|
| `Players`           | `IReadOnlyList<Player>`   | Seated players. |
| `Deck`              | `Deck`                    | Card source. |
| `CommunityCards`    | `List<Card>`              | Board cards. |
| `Phase`             | `GamePhase`               | Current phase. |
| `DealerSeat`        | `int`                     | Dealer button seat. |
| `CurrentSeatToAct`  | `int`                     | Active player seat. |
| `SmallBlind`        | `decimal`                 | SB amount. |
| `BigBlind`          | `decimal`                 | BB amount. |
| `RoundState`        | `RoundState`              | Per-round data. |
| `TotalContributions`| `Dictionary<Guid, decimal>` | Cumulative bets. |
| `Pots`              | `List<Pot>`               | Side pots. |
| `HandComplete`      | `bool`                    | True if hand finished. |

### Methods

```csharp
Player GetPlayerById(Guid id)
Player GetPlayerBySeat(int seat)
IEnumerable<Player> ActivePlayers() // Non-folded
```

---

## Player

Domain entity for participant.

### Properties

| Property     | Type                | Description |
|--------------|---------------------|-------------|
| `Id`         | `Guid`              | Unique identifier. |
| `Name`       | `string`            | Display name. |
| `SeatIndex`  | `int`               | Table position. |
| `Stack`      | `decimal`           | Current chips. |
| `IsFolded`   | `bool`              | True if folded this hand. |
| `IsAllIn`    | `bool`              | True if stack = 0 and not folded. |
| `IsActive`   | `bool`              | True if can act (not folded, has chips). |
| `HoleCards`  | `IReadOnlyList<Card>` | Private cards. |

### Methods

```csharp
void GiveHoleCards(Card first, Card second)
void ResetForNewHand()
decimal CommitChips(decimal amount) // Returns actual committed
void Fold()
void ReceivePayout(decimal amount)
```

---

## RoundState

Per-betting-round data.

### Properties

| Property           | Type                          | Description |
|--------------------|-------------------------------|-------------|
| `CurrentBet`       | `decimal`                     | Bet to match. |
| `LastAggressorSeat`| `int?`                        | Last raiser's seat. |
| `LastRaiseAmount`  | `decimal`                     | Min-raise reference. |
| `Contributions`    | `IReadOnlyDictionary<Guid, decimal>` | This round's bets. |
| `ContestingPlayers`| `IReadOnlyCollection<Guid>`   | Players still in pot. |
| `CanClose`         | `bool`                        | Hint for round closure. |

### Methods

```csharp
void ResetForNewRound(IEnumerable<Guid> activePlayerIds)
void RecordContribution(Guid playerId, decimal amount)
void SetCurrentBet(decimal betSize, int aggressorSeat, decimal raiseAmount)
void MarkFold(Guid playerId)
void MarkReadyToClose()
decimal GetContribution(Guid playerId)
```

---

## SecureRandom

Crypto RNG wrapper.

### Methods

```csharp
int NextInt(int maxExclusive)
int NextInt(int minInclusive, int maxExclusive)
void FillBytes(Span<byte> destination)
void Dispose()
```

---

## ShuffleService

Fisher–Yates shuffler.

### Methods

```csharp
void ShuffleInPlace(IList<Card> cards, SecureRandom rng)
```

---

## Deck

Card container.

### Static Methods

```csharp
static Deck CreateStandard(SecureRandom rng, ShuffleService shuffleService)
```

### Methods

```csharp
Card Draw()
IReadOnlyList<Card> Draw(int count)
void Burn()
bool HasCards(int count = 1)
```

### Properties

| Property | Type                  | Description |
|----------|-----------------------|-------------|
| `Cards`  | `IReadOnlyList<Card>` | Full deck (read-only). |
