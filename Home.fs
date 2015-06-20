module FreyaMusicStore.Home

open Arachne.Http

open Chiron

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

type Container = 
    { Greeting : string }

    static member ToJson (x: Container) =
            Json.write "greeting" x.Greeting

let fetch = Freya.init (fun (_ : Db.DbContext) -> {Greeting = "Hello World!"} )


let pipe = 
    freyaMachine {
        including (res fetch "home")
        methodsSupported (Freya.init [GET]) } |> FreyaMachine.toPipeline