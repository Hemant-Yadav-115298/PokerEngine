using System;
using System.Collections.Generic;
using System.Linq;
using HoldemPoker.Cards;
using HoldemPoker.Evaluator;

// File: HandEvaluatorWrapper.cs
// Purpose: Bridges the engine to a hand-evaluation component for ranking poker hands.
// Responsible for: Delegating to an evaluator to score hands, encapsulating external dependencies behind a stable interface.
// Not responsible for: Managing pots, determining winners based on betting context, or mutating GameState.
// Fit: Supplies PotManager and GameEngine with ranked outcomes during showdown without leaking evaluator details.

namespace PokerEngine.Rules
{
    /// <summary>
    /// Adapter layer for hand evaluation using HoldemPoker.Evaluator package.
    /// Lower rank value = better hand.
    /// </summary>
    internal sealed class HandEvaluatorWrapper
    {
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

            // Combine all 7 cards and convert to package format
            var allCards = holeCards.Concat(communityCards)
                .Select(ConvertCard)
                .ToArray();

            // Evaluate - lower ranking = better hand
            return HoldemHandEvaluator.GetHandRanking(allCards);
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

            var allCards = holeCards.Concat(communityCards)
                .Select(ConvertCard)
                .ToArray();

            return HoldemHandEvaluator.GetHandDescription(allCards);
        }

        /// <summary>
        /// Gets the hand category (e.g., Flush, FullHouse).
        /// </summary>
        public PokerHandCategory GetHandCategory(IReadOnlyList<Core.Card> holeCards, IReadOnlyList<Core.Card> communityCards)
        {
            if (holeCards.Count < 2 || communityCards.Count < 3)
                return PokerHandCategory.HighCard;

            var allCards = holeCards.Concat(communityCards)
                .Select(ConvertCard)
                .ToArray();

            return HoldemHandEvaluator.GetHandCategory(allCards);
        }

        private static Card ConvertCard(Core.Card card)
        {
            var rankChar = card.Rank switch
            {
                Core.Rank.Two => '2',
                Core.Rank.Three => '3',
                Core.Rank.Four => '4',
                Core.Rank.Five => '5',
                Core.Rank.Six => '6',
                Core.Rank.Seven => '7',
                Core.Rank.Eight => '8',
                Core.Rank.Nine => '9',
                Core.Rank.Ten => 'T',
                Core.Rank.Jack => 'J',
                Core.Rank.Queen => 'Q',
                Core.Rank.King => 'K',
                Core.Rank.Ace => 'A',
                _ => throw new ArgumentOutOfRangeException(nameof(card))
            };

            var suitChar = card.Suit switch
            {
                Core.Suit.Clubs => 'c',
                Core.Suit.Diamonds => 'd',
                Core.Suit.Hearts => 'h',
                Core.Suit.Spades => 's',
                _ => throw new ArgumentOutOfRangeException(nameof(card))
            };

            return Card.Parse($"{rankChar}{suitChar}");
        }
    }
}
