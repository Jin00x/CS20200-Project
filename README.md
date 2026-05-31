# F# Texas Hold'em Poker

A console-based Texas Hold'em poker game built with **F# / .NET 10** and **Spectre.Console** for terminal rendering.

Play heads-up or against up to 3 bots. Everyone starts with 10 tokens. Game runs for up to 5 rounds.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — verify with `dotnet --version` (should show `10.x.x`)

### Run

```bash
# Unix / macOS
chmod +x run.sh
./run.sh

# Windows
run.bat

# Or directly
dotnet run
```

### Build

```bash
dotnet build
```

---

## How to Play

1. Choose the number of bot opponents (1–3).
2. Each round, every player antes **1 token** into the pot.
3. Each player is dealt **2 private hole cards**. You can see yours; bots' cards are hidden.
4. **Pre-reveal betting**: Check, Call, Raise, or Fold before any community cards are shown.
5. **The Reveal + Turn**: The first 3 community cards (flop) and the 4th card (turn) are revealed immediately.
6. **Post-turn betting**: Another round of betting with 4 community cards visible.
7. **The River**: The 5th community card is revealed.
8. **Post-river betting**: Final betting round with all 5 community cards visible.
9. **Showdown**: All remaining players reveal their hole cards. Best 5-card hand from 7 (2 hole + 5 community) wins the pot.
10. If only one player remains at any point (all others folded), that player wins the pot immediately.

### Betting Actions

| Action | When available |
|--------|---------------|
| **Check** | No outstanding bet to call |
| **Call** | Match the current highest bet |
| **Raise** | Add extra tokens beyond the call (min 1) |
| **Fold** | Forfeit your contribution and drop out |

### Card Display

Cards show rank + suit symbol. Hearts (♥) and Diamonds (♦) appear in **red**; Spades (♠) and Clubs (♣) in default color.  
Unrevealed community cards are shown as `[ ? ]`.

### Hand Rankings (Highest to Lowest)

Royal Flush · Straight Flush · Four of a Kind · Full House · Flush · Straight · Three of a Kind · Two Pair · One Pair · High Card

### Bot Behavior

Bots compute a hand-strength score from their hole cards and visible community cards, then add random noise (σ ≈ ±0.18) to simulate bluffing and variance. Weak hands fold or check; medium hands call or check; strong hands raise by 2.

---

## Project Structure

```
CS20200-Project/
├── Poker.fsproj         # .NET 10 F# project (includes Spectre.Console)
├── run.sh               # Unix run script
├── run.bat              # Windows run script
├── README.md
├── requirements.md
├── plan.md
└── Poker/
    ├── Cards.fs         # Suit/Rank/Card types, deck creation, Fisher-Yates shuffle
    ├── Hand.fs          # Hand evaluation (best 5 from 7), hand name
    ├── Game.fs          # Texas Hold'em logic, betting rounds, bot AI, Spectre.Console display
    └── Program.fs       # Entry point, bot count prompt
```

---

## LLM Usage

This project was developed with assistance from **Claude** (Anthropic).

**What the LLM was used for**: Generating the F# implementation from the requirements document, design decisions, including the hand evaluation logic (best 5-card hand from 7 using combinatorial enumeration), the Texas Hold'em round flow (three betting stages, community card reveal), the bot AI hand-strength scoring, and the Spectre.Console display code.

**What required manual correction or reprompting**: 
The LLM initially implemented the game using only std output for display, without leveraging Spectre.Console's rich formatting capabilities. After correction, the display code was rewritten to use Spectre.Console's `Table`, `Panel`, and color features for a more engaging terminal UI.

The LLM didn't implement the game work flow correctly. Every new frame removed the bots' actions, results, bids from the previous frame. After correction, the game flow was structured into distinct stage.


**What the LLM could not do correctly initially**: The LLM initially failed to properly structure multi-stage conditional flow (pre-reveal → reveal → post-turn → river → post-river → showdown) with early exits in F#, generating syntax that the compiler rejected. The correct pattern like nested `match` arms where each `None ->` branch contains the entire remaining computation required explicit correction.
