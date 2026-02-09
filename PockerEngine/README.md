## PokerEngine

Backend-only Texas Hold'em engine focused on determinism, security, and clean separation of concerns. This repository holds the core game logic (no UI, no networking) that can be embedded in services or consumed by clients such as Unity without leaking presentation concerns into the domain model.

### Goals
- Correct Texas Hold'em rules and betting flow across multiple hands per session.
- Deterministic, testable core with GameState as the single source of truth.
- Cryptographically secure randomness isolated in RNG services.
- Clear boundaries: rules validate intent, engine mutates state, observers consume outputs.

### Architecture
- **Pure domain core**: No rendering, input handling, or network code. All mutations pass through the engine and state aggregates.
- **Single responsibility**: Each file/class owns one concern (cards, state aggregates, rule checks, orchestration, randomness, observation).
- **State first**: GameState captures authoritative data for the current session and round. Mutations are explicit and validated.
- **Rules validate, engine acts**: Rules enforce legal moves; engine applies state changes only after validation.
- **Randomness centralized**: SecureRandom and ShuffleService isolate entropy; no other class should generate randomness.

### Folder Guide
- **Core/**: Immutable domain primitives (cards, decks, players) that model poker concepts without side effects.
- **State/**: Aggregates describing current game/round/phase. GameState is the system of record.
- **Rules/**: Validation and classification (legal actions, hand evaluation wrappers, action types, PlayerAction intent objects).
- **Engine/**: Orchestration of game flow (dealing, betting rounds, turn order, pot handling). Uses rules to validate before mutating state.
- **RNG/**: Entropy providers (cryptographically secure) and shuffling utilities. Only entry point for randomness.
- **Interfaces/**: Contracts for observers to receive domain events without coupling to UI or transport layers.

### Game Flow (One Hand)
1. **Initialize**: Build GameState with players, blinds, and a seeded deck via RNG services.
2. **Pre-Flop**: TurnManager enforces order; ActionValidator checks each PlayerAction; GameEngine applies accepted actions and updates GameState.
3. **Flop/Turn/River**: RoundManager advances phases; cards revealed from Deck; betting rounds repeat with validation and pot updates.
4. **Showdown**: HandEvaluatorWrapper classifies hands; PotManager distributes winnings according to betting history and hand strength.
5. **Reset for next hand**: RoundState and GameState prepare for subsequent hands while retaining session-level context (e.g., seating, stacks).

### Data Flow
- **Input**: PlayerAction represents intent (fold/call/raise/check/bet) tied to the acting player and amount when applicable.
- **Validation**: Rules layer (ActionValidator + ActionType) checks legality against current GameState and RoundState.
- **Mutation**: GameEngine coordinates TurnManager, RoundManager, and PotManager to apply validated changes to GameState.
- **Observation**: IGameObserver implementations receive state changes/events for logging, analytics, or UI integration without mutating state.

### Design Decisions and Constraints
- Determinism where randomness is seeded and isolated; all other logic is pure and testable.
- No direct UI/network calls; all integrations consume events or read state.
- GameState is authoritative; avoid shadow copies of state in consumers.
- Explicit phases (GamePhase) to prevent illegal transitions and to simplify rule enforcement.
- Action validation precedes mutation to maintain invariants and avoid partial updates.

### Integration (e.g., Unity Client)
- Treat this engine as a headless library: feed PlayerAction objects from client input or network messages.
- Subscribe to observer interfaces to mirror state into UI layers or relay events over the network.
- Do not allow UI to mutate GameState directly; all changes must go through GameEngine with validated actions.

### Out of Scope
- Rendering, MonoBehaviour, scenes, or any Unity-specific APIs.
- Networking, matchmaking, lobbies, or persistence.
- User authentication, economy systems, or anti-cheat beyond deterministic logs and secure randomness.

### Extending Safely
- Add new behavior by extending rules/engine while keeping single responsibility per class.
- Introduce new actions only via PlayerAction and validate through ActionValidator before touching GameState.
- Keep randomness confined to RNG services; inject seeds for reproducible tests.
- Maintain clear phase transitions in RoundManager and keep PotManager as the single authority on wagers and pots.
