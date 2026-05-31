**Project Title**: F# Texas Hold'em Poker

**Overview**: A console-based single-player Texas Hold'em poker game where the user plays against 1–3 computer-controlled bots. The implementation is written in F# targeting .NET 10 and uses the Spectre.Console NuGet package for terminal rendering.

**Requirements**

*Game Setup*

1. When the program starts, the user is asked to choose the number of bot opponents. The user must enter an integer between 1 and 3 inclusive. If the input is invalid (out of range or non-numeric), the program prompts again.
2. The user starts the game with 10 tokens. Each bot also starts with 10 tokens.
3. The game lasts at most 5 rounds. It ends early if either the player's token count reaches 0, or all bots' token counts reach 0.

*Each Round*

4. At the start of a round, every participating player (anyone with at least 1 token) contributes 1 token to the pot as an ante. Players with 0 tokens are eliminated and do not play in this or subsequent rounds.
5. A standard 52-card deck is freshly shuffled at the start of each round.
6. Each participating player is dealt 2 private hole cards. The user can see their own hole cards. The bots' hole cards remain hidden from the user until showdown.
7. Five community cards are placed face-down on the board. They are revealed in stages:
   - The reveal: the first 3 cards (flop) are immediately revealed after the pre-reveal betting round.
   - The turn: the 4th community card is revealed immediately after the flop.
   - The river: the 5th community card is revealed after the post-turn betting round.
8. There are exactly three betting rounds per round: pre-reveal, post-turn, and post-river.

*Betting*

9. In each betting round, active players (those who have not folded) take actions in turn order. The user acts first; bots act in order (Bot 1, Bot 2, …).
10. On their turn, a player chooses exactly one of the following actions:
    - Fold: drop out of the round. Tokens already contributed are forfeited.
    - Check: pass without betting. Only valid when there is no outstanding bet to call.
    - Call: match the current highest bet.
    - Raise: add tokens beyond a call to increase the current bet. The minimum raise amount is 1 token. A player cannot raise more tokens than they currently have.
11. A betting round ends when every active player has either folded or matched the current highest bet.
12. If at any point only one active player remains (all others have folded), that player immediately wins the pot and the round ends without a showdown.

*Showdown*

13. If two or more players remain after the post-river betting round, a showdown occurs. All remaining players' hole cards are revealed.
14. For each remaining player, the best 5-card hand is selected from the 7 available cards (2 hole cards + 5 community cards).
15. Standard poker hand rankings (highest to lowest): Royal Flush, Straight Flush, Four of a Kind, Full House, Flush, Straight, Three of a Kind, Two Pair, One Pair, High Card. Standard kicker rules break ties.
16. The player with the best hand wins the entire pot. If two or more players tie, the pot is divided equally; any remainder goes to the player closest to the user in turn order (user first, then Bot 1, Bot 2, …).

*Bot Behavior*

17. Each bot computes a hand-strength score s (0.0–1.0) from its hole cards and any visible community cards.
18. Given the score s and a σ noise perturbation:
    - If s < 0.3: fold. If checking is free, check instead.
    - If 0.3 ≤ s < 0.7: call. If checking is free, check instead.
    - If s ≥ 0.7: raise by 2 tokens. If fewer than 2 extra tokens are available, call instead.
    - σ is a uniform noise parameter (±0.18) applied to s, simulating bluffing and variance in bot decisions.

*User Interface*

19. The terminal display (rendered using Spectre.Console) shows on each user turn:
    - The current round number (e.g., "Round 1 of 5").
    - The current pot size.
    - Each player's remaining tokens.
    - The current highest bet for the active betting round.
    - The community cards (unrevealed cards shown as face-down placeholders).
    - The user's two hole cards.
    - Each player's status: active, folded, or all-in.
20. Cards are rendered with their rank (2–10, J, Q, K, A) and suit (♠ ♥ ♦ ♣). Hearts and diamonds are displayed in red; spades and clubs are displayed in the default terminal color.
21. The user chooses actions through an interactive selection prompt (Spectre.Console SelectionPrompt) listing the legal actions for the current turn.

*Game End*

22. The game ends as soon as any of the following is true: 5 rounds have been completed, the player's token count reaches 0, or all bots' token counts reach 0.
23. When the game ends, the program displays the final token count of every player and announces the player(s) with the most tokens as the winner(s).
