module FreyaMusicStore.Genre

open Arachne.Http

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Router

type Genre = {
    Name : string
}

let getGenre =
    freya {
        let! name = Freya.getLensPartial (Route.atom "name")
        return! write ("home", {Name = sprintf "Genre: %s" name.Value})
    }

let pipe =
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } )
        handleOk (fun _ -> getGenre ) } |> FreyaMachine.toPipeline