open System
open Spectre.Console
open Game

[<EntryPoint>]
let main _ =
    AnsiConsole.Write(Rule("[bold yellow]=== F# Texas Hold'em Poker ===[/]"))
    AnsiConsole.MarkupLine "Each player starts with [yellow]10 tokens[/]. Game lasts at most [yellow]5 rounds[/]."
    AnsiConsole.MarkupLine "Ante: 1 token per round. Hands: Royal Flush > Straight Flush > Four of a Kind > Full House > Flush > Straight > Three of a Kind > Two Pair > One Pair > High Card."
    AnsiConsole.WriteLine()

    let numBots =
        AnsiConsole.Prompt(
            TextPrompt<int>("How many bot opponents? [grey](1–3)[/]: ")
                .Validate(fun n ->
                    if n >= 1 && n <= 3 then ValidationResult.Success()
                    else ValidationResult.Error("Enter 1, 2, or 3.")))

    runGame numBots
    0
