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

let decide (state: State) (cmd: Commands) : State = 
    match cmd, state with
    | TurnOn, Working (Off, 0) -> Broken
    | TurnOn, Working (Off, nbOfUseRemaining) -> Working (On, nbOfUseRemaining - 1)
    | TurnOff, Working (On, nbOfUseRemaining) -> Working (Off, nbOfUseRemaining)
    | _ -> state

// -------------------------
// Domain (imperative shell)
// -------------------------

type Repository = {
    Load: unit -> State
    Save: State -> unit
}

let execute (repository: Repository) (cmd: Commands) : unit =
    let state = repository.Load ()
    let newState = decide state cmd
    repository.Save newState

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

// -----------
// Application
// -----------

let [<Literal>] StateFile = "State"
let [<Literal>] EventsFile = "Events"

let repository : Repository = {
    Load = fun () -> loadState StateFile
    Save = fun state -> saveState StateFile state
}

let turnOff () = execute repository TurnOff
let turnOn () = execute repository TurnOn
