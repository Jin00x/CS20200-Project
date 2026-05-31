module Hand

open Cards

// Returns comparable int list; first element = category 0-8.
// 8=Straight Flush, 7=Four of a Kind, 6=Full House, 5=Flush,
// 4=Straight, 3=Three of a Kind, 2=Two Pair, 1=One Pair, 0=High Card
let evaluate (hand: Card array) : int list =
    let vals     = hand |> Array.map (fun c -> rankValue c.Rank)
    let sortAsc  = vals |> Array.sort
    let sortDesc = sortAsc |> Array.rev |> Array.toList
    let suits    = hand |> Array.map (fun c -> c.Suit)
    let isFlush  = suits |> Array.forall (fun s -> s = suits.[0])
    let isStraight =
        sortAsc.[4] - sortAsc.[0] = 4 &&
        sortAsc |> Array.pairwise |> Array.forall (fun (a,b) -> b-a=1)
    let isWheel =
        sortAsc.[0]=2 && sortAsc.[1]=3 && sortAsc.[2]=4 &&
        sortAsc.[3]=5 && sortAsc.[4]=14
    let groups =
        vals |> Array.groupBy id
             |> Array.map (fun (v,a) -> a.Length, v)
             |> Array.sortByDescending id
    let counts = groups |> Array.map fst
    let gvals  = groups |> Array.map snd |> Array.toList
    if   isFlush && isStraight then [8; List.head sortDesc]
    elif isFlush && isWheel    then [8; 5]
    else
    match counts with
    | [|4;1|] -> 7 :: gvals
    | [|3;2|] -> 6 :: gvals
    | _ ->
        if   isFlush    then 5 :: sortDesc
        elif isStraight then [4; List.head sortDesc]
        elif isWheel    then [4; 5]
        else
        match counts with
        | [|3;1;1|]    -> 3 :: gvals
        | [|2;2;1|]    -> 2 :: gvals
        | [|2;1;1;1|]  -> 1 :: gvals
        | _            -> 0 :: sortDesc

let handName (score: int list) =
    match List.head score with
    | 8 -> if score.[1] = 14 then "Royal Flush" else "Straight Flush"
    | 7 -> "Four of a Kind"
    | 6 -> "Full House"
    | 5 -> "Flush"
    | 4 -> "Straight"
    | 3 -> "Three of a Kind"
    | 2 -> "Two Pair"
    | 1 -> "One Pair"
    | _ -> "High Card"

// All k-element subsets of a list
let rec private combinations k lst =
    match k, lst with
    | 0, _      -> [[]]
    | _, []     -> []
    | k, x::xs  -> List.map (fun r -> x::r) (combinations (k-1) xs) @ combinations k xs

// Best 5-card score from any array of >=5 cards (e.g. 6 or 7)
let bestFiveFrom (cards: Card array) : int list =
    cards |> Array.toList
          |> combinations 5
          |> List.map (fun c -> evaluate (List.toArray c))
          |> List.max
