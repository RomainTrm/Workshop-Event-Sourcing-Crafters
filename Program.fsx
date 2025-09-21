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
// - Blog post : https://berthon.dev/posts/refining-software-architectures/
//
// ---------------------------------------------------------------------

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

type Commands = TurnOn | TurnOff

type State = 
    | Working of LightState * NbOfUseLeft
    | Broken
and LightState = On | Off
and NbOfUseLeft = int

type Events = 
    | TurnedOn
    | TurnedOff
    | Broke

let initialState : State = Working (Off, 3)

let evolve (state: State) (event: Events) : State = 
    match state, event with
    | Working (_, remaining), TurnedOff -> Working (Off, remaining)
    | Working (_, remaining), TurnedOn -> Working (On, remaining - 1)
    | _, Broke -> Broken
    | _ -> state

let decide (state: State) (cmd: Commands) : Events list = 
    match cmd, state with
    | TurnOn, Working (Off, 0) -> [Broke]
    | TurnOn, Working (Off, _) -> [TurnedOn]
    | TurnOff, Working (On, _) -> [TurnedOff]
    | _ -> []

let bulbDecider : Decider<Commands, Events, State> = {
    Decide = decide
    Evolve = evolve
    InitialState = initialState
}

// -------------------------
// Domain (imperative shell)
// -------------------------

type EventsStore<'Events> = {
    Load: unit -> 'Events list
    Save: 'Events list -> unit
}

let execute (decider: Decider<'Commands, 'Events, 'State>) (eventsStore: EventsStore<'Events>) (cmd: 'Commands) : unit =
    let history = eventsStore.Load () // Impure
    let state = List.fold decider.Evolve decider.InitialState history // Pure
    let events = decider.Decide state cmd // Pure
    eventsStore.Save events // Impure

// --------------
// Infrastructure 
// --------------

#r "nuget: FSharp.SystemTextJson,1.4.36"

open System.IO
open System.Text.Json
open System.Text.Json.Serialization

let jsonOptions = JsonFSharpOptions.Default().ToJsonSerializerOptions()

let deserializeState (json: string) : State = 
    match json with
    | "" -> Working (Off, 3)
    | json -> JsonSerializer.Deserialize<State> (json, jsonOptions)

let serializeState (state: State) : string = 
    JsonSerializer.Serialize (state, jsonOptions)

let loadState (filePath: string) : State = 
    File.ReadAllText filePath
    |> deserializeState

let saveState (filePath: string) (state: State) : unit =
    File.WriteAllText(filePath, serializeState state)

let deserializeEvents<'Events> (json: string) : 'Events =
    JsonSerializer.Deserialize<'Events> (json, jsonOptions)

let serializeEvent<'Events> (evt: 'Events) : string = 
    JsonSerializer.Serialize (evt, jsonOptions)

let loadEvents<'Events> (filePath: string) : 'Events list =
    File.ReadAllLines filePath
    |> Seq.map deserializeEvents<'Events>
    |> Seq.toList

let saveEvents<'Events> (filePath: string) (events: 'Events list) : unit =
    File.AppendAllLines(filePath, List.map serializeEvent<'Events> events)

// -----------
// Application
// -----------

let [<Literal>] StateFile = "State"
let [<Literal>] EventsFile = "Events"

let eventsStore : EventsStore<'Events> = {
    Load = fun () -> loadEvents<'Events> EventsFile
    Save = fun events -> saveEvents<'Events> EventsFile events
}

let turnOff () = execute bulbDecider eventsStore TurnOff
let turnOn () = execute bulbDecider eventsStore TurnOn
