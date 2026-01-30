using System;
using System.Collections.Generic;
using System.Text;

// File: RoundManager.cs
// Purpose: Advances the hand through Texas Hold'em phases (pre-flop, flop, turn, river, showdown).
// Responsible for: Controlling phase transitions, dealing community cards, and resetting round-specific state between hands.
// Not responsible for: Validating individual player actions or calculating pots (delegated to rules and PotManager).
// Fit: Works with GameEngine to ensure legal progression and to expose clear state transitions to observers.

namespace PokerEngine.Engine
{
    /// <summary>
    /// Coordinates phase changes and card reveals while preserving deterministic ordering from the deck.
    /// </summary>
    internal class RoundManager
    {
    }
}
