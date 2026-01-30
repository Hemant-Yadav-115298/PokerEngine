using System;
using System.Collections.Generic;
using System.Text;

// File: ActionValidator.cs
// Purpose: Enforces Texas Hold'em betting rules and verifies whether a PlayerAction is legal in the current state.
// Responsible for: Checking turn order, stack sufficiency, bet sizing rules, and phase-specific constraints before mutations occur.
// Not responsible for: Executing actions (GameEngine handles mutations) or managing pots (PotManager) once validated.
// Fit: Gatekeeper between external intent (PlayerAction) and internal state changes to preserve invariants.

namespace PokerEngine.Rules
{
    /// <summary>
    /// Validates player intents against GameState and RoundState to prevent illegal or contradictory actions.
    /// </summary>
    internal class ActionValidator
    {
    }
}
