module Game

open System
open Spectre.Console
open Cards
open Hand

// ── Types ──────────────────────────────────────────────────────────────────

type Action = Fold | Check | Call | Raise of int

type Player = {
    Name     : string
    IsUser   : bool
    Chips    : int
    Hole     : Card array
    Folded   : bool
    Bet      : int          // tokens bet in current sub-round
    RoundBet : int          // total tokens committed this round (all stages)
}

type RoundState = {
    RoundNum   : int
    TotalRounds: int
    Pot        : int
    Community  : Card array
    Players    : Player array
    Deck       : Card array
    CurrentBet : int
    ActionLog  : string list    // actions since user's last turn
    BotTaunt   : string option  // trash talk line shown while user picks action
}

// ── Card Box Rendering ──────────────────────────────────────────────────────

let private cardLines (c: Card) : string array =
    let r   = rankStr c.Rank
    let s   = suitSymbol c.Suit
    let col = match c.Suit with Hearts | Diamonds -> "red" | _ -> "white"
    let w l = sprintf "[%s]%s[/]" col l
    [| w "┌─────┐"
       w (sprintf "│%-5s│" r)
       w (sprintf "│  %s  │" s)
       w (sprintf "│%5s│" r)
       w "└─────┘" |]

let private hiddenLines : string array =
    [| "[grey]┌─────┐[/]"
       "[grey]│▓▓▓▓▓│[/]"
       "[grey]│▓▓▓▓▓│[/]"
       "[grey]│▓▓▓▓▓│[/]"
       "[grey]└─────┘[/]" |]

let private printCardRow (rows: string array array) =
    for i in 0 .. 4 do
        rows |> Array.map (fun r -> r.[i]) |> String.concat "  " |> AnsiConsole.MarkupLine

let private showCards (cards: Card array) =
    cards |> Array.map cardLines |> printCardRow

let private showCommunity (comm: Card array) =
    let revealed = comm |> Array.map cardLines
    let hidden   = Array.replicate (5 - comm.Length) hiddenLines
    printCardRow (Array.append revealed hidden)

// ── Player table ───────────────────────────────────────────────────────────

let private playerStatus (p: Player) =
    if p.Chips = 0 && not p.Folded then "[grey]All-in[/]"
    elif p.Folded then "[red]Folded[/]"
    else "[green]Active[/]"

let private actionDesc (p: Player) (a: Action) (prevBet: int) =
    let amt = min (prevBet - p.Bet) p.Chips
    match a with
    | Fold    -> if p.IsUser then "[red]You fold[/]"             else sprintf "[red]%s folds[/]"      p.Name
    | Check   -> if p.IsUser then "[grey]You check[/]"           else sprintf "[grey]%s checks[/]"    p.Name
    | Call    -> if p.IsUser then sprintf "[white]You call %d[/]" amt else sprintf "[white]%s calls %d[/]" p.Name amt
    | Raise n -> if p.IsUser then sprintf "[yellow]You raise by %d[/]" n else sprintf "[yellow]%s raises by %d[/]" p.Name n

// Clear screen + draw full game state + optional action log + bot taunt
let showState (rs: RoundState) =
    AnsiConsole.Clear()
    let stage = match rs.Community.Length with
                | 0 -> "Pre-Flop" | 3 -> "Flop" | 4 -> "Turn" | _ -> "River"
    let title = sprintf "[yellow]Round %d of %d[/]  [grey]%s[/]  Pot: [yellow]%d[/]  Bet: [yellow]%d[/]" rs.RoundNum rs.TotalRounds stage rs.Pot rs.CurrentBet
    AnsiConsole.Write(Rule(title))
    let tbl = Table().AddColumn("Player").AddColumn("Chips").AddColumn("Status").AddColumn("Bet").AddColumn("In Pot")
    for p in rs.Players do
        let name = if p.IsUser then "[cyan]You[/]" else p.Name
        tbl.AddRow(name, string p.Chips, playerStatus p, string p.Bet, string p.RoundBet) |> ignore
    AnsiConsole.Write(tbl)
    AnsiConsole.MarkupLine "[bold]Community cards:[/]"
    showCommunity rs.Community
    let user = rs.Players.[0]
    if user.Hole.Length = 2 then
        AnsiConsole.MarkupLine "[bold]Your hole cards:[/]"
        showCards user.Hole
    if not rs.ActionLog.IsEmpty then
        AnsiConsole.WriteLine()
        AnsiConsole.MarkupLine "[bold]What happened:[/]"
        for entry in rs.ActionLog do
            AnsiConsole.MarkupLine(sprintf "  %s" entry)
    match rs.BotTaunt with
    | Some t -> AnsiConsole.MarkupLine(sprintf "\n%s" t)
    | None   -> ()
    AnsiConsole.WriteLine()

// ── Bot AI ─────────────────────────────────────────────────────────────────

let private rng = Random()

let private handStrength (hole: Card array) (community: Card array) : float =
    let all = Array.append hole community
    match all.Length with
    | n when n >= 5 -> float (List.head (bestFiveFrom all)) / 8.0
    | 3 | 4 ->
        all |> Array.averageBy (fun c -> float (rankValue c.Rank)) |> fun avg -> avg / 14.0 * 0.6
    | _ ->
        let r0, r1 = rankValue hole.[0].Rank, rankValue hole.[1].Rank
        let b = float (max r0 r1) / 14.0 * 0.5
        b + (if r0 = r1 then 0.3 else 0.0) + (if hole.[0].Suit = hole.[1].Suit then 0.1 else 0.0)
        |> min 1.0

let private botDecide (p: Player) (community: Card array) (currentBet: int) : Action =
    let s = (handStrength p.Hole community + (rng.NextDouble() - 0.5) * 0.36) |> max 0.0 |> min 1.0
    let toCall   = currentBet - p.Bet
    let canCheck = toCall = 0
    if s < 0.3 then
        if canCheck then Check else Fold
    elif s < 0.7 then
        if canCheck then Check else Call
    else
        if p.Chips >= toCall + 2 then Raise 2
        elif canCheck then Check
        else Call

// ── User Input ─────────────────────────────────────────────────────────────

let private legalActions (p: Player) (currentBet: int) : string list =
    let toCall   = currentBet - p.Bet
    let canCheck = toCall = 0
    [ if canCheck then "Check" else "Call"
      if p.Chips > toCall then "Raise"
      "Fold" ]

let private getUserAction (rs: RoundState) : Action =
    let p = rs.Players.[0]
    let choice =
        AnsiConsole.Prompt(
            SelectionPrompt<string>()
                .Title("[cyan]Your action:[/]")
                .AddChoices(legalActions p rs.CurrentBet))
    match choice with
    | "Check" -> Check
    | "Call"  -> Call
    | "Fold"  -> Fold
    | _ ->
        let maxRaise = p.Chips - (rs.CurrentBet - p.Bet)
        let amount =
            AnsiConsole.Prompt(
                TextPrompt<int>(sprintf "[cyan]Raise by (1–%d):[/] " maxRaise)
                    .Validate(fun n ->
                        if n >= 1 && n <= maxRaise then ValidationResult.Success()
                        else ValidationResult.Error(sprintf "Enter 1–%d." maxRaise)))
        Raise amount

// ── Betting Round ──────────────────────────────────────────────────────────

let private applyAction (players: Player array) (pot: int ref) (currentBet: int ref) (i: int) (a: Action) =
    let p = players.[i]
    match a with
    | Fold  -> players.[i] <- { p with Folded = true }
    | Check -> ()
    | Call  ->
        let toCall = min (!currentBet - p.Bet) p.Chips
        players.[i] <- { p with Chips = p.Chips - toCall; Bet = p.Bet + toCall; RoundBet = p.RoundBet + toCall }
        pot.Value <- !pot + toCall
    | Raise amt ->
        let toCall = !currentBet - p.Bet
        let total  = min (toCall + amt) p.Chips
        players.[i] <- { p with Chips = p.Chips - total; Bet = p.Bet + total; RoundBet = p.RoundBet + total }
        pot.Value        <- !pot + total
        currentBet.Value <- !currentBet + (if total > toCall then amt else 0)

let private countActive (players: Player array) =
    players |> Array.filter (fun p -> not p.Folded) |> Array.length

let private tauntLines = [|
    "Are you bluffing? I can smell it from here."
    "I've got the best hand at this table. Just give up."
    "Let's see if you have the courage to call!"
    "This pot is already mine — you just don't know it yet."
    "Feeling lucky? You really shouldn't be."
    "I'm not even sweating right now. Are you?"
    "Bold move. Too bad it won't work out for you."
    "My hand is so good it should be illegal."
    "Go ahead and fold. Save yourself the embarrassment."
    "You're playing with fire and you know it."
    "I've seen better bluffs from a toddler."
    "Raise me if you dare. I double dare you."
    "You call that a poker face? Ha!"
    "I could win this blindfolded."
|]

let private pickTaunt (players: Player array) : string option =
    let bots = players |> Array.filter (fun p -> not p.IsUser && not p.Folded)
    if bots.Length = 0 || rng.NextDouble() > 0.65 then None
    else
        let bot = bots.[rng.Next(bots.Length)]
        let line = tauntLines.[rng.Next(tauntLines.Length)]
        Some (sprintf "[bold yellow]%s:[/] [italic]\"%s\"[/]" bot.Name line)

let private runBettingRound (rs: RoundState) : RoundState =
    let players    = rs.Players |> Array.map (fun p -> { p with Bet = 0 })  // reset sub-round Bet only
    let pot        = ref rs.Pot
    let currentBet = ref 0
    let log        = ref rs.ActionLog          // carry previous round's log into first user view

    let act (i: int) =
        let p = players.[i]
        if p.Folded || p.Chips = 0 then ()
        else
            let prevBet = !currentBet
            let action =
                if p.IsUser then
                    let taunt = pickTaunt players
                    let st = { rs with Players = Array.copy players; Pot = !pot; CurrentBet = !currentBet; ActionLog = !log; BotTaunt = taunt }
                    showState st
                    log := []    // reset log after user sees it
                    getUserAction st
                else
                    botDecide p rs.Community !currentBet
            applyAction players pot currentBet i action
            // Record this action for display at the next user turn
            log := !log @ [actionDesc p action prevBet]

    // First pass
    for i in 0 .. players.Length - 1 do act i

    // Extra passes if someone raised and others haven't matched
    let mutable safety = 0
    let mutable settled = false
    while not settled && safety < 4 do
        safety <- safety + 1
        let unsettled = players |> Array.exists (fun p -> not p.Folded && p.Chips > 0 && p.Bet < !currentBet)
        if not unsettled then settled <- true
        else
            for i in 0 .. players.Length - 1 do
                let p = players.[i]
                if not p.Folded && p.Chips > 0 && p.Bet < !currentBet then act i

    { rs with Players = players; Pot = !pot; CurrentBet = !currentBet; ActionLog = !log }

// ── Early-win helper ────────────────────────────────────────────────────────

let private checkEarlyWin (s: RoundState) : Player array option =
    if countActive s.Players = 1 then
        let winner = s.Players |> Array.find (fun p -> not p.Folded)
        let wName  = if winner.IsUser then "You" else winner.Name
        let updPlayers = s.Players |> Array.map (fun p ->
            if p.Name = winner.Name then { p with Chips = p.Chips + s.Pot } else p)
        AnsiConsole.Clear()
        let title = sprintf "[yellow]Round %d of %d[/]  —  [bold green]Round Over[/]" s.RoundNum s.TotalRounds
        AnsiConsole.Write(Rule(title))
        let tbl = Table().AddColumn("Player").AddColumn("Chips").AddColumn("Status").AddColumn("In Pot")
        for p in updPlayers do
            let name = if p.IsUser then "[cyan]You[/]" else p.Name
            tbl.AddRow(name, string p.Chips, playerStatus p, string p.RoundBet) |> ignore
        AnsiConsole.Write(tbl)
        AnsiConsole.MarkupLine "[bold]Community cards:[/]"
        showCommunity s.Community
        AnsiConsole.WriteLine()
        AnsiConsole.MarkupLine(sprintf "[bold green]★  %s won the pot of %d chips!  (all others folded)  ★[/]" wName s.Pot)
        AnsiConsole.MarkupLine "\n[grey](Press Enter for next round...)[/]"
        Console.ReadLine() |> ignore
        Some updPlayers
    else None

// ── One Full Round ─────────────────────────────────────────────────────────

let playRound (players: Player array) (roundNum: int) (totalRounds: int) : Player array =
    let ante = 1
    let mutable pot = 0
    let withAntes =
        players |> Array.map (fun p ->
            if p.Chips > 0 then
                pot <- pot + ante
                { p with Chips = p.Chips - ante; Folded = false; Bet = 0; RoundBet = ante; Hole = [||] }
            else { p with Folded = true; Bet = 0; RoundBet = 0; Hole = [||] })

    let deck = shuffle (createDeck ())
    let mutable di = 0
    let dealt =
        withAntes |> Array.map (fun p ->
            if not p.Folded then
                let h = deck.[di..di+1]
                di <- di + 2
                { p with Hole = h }
            else p)
    let remaining = deck.[di..]

    let roundTitle = sprintf "[yellow]Round %d of %d[/] — ante %d each — pot [yellow]%d[/]" roundNum totalRounds ante pot
    AnsiConsole.Write(Rule(roundTitle))

    let init = { RoundNum = roundNum; TotalRounds = totalRounds; Pot = pot
                 Community = [||]; Players = dealt; Deck = remaining; CurrentBet = 0; ActionLog = []; BotTaunt = None }

    // ── Pre-flop betting ──────────────────────────────────────────────────
    let s1 = runBettingRound init
    match checkEarlyWin s1 with
    | Some upd -> upd
    | None ->

    // ── Flop (3 cards) → post-flop betting ───────────────────────────────
    let flop  = s1.Deck.[0..2]
    let deck1 = s1.Deck.[3..]
    let sFlop = { s1 with Community = flop; Deck = deck1; CurrentBet = 0; ActionLog = s1.ActionLog }
    let s2    = runBettingRound sFlop
    match checkEarlyWin s2 with
    | Some upd -> upd
    | None ->

    // ── Turn (4th card) → post-turn betting ──────────────────────────────
    let turn  = s2.Deck.[0]
    let comm4 = Array.append s2.Community [| turn |]
    let deck2 = s2.Deck.[1..]
    let sTurn = { s2 with Community = comm4; Deck = deck2; CurrentBet = 0; ActionLog = s2.ActionLog }
    let s3    = runBettingRound sTurn
    match checkEarlyWin s3 with
    | Some upd -> upd
    | None ->

    // ── River (5th card) → post-river betting ────────────────────────────
    let river  = s3.Deck.[0]
    let comm5  = Array.append s3.Community [| river |]
    let deck3  = s3.Deck.[1..]
    let sRiver = { s3 with Community = comm5; Deck = deck3; CurrentBet = 0; ActionLog = s3.ActionLog }
    let s4     = runBettingRound sRiver
    match checkEarlyWin s4 with
    | Some upd -> upd
    | None ->

    // ── Showdown ──────────────────────────────────────────────────────────
    AnsiConsole.Clear()
    AnsiConsole.Write(Rule("[yellow]Showdown[/]"))
    AnsiConsole.MarkupLine "[bold]Community cards:[/]"
    showCommunity s4.Community
    AnsiConsole.WriteLine()

    let remaining2 = s4.Players |> Array.filter (fun p -> not p.Folded)
    let scored     = remaining2 |> Array.map (fun p -> p, bestFiveFrom (Array.append p.Hole s4.Community))

    for (p, sc) in scored do
        let nStr = if p.IsUser then "[cyan]You[/]" else p.Name
        AnsiConsole.MarkupLine(sprintf "%s  →  [bold]%s[/]" nStr (handName sc))
        showCards p.Hole

    let bestScore  = scored |> Array.map snd |> Array.max
    let winners    = scored |> Array.filter (fun (_,sc) -> sc = bestScore) |> Array.map fst
    let share      = s4.Pot / winners.Length
    let remainder  = s4.Pot % winners.Length
    let winSet     = winners |> Array.map (fun p -> p.Name) |> Set.ofArray
    let mutable extra = remainder
    let finalPlayers =
        s4.Players |> Array.map (fun p ->
            if Set.contains p.Name winSet then
                let bonus = if extra > 0 then (extra <- extra - 1; 1) else 0
                { p with Chips = p.Chips + share + bonus }
            else p)

    let wStr = winners |> Array.map (fun p -> if p.IsUser then "You" else p.Name) |> String.concat ", "
    AnsiConsole.MarkupLine(sprintf "[green]Winner(s): %s — pot of %d.[/]" wStr s4.Pot)
    Console.ReadLine() |> ignore
    finalPlayers

// ── Full Game ──────────────────────────────────────────────────────────────

let runGame (numBots: int) =
    let user = { Name="You"; IsUser=true; Chips=10; Hole=[||]; Folded=false; Bet=0; RoundBet=0 }
    let bots = Array.init numBots (fun i -> { Name=sprintf "Bot %d" (i+1); IsUser=false; Chips=10; Hole=[||]; Folded=false; Bet=0; RoundBet=0 })
    let mutable players = Array.append [|user|] bots
    let total = 5

    let mutable round = 1
    let mutable over  = false
    while not over && round <= total do
        players <- playRound players round total
        let userAlive = players.[0].Chips > 0
        let botsAlive = players |> Array.skip 1 |> Array.exists (fun p -> p.Chips > 0)
        if not userAlive || not botsAlive then over <- true
        else round <- round + 1

    AnsiConsole.Clear()
    AnsiConsole.Write(Rule("[bold yellow]Game Over[/]"))
    let tbl = Table().AddColumn("Player").AddColumn("Tokens")
    for p in players do
        tbl.AddRow((if p.IsUser then "[cyan]You[/]" else p.Name), string p.Chips) |> ignore
    AnsiConsole.Write(tbl)
    let best  = players |> Array.map (fun p -> p.Chips) |> Array.max
    let gw    = players |> Array.filter (fun p -> p.Chips = best)
    let gwStr = gw |> Array.map (fun p -> if p.IsUser then "You" else p.Name) |> String.concat ", "
    AnsiConsole.MarkupLine(sprintf "[bold green]%s win(s) the game with %d tokens![/]" gwStr best)
