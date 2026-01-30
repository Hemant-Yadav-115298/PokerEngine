using System;
using System.Collections.Generic;
using System.Text;

// File: GameState.cs
// Purpose: Single source of truth describing the current poker session and active hand state.
// Responsible for: Holding player data, deck references, pots, betting history, and current phase/turn context.
// Not responsible for: Business rules enforcement (rules layer), randomness, or presenting data to UI.
// Fit: Central aggregate mutated by GameEngine after validation; all other components read from this authoritative state.

namespace PokerEngine.State
{
    /// <summary>
    /// Authoritative snapshot of session and hand data; only mutated through validated engine operations.
    /// </summary>
    internal class GameState
    {
    }
}
