module FreyaMusicStore.Genres

open Arachne.Http

open Chiron

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

type Genre = 
    { Name : string }

    static member fromDb (g: Db.Genre) =
        { Name = g.Name }

    static member ToJson (x: Genre) =
            Json.write "name" x.Name

type Genres = 
    { Genres : Genre [] }

let repJson x =
    Freya.init
        { Data = (Json.serialize >> Json.format >> System.Text.Encoding.UTF8.GetBytes) x
          Description =
            { Charset = Some Charset.Utf8
              Encodings = None
              MediaType = Some MediaType.Json
              Languages = None } }

let ok spec =
    freya {
        let ctx = Db.getContext()
        let genres = { Genres = Db.getGenres ctx |> Array.map Genre.fromDb }
        return!
            match spec.MediaTypes with
            | Free ->  repJson genres.Genres
            | Negotiated (m :: _) when m = MediaType.Json -> repJson genres.Genres
            | Negotiated (m :: _) when m = MediaType.Html -> writeHtml ("genres", genres)
            | _ -> failwith "Representation Failure"
    }

let pipe = 
    freyaMachine {
        using http
        mediaTypesSupported (Freya.init [MediaType.Html; MediaType.Json])
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk ok } |> FreyaMachine.toPipeline