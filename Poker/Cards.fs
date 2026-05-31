module Cards

open System

type Suit = Spades | Hearts | Diamonds | Clubs
type Rank = Two|Three|Four|Five|Six|Seven|Eight|Nine|Ten|Jack|Queen|King|Ace

type Card = { Rank: Rank; Suit: Suit }

let rankValue = function
    | Two->2|Three->3|Four->4|Five->5|Six->6|Seven->7|Eight->8|Nine->9|Ten->10
    | Jack->11|Queen->12|King->13|Ace->14

let rankStr = function
    | Two->"2"|Three->"3"|Four->"4"|Five->"5"|Six->"6"|Seven->"7"|Eight->"8"
    | Nine->"9"|Ten->"10"|Jack->"J"|Queen->"Q"|King->"K"|Ace->"A"

let suitSymbol = function
    | Spades->"♠"|Hearts->"♥"|Diamonds->"♦"|Clubs->"♣"

// Spectre markup: hearts/diamonds red, spades/clubs default color
let cardMarkup (c: Card) =
    let sym = suitSymbol c.Suit
    let r   = rankStr c.Rank
    match c.Suit with
    | Hearts | Diamonds -> sprintf "[red]%s%s[/]" r sym
    | _                 -> sprintf "%s%s" r sym

let cardStr c = rankStr c.Rank + suitSymbol c.Suit

let createDeck () : Card array =
    [| for s in [|Spades;Hearts;Diamonds;Clubs|] do
        for r in [|Two;Three;Four;Five;Six;Seven;Eight;Nine;Ten;Jack;Queen;King;Ace|] do
            yield { Rank = r; Suit = s } |]

let private rng = Random()

let shuffle (deck: Card array) : Card array =
    let a = Array.copy deck
    for i = a.Length - 1 downto 1 do
        let j = rng.Next(i + 1)
        let t = a.[i] in a.[i] <- a.[j]; a.[j] <- t
    a
