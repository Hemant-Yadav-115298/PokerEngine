using System;
using System.Collections.Generic;
using System.Text;

// File: ShuffleService.cs
// Purpose: Applies shuffling strategies to decks using SecureRandom-provided entropy.
// Responsible for: Producing deterministic, replayable shuffles when seeded; ensuring no other class manipulates deck order.
// Not responsible for: Generating randomness (SecureRandom), dealing cards, or managing game state.
// Fit: Bridge between RNG entropy and Core.Deck usage within the engine lifecycle.

namespace PokerEngine.RNG
{
    /// <summary>
    /// Encapsulates deck shuffling logic to keep randomness centralized and auditable.
    /// </summary>
    internal class ShuffleService
    {
    }
}
