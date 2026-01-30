using System;
using System.Collections.Generic;
using System.Linq;
using PokerEngine.Core;
using PokerEngine.Engine;
using PokerEngine.RNG;
using PokerEngine.Rules;
using PokerEngine.State;

namespace ConsoleTest
{
    public class Program
    {
        private static GameEngine _engine = new();
        private static GameState? _state;

        public static void Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("       TEXAS HOLD'EM POKER ENGINE - CONSOLE TEST");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();

            var players = SetupPlayers();
            using var rng = new SecureRandom();
            var shuffle = new ShuffleService();

            _state = _engine.CreateGameState(players, smallBlind: 5m, bigBlind: 10m, dealerSeat: 0, rng, shuffle);

            Console.WriteLine("\n[Game Created] Starting hand...\n");
            _engine.StartHand(_state);

            RunGameLoop();
        }

        private static List<Player> SetupPlayers()
        {
            Console.WriteLine("Enter number of players (2-9): ");
            int playerCount;
            while (!int.TryParse(Console.ReadLine(), out playerCount) || playerCount < 2 || playerCount > 9)
            {
                Console.WriteLine("Invalid input. Enter a number between 2 and 9:");
            }

            var players = new List<Player>();
            for (int i = 0; i < playerCount; i++)
            {
                Console.Write($"Enter name for Player {i + 1} (seat {i}): ");
                var name = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(name)) name = $"Player{i + 1}";

                Console.Write($"Enter starting stack for {name} (default 1000): ");
                var stackInput = Console.ReadLine();
                decimal stack = 1000m;
                if (!string.IsNullOrWhiteSpace(stackInput) && decimal.TryParse(stackInput, out var parsedStack))
                {
                    stack = parsedStack;
                }

                players.Add(new Player(Guid.NewGuid(), name, seatIndex: i, stack: stack));
                Console.WriteLine($"  ✓ {name} added with {stack:C0} chips at seat {i}");
            }

            return players;
        }

        private static void RunGameLoop()
        {
            while (_state != null && !_state.HandComplete && _state.Phase != GamePhase.Showdown)
            {
                // Check if anyone can still act (not folded, not all-in, has chips)
                var canAct = _state.Players.Any(p => !p.IsFolded && !p.IsAllIn && p.Stack > 0);
                if (!canAct)
                {
                    // All-in runout - no more actions needed
                    break;
                }

                PrintGameState();
                var currentPlayer = _state.GetPlayerBySeat(_state.CurrentSeatToAct);

                // Skip folded or all-in players (shouldn't happen but safety check)
                if (currentPlayer.IsFolded || currentPlayer.IsAllIn)
                {
                    Console.WriteLine($"\n[{currentPlayer.Name} cannot act - skipping]");
                    continue;
                }

                Console.WriteLine($"\n>>> {currentPlayer.Name}'s turn (Stack: {currentPlayer.Stack:C0})");
                PrintHoleCards(currentPlayer);
                PrintAvailableActions(currentPlayer);

                var action = GetPlayerAction(currentPlayer);
                if (action == null) continue;

                var result = _engine.ApplyAction(_state, action);
                if (!result.IsValid)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Invalid action: {string.Join(", ", result.Errors)}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Action applied successfully");
                    Console.ResetColor();
                }
            }

            if (_state != null)
            {
                HandleShowdownOrWin();
            }
        }

        private static void PrintGameState()
        {
            Console.WriteLine("\n───────────────────────────────────────────────────────────");
            Console.WriteLine($"Phase: {_state!.Phase} | Pot: {_state.TotalContributions.Values.Sum():C0} | Current Bet: {_state.RoundState.CurrentBet:C0}");

            if (_state.CommunityCards.Any())
            {
                Console.Write("Board: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Join(" | ", _state.CommunityCards.Select(FormatCard)));
                Console.ResetColor();
            }

            Console.WriteLine("\nPlayers:");
            foreach (var p in _state.Players)
            {
                var status = p.IsFolded ? "[FOLDED]" : p.IsAllIn ? "[ALL-IN]" : "";
                var contribution = _state.RoundState.GetContribution(p.Id);
                var marker = p.SeatIndex == _state.CurrentSeatToAct ? " ◀" : "";
                var dealerChip = p.SeatIndex == _state.DealerSeat ? " (D)" : "";
                Console.WriteLine($"  Seat {p.SeatIndex}: {p.Name,-12} Stack: {p.Stack,8:C0}  Bet: {contribution,6:C0} {status}{dealerChip}{marker}");
            }
        }

        private static void PrintHoleCards(Player player)
        {
            if (player.HoleCards.Count == 2)
            {
                Console.Write("  Your cards: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{FormatCard(player.HoleCards[0])} | {FormatCard(player.HoleCards[1])}");
                Console.ResetColor();
            }
        }

        private static void PrintAvailableActions(Player player)
        {
            var round = _state!.RoundState;
            var contribution = round.GetContribution(player.Id);
            var toCall = round.CurrentBet - contribution;

            Console.WriteLine("  Available actions:");
            Console.WriteLine("    [F] Fold");

            if (round.CurrentBet == 0 || contribution >= round.CurrentBet)
            {
                Console.WriteLine("    [K] Check");
            }

            if (toCall > 0 && player.Stack > 0)
            {
                Console.WriteLine($"    [C] Call ({toCall:C0})");
            }

            if (round.CurrentBet == 0 && player.Stack >= _state.BigBlind)
            {
                Console.WriteLine($"    [B] Bet (min {_state.BigBlind:C0})");
            }

            if (round.CurrentBet > 0 && player.Stack > toCall)
            {
                var minRaise = round.CurrentBet + round.LastRaiseAmount;
                Console.WriteLine($"    [R] Raise (min total {minRaise:C0})");
            }

            if (player.Stack > 0)
            {
                Console.WriteLine($"    [A] All-In ({player.Stack:C0})");
            }
        }

        private static PlayerAction? GetPlayerAction(Player player)
        {
            Console.Write("  Enter action: ");
            var input = Console.ReadLine()?.Trim().ToUpper();

            return input switch
            {
                "F" => PlayerAction.Fold(player.Id),
                "K" => PlayerAction.Check(player.Id),
                "C" => PlayerAction.Call(player.Id),
                "B" => GetBetAction(player),
                "R" => GetRaiseAction(player),
                "A" => PlayerAction.AllIn(player.Id, player.Stack),
                _ => null
            };
        }

        private static PlayerAction GetBetAction(Player player)
        {
            Console.Write($"    Enter bet amount (min {_state!.BigBlind:C0}): ");
            if (decimal.TryParse(Console.ReadLine(), out var amount))
            {
                return PlayerAction.Bet(player.Id, amount);
            }
            return PlayerAction.Bet(player.Id, _state.BigBlind);
        }

        private static PlayerAction GetRaiseAction(Player player)
        {
            var round = _state!.RoundState;
            var minRaise = round.CurrentBet + round.LastRaiseAmount;
            Console.Write($"    Enter total raise amount (min {minRaise:C0}): ");
            if (decimal.TryParse(Console.ReadLine(), out var amount))
            {
                return PlayerAction.Raise(player.Id, amount);
            }
            return PlayerAction.Raise(player.Id, minRaise);
        }

        private static void HandleShowdownOrWin()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");

            var remaining = _state!.Players.Where(p => !p.IsFolded).ToList();

            if (remaining.Count == 1)
            {
                var winner = remaining[0];
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"🏆 {winner.Name} WINS! (all others folded)");
                Console.WriteLine($"   Final stack: {winner.Stack:C0}");
                Console.ResetColor();
            }
            else if (_state.Phase == GamePhase.Showdown || _state.Phase == GamePhase.Complete || _state.CommunityCards.Count == 5)
            {
                Console.WriteLine("\n*** SHOWDOWN ***");
                Console.WriteLine($"\nBoard: {string.Join(" | ", _state.CommunityCards.Select(FormatCard))}");
                Console.WriteLine("\nHands:");

                // Use HandEvaluatorWrapper for automatic evaluation
                var evaluator = new HandEvaluatorWrapper();
                var handResults = new List<(Player Player, int Rank, string HandName)>();

                foreach (var p in remaining)
                {
                    if (p.HoleCards.Count == 2)
                    {
                        var rank = evaluator.EvaluateHand(p.HoleCards, _state.CommunityCards);
                        var handName = evaluator.GetHandName(p.HoleCards, _state.CommunityCards);
                        handResults.Add((p, rank, handName));

                        Console.WriteLine($"  {p.Name}: {FormatCard(p.HoleCards[0])} | {FormatCard(p.HoleCards[1])} → {handName}");
                    }
                }

                // Create hand ranks dictionary (lower rank = better hand)
                var handRanks = handResults.ToDictionary(r => r.Player.Id, r => r.Rank);

                var payouts = _engine.Showdown(_state, handRanks);

                // Find winners (those with payouts > 0)
                var winners = payouts.Where(p => p.Value > 0).ToList();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n🏆 WINNERS:");
                foreach (var (playerId, amount) in winners)
                {
                    var player = _state.GetPlayerById(playerId);
                    var handInfo = handResults.First(r => r.Player.Id == playerId);
                    Console.WriteLine($"  {player.Name} wins {amount:C0} with {handInfo.HandName}");
                }
                Console.ResetColor();
            }

            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            PrintFinalStandings();

            Console.WriteLine("\nPlay another hand? (Y/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() == "Y")
            {
                StartNewHand();
            }
        }

        private static void StartNewHand()
        {
            // Rotate dealer
            var seats = _state!.Players.Select(p => p.SeatIndex).OrderBy(s => s).ToList();
            var currentIndex = seats.IndexOf(_state.DealerSeat);
            _state.DealerSeat = seats[(currentIndex + 1) % seats.Count];

            // Reset players
            foreach (var p in _state.Players)
            {
                p.ResetForNewHand();
            }

            // Create new deck
            using var rng = new SecureRandom();
            var shuffle = new ShuffleService();
            var players = _state.Players.ToList();

            _state = _engine.CreateGameState(players, _state.SmallBlind, _state.BigBlind, _state.DealerSeat, rng, shuffle);
            _engine.StartHand(_state);

            Console.WriteLine("\n[New hand started!]\n");
            RunGameLoop();
        }

        private static void PrintFinalStandings()
        {
            Console.WriteLine("Final Standings:");
            foreach (var p in _state!.Players.OrderByDescending(p => p.Stack))
            {
                Console.WriteLine($"  {p.Name}: {p.Stack:C0}");
            }
        }

        private static string FormatCard(Card card)
        {
            var suitSymbol = card.Suit switch
            {
                Suit.Clubs => "♣",
                Suit.Diamonds => "♦",
                Suit.Hearts => "♥",
                Suit.Spades => "♠",
                _ => "?"
            };

            var rankDisplay = card.Rank switch
            {
                Rank.Two => "2",
                Rank.Three => "3",
                Rank.Four => "4",
                Rank.Five => "5",
                Rank.Six => "6",
                Rank.Seven => "7",
                Rank.Eight => "8",
                Rank.Nine => "9",
                Rank.Ten => "10",
                Rank.Jack => "J",
                Rank.Queen => "Q",
                Rank.King => "K",
                Rank.Ace => "A",
                _ => "?"
            };

            return $"{rankDisplay}{suitSymbol}";
        }
    }
}
