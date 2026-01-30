using System;
using System.Collections.Generic;
using System.Text;

// File: GameEngine.cs
// Purpose: Central orchestrator coordinating game phases, player turns, and pot management.
// Responsible for: Sequencing play, applying validated actions to GameState, delegating to managers for turns, rounds, and pots.
// Not responsible for: UI input/output, networking, or randomness generation (delegated to RNG services).
// Fit: Entry point for consuming PlayerAction instances and emitting state changes/observer notifications.

namespace PokerEngine.Engine
{
    /// <summary>
    /// Drives the poker hand lifecycle by invoking TurnManager, RoundManager, and PotManager while mutating GameState safely.
    /// </summary>
    internal class GameEngine
    {
    }
}
