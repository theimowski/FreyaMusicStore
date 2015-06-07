module FreyaMusicStore.Home

open Arachne.Http

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

type Container = {
    Greeting : string
}

let pipe =
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } )
        handleOk (fun _ -> freya { return!  write ("home", {Greeting = "Hello World!" } ) } ) } |> FreyaMachine.toPipeline