using System;
using System.Collections.Generic;
using System.Text;

// File: Deck.cs
// Purpose: Models a standard 52-card deck lifecycle for a hand.
// Responsible for: Holding an ordered collection of Card instances and exposing draw operations.
// Not responsible for: Randomness (delegated to ShuffleService), betting logic, or player state.
// Fit: Supplies cards to GameEngine and RoundManager while preserving deterministic ordering.

namespace PokerEngine.Core
{
    /// <summary>
    /// Represents a standard deck; intended to be constructed, shuffled externally, and consumed by the engine.
    /// </summary>
    internal class Deck
    {
    }
}
