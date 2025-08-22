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

// ----------
// Domain
// ----------

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













#r "nuget: FSharp.SystemTextJson,1.4.36"

open System.IO
open System.Text.Json
open System.Text.Json.Serialization

let jsonOptions = JsonFSharpOptions.Default().ToJsonSerializerOptions()

let [<Literal>] StateFile = "State"
let [<Literal>] EventsFile = "Events"
