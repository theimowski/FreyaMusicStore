module FreyaMusicStore.Genres

open Arachne.Http

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

let fetch = Db.getGenres >> Array.map (fun g -> g.Name) |> Freya.init

let pipe = 
    freyaMachine {
        including (res fetch "genres")
        methodsSupported (Freya.init [GET]) } |> FreyaMachine.toPipeline