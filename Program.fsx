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

// -------------------------
// Domain (imperative shell)
// -------------------------

type Repository = {
    Load: unit -> Events list
    Save: State * Events list -> unit
}

let execute (repository: Repository) (cmd: Commands) : unit =
    let history = repository.Load () // Impure
    let state = List.fold evolve initialState history // Pure
    let events = decide state cmd // Pure
    let newState = List.fold evolve state events // Pure
    repository.Save (newState, events) // Impure

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

let deserializeEvents (json: string) : Events =
    JsonSerializer.Deserialize<Events> (json, jsonOptions)

let serializeEvent (evt: Events) : string = 
    JsonSerializer.Serialize (evt, jsonOptions)

let loadEvents (filePath: string) : Events list =
    File.ReadAllLines filePath
    |> Seq.map deserializeEvents
    |> Seq.toList

let saveEvents (filePath: string) (events: Events list) : unit =
    File.AppendAllLines(filePath, List.map serializeEvent events)

// -----------
// Application
// -----------

let [<Literal>] StateFile = "State"
let [<Literal>] EventsFile = "Events"

let repository : Repository = {
    Load = fun () -> loadEvents EventsFile
    Save = fun (state, events) -> 
        saveState StateFile state
        saveEvents EventsFile events
}

let turnOff () = execute repository TurnOff
let turnOn () = execute repository TurnOn
