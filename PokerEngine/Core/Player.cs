using System;
using System.Collections.Generic;
using System.Text;

// File: Player.cs
// Purpose: Represents a participant in a poker session with identity and stack data.
// Responsible for: Tracking player-specific state (chips, seat, status) that feeds into GameState.
// Not responsible for: Turn logic, action validation, UI input, or networking identity management.
// Fit: Serves as the domain entity referenced by actions, state transitions, and pot distribution.

namespace PokerEngine.Core
{
    /// <summary>
    /// Domain entity describing a player; designed to integrate with GameState and action processing.
    /// </summary>
    internal class Player
    {
    }
}
