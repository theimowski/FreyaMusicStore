module FreyaMusicStore.Genre

open Arachne.Http

open Chiron
open Chiron.Operators

open Freya.Core
open Freya.Machine
open Freya.Router

type Genre = 
    { Name : string
      Albums : Album.Album [] }

    static member ToJson (x: Genre) =
            Json.write "name" x.Name
         *> Json.write "albums" x.Albums

let name =
    freya {
        let! name = Freya.getLensPartial (Route.atom "0")
        return name.Value
    }

let fetch : Freya<Db.DbContext -> _> = 
    freya {
        let! name = name
        return 
            Db.getAlbumsForGenre name
            >> Array.map Album.Album.fromDb
            >> (fun albums -> { Name = name; Albums = albums })
    }

let pipe = res fetch "genre" |> FreyaMachine.toPipeline