using System;
using System.Collections.Generic;
using System.Text;

// File: SecureRandom.cs
// Purpose: Provides cryptographically strong random numbers for shuffling and seeding.
// Responsible for: Centralizing entropy generation so gameplay randomness can be audited and seeded when needed.
// Not responsible for: Shuffling algorithms (handled by ShuffleService) or any game logic.
// Fit: Only approved source of randomness for the engine to maintain determinism and security.

namespace PokerEngine.RNG
{
    /// <summary>
    /// Cryptographically secure RNG wrapper intended for deck seeding and any stochastic operations.
    /// </summary>
    internal class SecureRandom
    {
    }
}
