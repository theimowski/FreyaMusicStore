module FreyaMusicStore.Album

open System

open Arachne.Http

open Freya.Core
open Freya.Router
open Freya.Machine
open Freya.Machine.Extensions.Http

let albumId =
    freya {
        let! id = Freya.getLensPartial (Route.atom "id")
        match Int32.TryParse id.Value with
        | true, id -> return Some id
        | _ -> return None
    }

let getAlbum =
    freya {
        let! id = albumId
        let ctx = Db.getContext()
        let album = Db.getAlbumDetails id.Value ctx |> Option.get |> View.toAlbum
        return! write ("album", album)
    }

let albumMalformed = 
    freya {
        let! id = albumId
        return Option.isNone id
    }

let albumExists = 
    freya {
        let! id = albumId
        let ctx = Db.getContext()
        return Db.getAlbumDetails id.Value ctx |> Option.isSome
    }

let album = 
    freyaMachine {
        including common
        malformed albumMalformed
        exists albumExists
        methodsSupported ( freya { return [ GET ] } ) 
        handleOk (fun _ -> getAlbum) } |> FreyaMachine.toPipeline