module FreyaMusicStore.Genre

open Arachne.Http

open Chiron
open Chiron.Operators

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http
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

let ok spec =
    freya {
        let! name = name
        let ctx = Db.getContext()
        let albums = Db.getAlbumsForGenre name ctx |> Array.map Album.Album.fromDb
        let genre = { Name = name; Albums = albums }
        return!
            match spec.MediaTypes with
            | Free ->  repJson genre
            | Negotiated (m :: _) when m = MediaType.Json -> repJson genre
            | Negotiated (m :: _) when m = MediaType.Html -> writeHtml ("genre", genre)
            | _ -> failwith "Representation Failure"
    }

let pipe =
    freyaMachine {
        using http
        mediaTypesSupported (Freya.init [MediaType.Html; MediaType.Json])
        methodsSupported ( freya { return [ GET ] } )
        handleOk ok } |> FreyaMachine.toPipeline