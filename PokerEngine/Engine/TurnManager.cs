using System;
using System.Collections.Generic;
using System.Linq;
using PokerEngine.State;

// File: TurnManager.cs
// Purpose: Enforces player turn order and manages action sequencing within a betting round.
// Responsible for: Determining the next actor, handling blinds/straddles kickoff, and ensuring rotations respect folds and all-ins.
// Not responsible for: Validating action legality (rules layer) or adjusting pots (PotManager handles chip movement).
// Fit: Serves GameEngine by providing deterministic turn advancement tied to current GameState and RoundState.

namespace PokerEngine.Engine
{
    /// <summary>
    /// Maintains orderly progression of player turns while signaling when a betting round can close.
    /// </summary>
    internal sealed class TurnManager
    {
        public int NextSeat(GameState state)
        {
            var seats = state.Players.Select(p => p.SeatIndex).OrderBy(s => s).ToList();
            if (seats.Count == 0) return state.CurrentSeatToAct;

            var currentIndex = seats.IndexOf(state.CurrentSeatToAct);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            for (var i = 1; i <= seats.Count; i++)
            {
                var seat = seats[(currentIndex + i) % seats.Count];
                var player = state.GetPlayerBySeat(seat);
                if (!player.IsFolded && !player.IsAllIn && player.Stack > 0)
                {
                    return seat;
                }
            }

            return state.CurrentSeatToAct;
        }

        public bool ShouldCloseRound(GameState state)
        {
            var round = state.RoundState;
            var contesting = round.ContestingPlayers.ToList();
            if (contesting.Count == 0)
            {
                return true;
            }

            foreach (var playerId in contesting)
            {
                var player = state.GetPlayerById(playerId);
                if (player.IsFolded)
                {
                    continue;
                }

                var contribution = round.GetContribution(playerId);
                var needsToAct = !player.IsAllIn && contribution < round.CurrentBet;
                if (needsToAct)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
