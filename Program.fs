// ---------------------------------------------------------------------
// Workshop adapté de celui de Jérémie Chassaing
//
// - Workshop d'origine : https://codeberg.org/thinkbeforecoding/es-workshop
// - Blog: https://thinkbeforecoding.com
// - Blog post sur le pattern `Decider`: https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider
//
// Romain Berthon
//
// - Blog post : https://berthon.dev/posts/from-a-state-based-to-an-event-sourced-codebase/
// ---------------------------------------------------------------------

open System

// ------------------------
// Domain DSL
// ------------------------

type Decider<'Commands, 'Events, 'State> = {
    Evolve: 'State -> 'Events -> 'State
    Decide: 'State -> 'Commands -> 'Events list
    InitialState: 'State
}

// ------------------------
// Domain (functional core)
// ------------------------

type Events = 
    | TurnedOn
    | TurnedOff
    | Broke

type Commands = 
    | TurnOn 
    | TurnOff

type State = 
    | Working of LightState * NbOfUseLeft
    | Broken
and LightState =
    | On
    | Off
and NbOfUseLeft = int

let initialState = Working (Off, 3)

let evolve (state: State) (event: Events) = 
    match state, event with
    | Working (_, remaining), TurnedOff -> Working (Off, remaining)
    | Working (_, remaining), TurnedOn -> Working (On, remaining - 1)
    | _, Broke -> Broken
    | _ -> state

let decide (state: State) (cmd: Commands) = 
    match cmd, state with
    | _, Broken -> []
    | TurnOn, Working (On, _) -> []
    | TurnOn, Working (Off, 0) -> [Broke]
    | TurnOn, Working (Off, _) -> [TurnedOn]
    | TurnOff, Working (On, _) -> [TurnedOff]
    | TurnOff, Working (Off, _) -> []

let bulbDecider : Decider<Commands, Events, State> = {
    Decide = decide
    Evolve = evolve
    InitialState = initialState
}

// -------------------------
// Domain (imperative shell)
// -------------------------

type EventStore<'Events> = {
    Load: unit -> 'Events list
    Save: 'Events list -> unit
}

let execute (decider: Decider<'Commands, 'Events, _>) (eventStore: EventStore<'Events>) (cmd: 'Commands) =
    let history = eventStore.Load ()
    let state = List.fold decider.Evolve decider.InitialState history
    let newEvents = decider.Decide state cmd
    eventStore.Save newEvents

// --------------
// Infrastructure 
// --------------

let deserializeEvent (evt: string) : Events list = 
    match evt with
    | "TurnedOn" -> [TurnedOn]
    | "TurnedOff" -> [TurnedOff]
    | "Broke" -> [Broke]
    | _ -> []

let serializeEvent (evt: Events) : string = 
    match evt with
    | TurnedOn -> "TurnedOn"
    | TurnedOff -> "TurnedOff"
    | Broke -> "Broke"

let loadEvents filePath = 
    IO.File.ReadAllLines filePath
    |> Seq.collect deserializeEvent
    |> Seq.toList

let saveEvents filePath events =
    IO.File.AppendAllLines(filePath, List.map serializeEvent events)

let [<Literal>] EventsFile = "Events"

let eventStore : EventStore<_> = {
    Load = fun () -> loadEvents EventsFile
    Save = saveEvents EventsFile
}

// -----------
// Application
// -----------

[<EntryPoint>]
let main argv =
    match argv with
    | [| "TurnOn" |] -> execute bulbDecider eventStore TurnOn
    | [| "TurnOff" |] -> execute bulbDecider eventStore TurnOff
    | _ -> ()
    0
