module FreyaMusicStore.Home

open Chiron

open Freya.Core
open Freya.Machine

type Container = 
    { Greeting : string }

    static member ToJson (x: Container) =
            Json.write "greeting" x.Greeting

let fetch = Freya.init (fun (_ : Db.DbContext) -> {Greeting = "Hello World!"} )

let pipe = res fetch "home" |> FreyaMachine.toPipeline