using System;
using System.Collections.Generic;
using System.Linq;
using PokerEngine.State;

// File: PotManager.cs
// Purpose: Encapsulates pot creation, contribution tracking, and payout calculation.
// Responsible for: Managing wagers, side pots, and distributing winnings post-showdown based on validated outcomes.
// Not responsible for: Determining winner rankings (delegated to hand evaluation) or validating actions (rules layer).
// Fit: Called by GameEngine during betting rounds and showdown to keep GameState pot data consistent.

namespace PokerEngine.Engine
{
    /// <summary>
    /// Maintains pots and applies chip movements, keeping betting history aligned with GameState invariants.
    /// </summary>
    internal class PotManager
    {
        public void ApplyContribution(GameState state, Guid playerId, decimal amount)
        {
            if (!state.TotalContributions.ContainsKey(playerId))
            {
                state.TotalContributions[playerId] = 0m;
            }
            state.TotalContributions[playerId] += amount;
        }

        public List<Pot> BuildPots(GameState state, IReadOnlyCollection<Guid> eligiblePlayers)
        {
            var contributions = state.TotalContributions
                .Where(kv => kv.Value > 0)
                .OrderBy(kv => kv.Value)
                .ToList();

            var remainingContributors = new HashSet<Guid>(state.TotalContributions.Where(kv => kv.Value > 0).Select(kv => kv.Key));
            var remainingEligible = new HashSet<Guid>(eligiblePlayers);

            var pots = new List<Pot>();
            decimal previous = 0m;

            foreach (var kvp in contributions)
            {
                var slice = kvp.Value - previous;
                if (slice > 0 && remainingContributors.Count > 0)
                {
                    var amount = slice * remainingContributors.Count;
                    var eligibleForPot = remainingContributors.Where(remainingEligible.Contains).ToArray();
                    pots.Add(new Pot(amount, eligibleForPot));
                }

                remainingContributors.Remove(kvp.Key);
                remainingEligible.Remove(kvp.Key);
                previous = kvp.Value;
            }

            state.Pots.Clear();
            state.Pots.AddRange(pots);
            return pots;
        }

        public Dictionary<Guid, decimal> Settle(GameState state, IReadOnlyDictionary<Guid, int> handRanks, IReadOnlyCollection<Guid> eligiblePlayers)
        {
            var pots = BuildPots(state, eligiblePlayers);
            var payouts = eligiblePlayers.ToDictionary(id => id, _ => 0m);

            foreach (var pot in pots)
            {
                var contenders = pot.EligiblePlayers.Where(handRanks.ContainsKey).ToList();
                if (contenders.Count == 0)
                {
                    continue;
                }

                var bestRank = contenders.Min(id => handRanks[id]);
                var winners = contenders.Where(id => handRanks[id] == bestRank).ToList();
                var share = pot.Amount / winners.Count;

                foreach (var winner in winners)
                {
                    payouts[winner] += share;
                }
            }

            foreach (var payout in payouts)
            {
                var player = state.GetPlayerById(payout.Key);
                player.ReceivePayout(payout.Value);
            }

            return payouts;
        }
    }

    /// <summary>
    /// Represents a discrete pot with a set of eligible players.
    /// </summary>
    internal sealed class Pot
    {
        public Pot(decimal amount, IEnumerable<Guid> eligiblePlayers)
        {
            Amount = amount;
            EligiblePlayers = new HashSet<Guid>(eligiblePlayers);
        }

        public decimal Amount { get; }

        public HashSet<Guid> EligiblePlayers { get; }
    }
}
