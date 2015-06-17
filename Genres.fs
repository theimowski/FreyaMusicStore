module FreyaMusicStore.Genres

open Arachne.Http

open Chiron

open Freya.Core
open Freya.Machine
open Freya.Machine.Extensions.Http

let ok spec =
    freya {
        let ctx = Db.getContext()
        let genres = Db.getGenres ctx |> Array.map (fun g -> g.Name)
        return!
            match spec.MediaTypes with
            | Free ->  repJson genres
            | Negotiated (m :: _) when m = MediaType.Json -> repJson genres
            | Negotiated (m :: _) when m = MediaType.Html -> writeHtml ("genres", genres)
            | _ -> failwith "Representation Failure"
    }

let pipe = 
    freyaMachine {
        using http
        mediaTypesSupported (Freya.init [MediaType.Html; MediaType.Json])
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk ok } |> FreyaMachine.toPipeline