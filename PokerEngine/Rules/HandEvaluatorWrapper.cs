using System;
using System.Collections.Generic;
using System.Text;

// File: HandEvaluatorWrapper.cs
// Purpose: Bridges the engine to a hand-evaluation component for ranking poker hands.
// Responsible for: Delegating to an evaluator to score hands, encapsulating external dependencies behind a stable interface.
// Not responsible for: Managing pots, determining winners based on betting context, or mutating GameState.
// Fit: Supplies PotManager and GameEngine with ranked outcomes during showdown without leaking evaluator details.

namespace PokerEngine.Rules
{
    /// <summary>
    /// Adapter layer for hand evaluation to keep the rules engine decoupled from specific evaluator implementations.
    /// </summary>
    internal class HandEvaluatorWrapper
    {
    }
}
