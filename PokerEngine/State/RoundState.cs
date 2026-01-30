using System;
using System.Collections.Generic;
using System.Text;

// File: RoundState.cs
// Purpose: Captures transient data for the current betting round within a hand.
// Responsible for: Tracking current bets, player positions, and flags needed to determine when a round closes.
// Not responsible for: Overall session data (GameState) or pot distribution (PotManager handles chip movements).
// Fit: Round-scoped data that TurnManager and RoundManager consult to manage intra-hand flow.

namespace PokerEngine.State
{
    /// <summary>
    /// Holds per-round context to coordinate betting progress and readiness for phase advancement.
    /// </summary>
    internal class RoundState
    {
    }
}
