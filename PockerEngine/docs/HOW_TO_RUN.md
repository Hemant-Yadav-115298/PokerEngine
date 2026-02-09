# How to Run the Poker Engine

## Prerequisites

- **.NET 10.0 SDK** (or later) installed
- Verify with: `dotnet --version`
- **Internet connection** (required on first build to download NuGet packages)

## Quick Start

### 1. Clone/Navigate to Repository

```bash
cd C:\Users\MGAsia\source\repos\PokerEngine
```

### 2. Restore Dependencies & Build

```bash
dotnet restore
dotnet build PokerEngine.slnx
```

Or build the console test directly (restore happens automatically):

```bash
dotnet build ConsoleTest/ConsoleTest.csproj
```

### 3. Run the Console Test

```bash
dotnet run --project ConsoleTest/ConsoleTest.csproj
```

## Gameplay Instructions

### Starting a Game

1. **Enter number of players** (2-9)
2. **Enter player names** (or press Enter for default)
3. **Enter starting stacks** (or press Enter for default 1000)

### During Your Turn

The console shows:
- Current game phase (PreFlop, Flop, Turn, River)
- Community cards on the board
- All players with stacks, bets, and status
- Your hole cards
- Available actions

### Action Commands

| Key | Action | Description |
|-----|--------|-------------|
| `F` | Fold | Surrender your hand |
| `K` | Check | Pass (when no bet to call) |
| `C` | Call | Match the current bet |
| `B` | Bet | Open betting (when no current bet) |
| `R` | Raise | Increase the bet |
| `A` | All-In | Bet all your remaining chips |

### Betting Amounts

- **Bet**: Enter amount when prompted (minimum = big blind)
- **Raise**: Enter total amount when prompted (minimum = current bet + last raise)

### Example Session

```
═══════════════════════════════════════════════════════════
       TEXAS HOLD'EM POKER ENGINE - CONSOLE TEST
═══════════════════════════════════════════════════════════

Enter number of players (2-9):
3
Enter name for Player 1 (seat 0): Alice
Enter starting stack for Alice (default 1000): 1000
  ✓ Alice added with $1,000 chips at seat 0
Enter name for Player 2 (seat 1): Bob
Enter starting stack for Bob (default 1000): 1000
  ✓ Bob added with $1,000 chips at seat 1
Enter name for Player 3 (seat 2): Carol
Enter starting stack for Carol (default 1000): 1000
  ✓ Carol added with $1,000 chips at seat 2

[Game Created] Starting hand...

───────────────────────────────────────────────────────────
Phase: PreFlop | Pot: $15 | Current Bet: $10

Players:
  Seat 0: Alice        Stack:   $1,000  Bet:     $0  (D)
  Seat 1: Bob          Stack:     $995  Bet:     $5
  Seat 2: Carol        Stack:     $990  Bet:    $10 ◀

>>> Carol's turn (Stack: $990)
  Your cards: A♠ | K♦
  Available actions:
    [F] Fold
    [C] Call ($0)
    [K] Check
    [R] Raise (min total $20)
    [A] All-In ($990)
  Enter action: R
    Enter total raise amount (min $20): 30
  ✓ Action applied successfully
```

### Showdown

When all betting rounds complete:
1. Remaining players' cards are revealed
2. Enter the winner's seat number (demo mode)
3. Pot is distributed
4. Option to play another hand

## Game Flow

```
Setup → PreFlop → Flop → Turn → River → Showdown → Complete
          ↓         ↓       ↓       ↓
      (betting) (betting) (betting) (betting)
```

### Blinds

- **Dealer (D)**: Button position
- **Small Blind**: Seat after dealer ($5 default)
- **Big Blind**: Seat after small blind ($10 default)

### Card Notation

- **Suits**: ♣ Clubs, ♦ Diamonds, ♥ Hearts, ♠ Spades
- **Ranks**: 2-10, J (Jack), Q (Queen), K (King), A (Ace)

## Troubleshooting

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build ConsoleTest/ConsoleTest.csproj
```

### Missing SDK

Download .NET 10.0 SDK from: https://dotnet.microsoft.com/download

### Invalid Action

If you see "Invalid action" errors:
- Check if it's your turn (◀ marker)
- Ensure you have enough chips
- Verify the action is allowed (e.g., can't check when facing a bet)

## Advanced Usage

### Programmatic Integration

```csharp
using PokerEngine.Core;
using PokerEngine.Engine;
using PokerEngine.RNG;
using PokerEngine.Rules;

// Setup
using var rng = new SecureRandom();
var shuffle = new ShuffleService();
var engine = new GameEngine();

var players = new[]
{
    new Player(Guid.NewGuid(), "Alice", 0, 1000m),
    new Player(Guid.NewGuid(), "Bob", 1, 1000m),
};

var state = engine.CreateGameState(players, 5m, 10m, 0, rng, shuffle);
engine.StartHand(state);

// Apply actions
var result = engine.ApplyAction(state, PlayerAction.Call(players[1].Id));
if (result.IsValid)
{
    // Action succeeded
}
```

### Running Tests

```bash
# If test project exists
dotnet test PokerEngine.Tests/PokerEngine.Tests.csproj
```
