using System;
using System.Collections.Generic;
using System.Text;

// File: PlayerAction.cs
// Purpose: Represents an immutable intent submitted by a player (action type, target amounts, metadata).
// Responsible for: Capturing user intent in a structured form for validation and processing.
// Not responsible for: Enforcing legality (ActionValidator) or applying changes to GameState (GameEngine handles mutations).
// Fit: Boundary DTO moving from external input layers into the rules/engine pipeline.

namespace PokerEngine.Rules
{
    /// <summary>
    /// Intent model describing what a player wishes to do on their turn; consumed by validators and the engine.
    /// </summary>
    internal class PlayerAction
    {
    }
}
