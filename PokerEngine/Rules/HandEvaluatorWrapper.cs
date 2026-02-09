using System;
using System.Collections.Generic;
using System.Linq;

// File: HandEvaluatorWrapper.cs
// Purpose: Simplified hand evaluator compatible with Unity/.NET Standard 2.1
// Responsible for: Ranking poker hands for showdown without external dependencies
// Not responsible for: Managing pots, determining winners based on betting context, or mutating GameState.
// Fit: Supplies PotManager and GameEngine with ranked outcomes during showdown.

namespace PokerEngine.Rules
{
    /// <summary>
    /// Simple hand evaluation for Unity compatibility.
    /// Lower rank value = better hand.
    /// </summary>
    public sealed class HandEvaluatorWrapper
    {
        public enum HandRank
        {
            HighCard = 8,
            Pair = 7,
            TwoPair = 6,
            ThreeOfAKind = 5,
            Straight = 4,
            Flush = 3,
            FullHouse = 2,
            FourOfAKind = 1,
            StraightFlush = 0
        }

        /// <summary>
        /// Evaluates a player's best 5-card hand from hole cards + community cards.
        /// Returns a rank where lower = better hand.
        /// </summary>
        public int EvaluateHand(IReadOnlyList<Core.Card> holeCards, IReadOnlyList<Core.Card> communityCards)
        {
            if (holeCards.Count < 2)
                throw new ArgumentException("Need at least 2 hole cards", nameof(holeCards));
            if (communityCards.Count < 3)
                throw new ArgumentException("Need at least 3 community cards", nameof(communityCards));

            var allCards = holeCards.Concat(communityCards).ToList();
            var category = EvaluateHandCategory(allCards);
            
            // Simple ranking: category * 1000 + high card value
            var highCard = allCards.Max(c => (int)c.Rank);
            return ((int)category * 1000) + (14 - highCard);
        }

        /// <summary>
        /// Evaluates multiple players and returns their ranks (lower = better).
        /// </summary>
        public Dictionary<Guid, int> EvaluateAllHands(
            IEnumerable<(Guid PlayerId, IReadOnlyList<Core.Card> HoleCards)> players,
            IReadOnlyList<Core.Card> communityCards)
        {
            var results = new Dictionary<Guid, int>();

            foreach (var (playerId, holeCards) in players)
            {
                var rank = EvaluateHand(holeCards, communityCards);
                results[playerId] = rank;
            }

            return results;
        }

        /// <summary>
        /// Gets the hand category name for display purposes.
        /// </summary>
        public string GetHandName(IReadOnlyList<Core.Card> holeCards, IReadOnlyList<Core.Card> communityCards)
        {
            if (holeCards.Count < 2 || communityCards.Count < 3)
                return "Unknown";

            var allCards = holeCards.Concat(communityCards).ToList();
            var category = EvaluateHandCategory(allCards);
            
            return category switch
            {
                HandRank.StraightFlush => "Straight Flush",
                HandRank.FourOfAKind => "Four of a Kind",
                HandRank.FullHouse => "Full House",
                HandRank.Flush => "Flush",
                HandRank.Straight => "Straight",
                HandRank.ThreeOfAKind => "Three of a Kind",
                HandRank.TwoPair => "Two Pair",
                HandRank.Pair => "Pair",
                _ => "High Card"
            };
        }

        private HandRank EvaluateHandCategory(List<Core.Card> cards)
        {
            var isFlush = IsFlush(cards);
            var isStraight = IsStraight(cards);
            
            if (isFlush && isStraight) return HandRank.StraightFlush;
            if (HasFourOfAKind(cards)) return HandRank.FourOfAKind;
            if (HasFullHouse(cards)) return HandRank.FullHouse;
            if (isFlush) return HandRank.Flush;
            if (isStraight) return HandRank.Straight;
            if (HasThreeOfAKind(cards)) return HandRank.ThreeOfAKind;
            if (HasTwoPair(cards)) return HandRank.TwoPair;
            if (HasPair(cards)) return HandRank.Pair;
            
            return HandRank.HighCard;
        }

        private bool IsFlush(List<Core.Card> cards)
        {
            var suitCounts = cards.GroupBy(c => c.Suit).Select(g => g.Count());
            return suitCounts.Any(count => count >= 5);
        }

        private bool IsStraight(List<Core.Card> cards)
        {
            var ranks = cards.Select(c => (int)c.Rank).Distinct().OrderBy(r => r).ToList();
            
            // Check for 5 consecutive ranks
            for (int i = 0; i <= ranks.Count - 5; i++)
            {
                if (ranks[i + 4] - ranks[i] == 4)
                    return true;
            }
            
            // Check for wheel (A-2-3-4-5)
            if (ranks.Contains(14) && ranks.Contains(2) && ranks.Contains(3) && 
                ranks.Contains(4) && ranks.Contains(5))
                return true;
            
            return false;
        }

        private bool HasFourOfAKind(List<Core.Card> cards)
        {
            return cards.GroupBy(c => c.Rank).Any(g => g.Count() >= 4);
        }

        private bool HasFullHouse(List<Core.Card> cards)
        {
            var groups = cards.GroupBy(c => c.Rank).Select(g => g.Count()).OrderByDescending(c => c).ToList();
            return groups.Count >= 2 && groups[0] >= 3 && groups[1] >= 2;
        }

        private bool HasThreeOfAKind(List<Core.Card> cards)
        {
            return cards.GroupBy(c => c.Rank).Any(g => g.Count() >= 3);
        }

        private bool HasTwoPair(List<Core.Card> cards)
        {
            var pairs = cards.GroupBy(c => c.Rank).Where(g => g.Count() >= 2).Count();
            return pairs >= 2;
        }

        private bool HasPair(List<Core.Card> cards)
        {
            return cards.GroupBy(c => c.Rank).Any(g => g.Count() >= 2);
        }
    }
}
