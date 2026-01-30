using System;
using System.Collections.Generic;
using System.Text;

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
    internal class TurnManager
    {
    }
}
