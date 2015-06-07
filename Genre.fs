module FreyaMusicStore.Genre

open Arachne.Http

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http
open Freya.Router

type Genre = {
    Name : string
    Albums : Album.Album []
}

let name =
    freya {
        let! name = Freya.getLensPartial (Route.atom "name")
        return name.Value
    }

let get =
    freya {
        let! name = name
        let ctx = Db.getContext()
        let albums = Db.getAlbumsForGenre name ctx |> Array.map Album.Album.fromDb
        return! write ("genre", { Name = name; Albums = albums } )
    }

let pipe =
    freyaMachine {
        including common
        methodsSupported ( freya { return [ GET ] } )
        handleOk (fun _ -> get ) } |> FreyaMachine.toPipeline